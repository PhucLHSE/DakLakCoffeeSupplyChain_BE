using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.WarehouseInboundRequestDTOs;
using DakLakCoffeeSupplyChain.Common.Enum.WarehouseInboundRequestEnums;
using DakLakCoffeeSupplyChain.Repositories.Models;
using DakLakCoffeeSupplyChain.Repositories.UnitOfWork;
using DakLakCoffeeSupplyChain.Services.Base;
using DakLakCoffeeSupplyChain.Services.IServices;
using DakLakCoffeeSupplyChain.Services.Mappers;
using DakLakCoffeeSupplyChain.Services.Generators;
using System;
using System.Linq;
using System.Threading.Tasks;
using DakLakCoffeeSupplyChain.Common.Enum.ProcessingEnums;
using System.Collections.Generic;

namespace DakLakCoffeeSupplyChain.Services.Services
{
    public class WarehouseInboundRequestService : IWarehouseInboundRequestService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly INotificationService _notificationService;
        private readonly ICodeGenerator _codeGenerator;

        public WarehouseInboundRequestService(
            IUnitOfWork unitOfWork,
            INotificationService notificationService,
            ICodeGenerator codeGenerator)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _codeGenerator = codeGenerator ?? throw new ArgumentNullException(nameof(codeGenerator));
        }

        /// <summary>
        /// Tính khối lượng còn lại có thể yêu cầu nhập kho cho một lô
        /// </summary>
        private async Task<double> CalcRemainingForBatchAsync(Guid batchId, Guid? excludeRequestId = null)
        {
            // 1. Lấy tất cả progress của batch này
            var progresses = await _unitOfWork.ProcessingBatchProgressRepository.GetAllAsync(
                p => p.BatchId == batchId && !p.IsDeleted && p.OutputQuantity != null
            );
            
            if (!progresses.Any())
                return 0; // Không có progress nào

            // Lấy OutputQuantity của bước cuối cùng (StepIndex cao nhất)
            var finalProgress = progresses.OrderByDescending(p => p.StepIndex).First();
            double totalOutput = finalProgress.OutputQuantity ?? 0;

            // 2. Tổng tất cả InboundRequest đã được xử lý (trừ request hiện tại)
            var allRequests = await _unitOfWork.WarehouseInboundRequests.GetAllAsync(
                r => r.BatchId == batchId && !r.IsDeleted && 
                     r.InboundRequestId != (excludeRequestId ?? Guid.Empty)
            );
            
            // 3. Tính theo trạng thái
            double totalCompleted = allRequests
                .Where(r => r.Status == InboundRequestStatus.Completed.ToString())
                .Sum(r => r.RequestedQuantity ?? 0);
                
            double totalPendingApproved = allRequests
                .Where(r => r.Status == InboundRequestStatus.Pending.ToString() || 
                           r.Status == InboundRequestStatus.Approved.ToString())
                .Sum(r => r.RequestedQuantity ?? 0);

            // 4. Tính khối lượng còn lại = Output lớn nhất - (Đã nhập + Đang xử lý)
            double remaining = totalOutput - (totalCompleted + totalPendingApproved);
            
            // Debug logging
            Console.WriteLine($"DEBUG CalcRemainingForBatchAsync for BatchId: {batchId}");
            Console.WriteLine($"  - Total Output: {totalOutput}");
            Console.WriteLine($"  - Total Completed Requests: {totalCompleted}");
            Console.WriteLine($"  - Total Pending/Approved Requests: {totalPendingApproved}");
            Console.WriteLine($"  - Remaining: {remaining}");
            
            return remaining < 0 ? 0 : remaining;
        }

        /// <summary>
        /// Tính khối lượng còn lại có thể yêu cầu nhập kho cho một mùa vụ (cà phê tươi)
        /// </summary>
        private async Task<double> CalcRemainingForDetailAsync(Guid detailId, Guid? excludeRequestId = null)
        {
            var detail = await _unitOfWork.CropSeasonDetailRepository.GetByIdAsync(detailId);
            if (detail == null) return 0;

            // Lấy tất cả yêu cầu nhập kho đã được duyệt cho detail này
            var approvedRequests = await _unitOfWork.WarehouseInboundRequests.GetAllAsync(r =>
                r.DetailId == detailId &&
                r.Status == InboundRequestStatus.Approved.ToString() &&
                !r.IsDeleted &&
                (excludeRequestId == null || r.InboundRequestId != excludeRequestId)
            );

            double totalRequested = approvedRequests.Sum(r => r.RequestedQuantity ?? 0);

            // Lấy tất cả phiếu nhập kho đã xác nhận cho detail này
            var receipts = await _unitOfWork.WarehouseReceipts.GetAllAsync(r =>
                r.DetailId == detailId &&
                r.ReceivedQuantity.HasValue &&
                !r.IsDeleted
            );

            double totalReceived = receipts.Sum(r => r.ReceivedQuantity ?? 0);

            // Khối lượng còn lại = Sản lượng thực tế - Tổng đã yêu cầu + Tổng đã nhập
            return Math.Max(0, (detail.ActualYield ?? 0) - totalRequested + totalReceived);
        }

        public async Task<IServiceResult> CreateRequestAsync(Guid userId, WarehouseInboundRequestCreateDto dto)
        {
            var farmer = await _unitOfWork.FarmerRepository.FindByUserIdAsync(userId);
            if (farmer == null)
                return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không tìm thấy Farmer tương ứng với User.");

            // Kiểm tra validation: phải có BatchId HOẶC DetailId, không được có cả hai
            if (dto.BatchId.HasValue && dto.DetailId.HasValue)
                return new ServiceResult(Const.FAIL_CREATE_CODE, "Chỉ được chọn một loại sản phẩm (lô sản xuất hoặc mùa vụ).");

            if (!dto.BatchId.HasValue && !dto.DetailId.HasValue)
                return new ServiceResult(Const.FAIL_CREATE_CODE, "Phải chọn một loại sản phẩm (lô sản xuất hoặc mùa vụ).");

            if (dto.RequestedQuantity <= 0)
                return new ServiceResult(Const.FAIL_CREATE_CODE, "Khối lượng yêu cầu phải lớn hơn 0.");

            double remaining = 0;

            if (dto.BatchId.HasValue)
            {
                var batch = await _unitOfWork.ProcessingBatchRepository.GetByIdAsync(dto.BatchId.Value);
                if (batch == null)
                    return new ServiceResult(Const.FAIL_CREATE_CODE, "Không tìm thấy lô chế biến.");

                if (batch.FarmerId != farmer.FarmerId)
                    return new ServiceResult(Const.FAIL_CREATE_CODE, "Bạn không có quyền gửi yêu cầu cho lô chế biến này.");

                // Lô phải hoàn tất
                if (!string.Equals(batch.Status, ProcessingStatus.Completed.ToString(), StringComparison.OrdinalIgnoreCase))
                    return new ServiceResult(Const.FAIL_CREATE_CODE, "Chỉ được gửi yêu cầu nhập kho cho lô đã hoàn tất sơ chế.");

                // Kiểm tra khối lượng còn lại
                remaining = await CalcRemainingForBatchAsync(batch.BatchId);
            }
            else if (dto.DetailId.HasValue)
            {
                var detail = await _unitOfWork.CropSeasonDetailRepository.GetByIdAsync(dto.DetailId.Value);
                if (detail == null)
                    return new ServiceResult(Const.FAIL_CREATE_CODE, "Không tìm thấy mùa vụ.");

                var cropSeason = await _unitOfWork.CropSeasonRepository.GetByIdAsync(detail.CropSeasonId);
                if (cropSeason == null || cropSeason.FarmerId != farmer.FarmerId)
                    return new ServiceResult(Const.FAIL_CREATE_CODE, "Bạn không có quyền gửi yêu cầu cho mùa vụ này.");

                // Mùa vụ phải hoàn tất
                if (!string.Equals(detail.Status, "Completed", StringComparison.OrdinalIgnoreCase))
                    return new ServiceResult(Const.FAIL_CREATE_CODE, "Chỉ được gửi yêu cầu nhập kho cho mùa vụ đã hoàn tất.");

                // ✅ THÊM: Kiểm tra ràng buộc hợp đồng - ProcessMethodId
                var planDetail = detail.CommitmentDetail?.PlanDetail;
                if (planDetail == null)
                    return new ServiceResult(Const.FAIL_CREATE_CODE, "Không tìm thấy thông tin kế hoạch từ hợp đồng.");

                        // Nếu ProcessMethodId có giá trị → Yêu cầu sơ chế → Không được giao tươi
        if (planDetail.ProcessMethodId.HasValue && planDetail.ProcessMethodId.Value > 0)
        {
            return new ServiceResult(Const.FAIL_CREATE_CODE, 
                "Cam kết sơ chế, vui lòng gửi hàng sơ chế hoặc nếu chưa có hãy tạo lô sơ chế trước khi gửi yêu cầu nhập kho.");
        }

                // Kiểm tra khối lượng còn lại
                remaining = await CalcRemainingForDetailAsync(detail.DetailId);
            }

            // Ngày giao dự kiến không nằm quá khứ
            if (dto.PreferredDeliveryDate < DateOnly.FromDateTime(DateTime.UtcNow))
                return new ServiceResult(Const.FAIL_CREATE_CODE, "Ngày giao dự kiến không được nằm trong quá khứ.");

            if (remaining <= 0)
                return new ServiceResult(Const.FAIL_CREATE_CODE, "Sản phẩm này đã hết khối lượng có thể yêu cầu nhập kho.");

            if (dto.RequestedQuantity > remaining)
                return new ServiceResult(Const.FAIL_CREATE_CODE,
                    $"Khối lượng yêu cầu vượt quá lượng còn lại. Hiện còn {remaining} kg có thể yêu cầu nhập kho.");

            // Sinh mã và lưu
            var inboundCode = await _codeGenerator.GenerateInboundRequestCodeAsync();
            var newRequest = dto.ToEntityFromCreateDto(farmer.FarmerId, inboundCode);

            await _unitOfWork.WarehouseInboundRequests.CreateAsync(newRequest);
            await _unitOfWork.SaveChangesAsync();

             _notificationService.NotifyInboundRequestCreatedAsync(newRequest.InboundRequestId, farmer.FarmerId);

            return new ServiceResult(Const.SUCCESS_CREATE_CODE, "Tạo yêu cầu nhập kho thành công", newRequest.InboundRequestId);
        }

        public async Task<IServiceResult> ApproveRequestAsync(Guid requestId, Guid staffUserId)
        {
            var request = await _unitOfWork.WarehouseInboundRequests.GetByIdAsync(requestId);
            if (request == null)
                return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không tìm thấy yêu cầu nhập kho.");

            if (!string.Equals(request.Status, InboundRequestStatus.Pending.ToString(), StringComparison.OrdinalIgnoreCase))
                return new ServiceResult(Const.FAIL_UPDATE_CODE, "Yêu cầu đã được xử lý hoặc không hợp lệ.");

            var staff = await _unitOfWork.BusinessStaffRepository.FindByUserIdAsync(staffUserId);
            if (staff == null)
                return new ServiceResult(Const.FAIL_UPDATE_CODE, "Không xác định được nhân viên xử lý.");

            // Re-check khối lượng còn lại để chống over-commit
            double remaining = 0;
            double qty = request.RequestedQuantity ?? 0;

            if (request.BatchId != null)
            {
                remaining = await CalcRemainingForBatchAsync(request.BatchId.Value, request.InboundRequestId);
            }
            else if (request.DetailId != null)
            {
                remaining = await CalcRemainingForDetailAsync(request.DetailId.Value, request.InboundRequestId);
            }

            if (qty <= 0)
                return new ServiceResult(Const.FAIL_UPDATE_CODE, "Khối lượng yêu cầu không hợp lệ.");

            if (qty > remaining)
                return new ServiceResult(Const.FAIL_UPDATE_CODE,
                    $"Không thể duyệt vì khối lượng còn lại chỉ còn {remaining} kg.");

            request.Status = InboundRequestStatus.Approved.ToString();
            request.BusinessStaffId = staff.StaffId;
            request.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.WarehouseInboundRequests.Update(request);
            await _unitOfWork.SaveChangesAsync();

            if (request.Farmer?.User != null)
            {
                await _notificationService.NotifyInboundRequestApprovedAsync(
                    request.InboundRequestId,
                    request.Farmer.User.UserId
                );
            }

            return new ServiceResult(Const.SUCCESS_UPDATE_CODE, "Duyệt yêu cầu nhập kho thành công.");
        }

        public async Task<IServiceResult> GetAllAsync(Guid userId)
        {
            // Xác định ManagerId (manager hoặc supervisor của staff)
            Guid? managerId = null;

            var manager = await _unitOfWork.BusinessManagerRepository.FindByUserIdAsync(userId);

            if (manager != null && !manager.IsDeleted)
            {
                managerId = manager.ManagerId;
            }
            else
            {
                var staff = await _unitOfWork.BusinessStaffRepository.FindByUserIdAsync(userId);

                if (staff != null && !staff.IsDeleted && staff.SupervisorId != null)
                    managerId = staff.SupervisorId;
                else
                    return new ServiceResult(Const.FAIL_READ_CODE, "Không xác định được công ty của người dùng.");
            }

            var allRequests = await _unitOfWork.WarehouseInboundRequests.GetAllWithIncludesAsync();
            
            Console.WriteLine($"DEBUG GetAllAsync: Total requests: {allRequests.Count}");
            Console.WriteLine($"DEBUG GetAllAsync: ManagerId: {managerId}");

            // Tạm thời bỏ filter để xem tất cả requests
            var filtered = allRequests
                .Where(r => !r.IsDeleted)
                .Select(r => {
                    Console.WriteLine($"DEBUG Request {r.InboundRequestId}: BatchId={r.BatchId}, DetailId={r.DetailId}, " +
                                    $"Status={r.Status}, FarmerId={r.FarmerId}");
                    return r.ToViewDto();
                })
                .ToList();

            return new ServiceResult(Const.SUCCESS_READ_CODE, "Lấy danh sách yêu cầu theo công ty thành công", filtered);
        }

        public async Task<IServiceResult> GetByIdAsync(Guid requestId)
        {
            var request = await _unitOfWork.WarehouseInboundRequests.GetDetailByIdAsync(requestId);

            if (request == null)
                return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không tìm thấy yêu cầu.", null);

            var dto = request.ToDetailDto();

            return new ServiceResult(Const.SUCCESS_READ_CODE, "Lấy chi tiết thành công", dto);
        }

        public async Task<IServiceResult> CancelRequestAsync(Guid requestId, Guid farmerUserId)
        {
            var request = await _unitOfWork.WarehouseInboundRequests.GetByIdAsync(requestId);

            if (request == null || request.Status != InboundRequestStatus.Pending.ToString())
                return new ServiceResult(Const.FAIL_UPDATE_CODE, "Yêu cầu không tồn tại hoặc không thể huỷ.");

            var farmer = await _unitOfWork.FarmerRepository.FindByUserIdAsync(farmerUserId);

            if (farmer == null || request.FarmerId != farmer.FarmerId)
                return new ServiceResult(Const.FAIL_UPDATE_CODE, "Không có quyền huỷ yêu cầu này.");

            request.Status = InboundRequestStatus.Cancelled.ToString();
            request.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.WarehouseInboundRequests.Update(request);
            await _unitOfWork.SaveChangesAsync();

            return new ServiceResult(Const.SUCCESS_UPDATE_CODE, "Đã huỷ yêu cầu thành công.");
        }

        public async Task<IServiceResult> RejectRequestAsync(Guid requestId, Guid staffUserId)
        {
            var request = await _unitOfWork.WarehouseInboundRequests.GetByIdAsync(requestId);
            if (request == null || request.Status != InboundRequestStatus.Pending.ToString())
                return new ServiceResult(Const.FAIL_UPDATE_CODE, "Yêu cầu không tồn tại hoặc đã được xử lý.");

            var staff = await _unitOfWork.BusinessStaffRepository.FindByUserIdAsync(staffUserId);
            if (staff == null)
                return new ServiceResult(Const.FAIL_UPDATE_CODE, "Không xác định được nhân viên.");

            request.Status = InboundRequestStatus.Rejected.ToString();
            request.BusinessStaffId = staff.StaffId;
            request.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.WarehouseInboundRequests.Update(request);
            await _unitOfWork.SaveChangesAsync();

            return new ServiceResult(Const.SUCCESS_UPDATE_CODE, "Đã từ chối yêu cầu thành công.");
        }

        public async Task<IServiceResult> GetAllByFarmerAsync(Guid userId)
        {
            var farmer = await _unitOfWork.FarmerRepository.FindByUserIdAsync(userId);
            if (farmer == null)
                return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không tìm thấy nông dân.");

            var requests = await _unitOfWork.WarehouseInboundRequests.GetAllByFarmerIdAsync(farmer.FarmerId);
            var result = requests.Select(r => r.ToFarmerViewDto()).ToList();

            return new ServiceResult(Const.SUCCESS_READ_CODE, "Lấy danh sách thành công", result);
        }

        public async Task<IServiceResult> GetByIdForFarmerAsync(Guid requestId, Guid userId)
        {
            var farmer = await _unitOfWork.FarmerRepository.FindByUserIdAsync(userId);
            if (farmer == null)
                return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không tìm thấy Farmer.");

            var request = await _unitOfWork.WarehouseInboundRequests.GetDetailByIdAsync(requestId);
            if (request == null || request.FarmerId != farmer.FarmerId)
                return new ServiceResult(Const.FAIL_UPDATE_CODE, "Bạn không có quyền truy cập yêu cầu này.");

            var dto = request.ToFarmerDetailDto();
            return new ServiceResult(Const.SUCCESS_READ_CODE, "Lấy chi tiết thành công", dto);
        }
    }
}
