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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Services.Services
{
    public class WarehouseInboundRequestService : IWarehouseInboundRequestService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly INotificationService _notificationService;
        private readonly ICodeGenerator _codeGenerator;

        public WarehouseInboundRequestService(IUnitOfWork unitOfWork, INotificationService notificationService, ICodeGenerator codeGenerator)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _codeGenerator = codeGenerator ?? throw new ArgumentNullException(nameof(codeGenerator));
        }

        //public async Task<IServiceResult> CreateRequestAsync(Guid userId, WarehouseInboundRequestCreateDto dto)
        //{
        //    var farmer = await _unitOfWork.FarmerRepository.FindByUserIdAsync(userId);
        //    if (farmer == null)
        //        return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không tìm thấy Farmer tương ứng với User.");

        //    var inboundCode = await _codeGenerator.GenerateInboundRequestCodeAsync();

        //    var newRequest = new WarehouseInboundRequest
        //    {
        //        InboundRequestId = Guid.NewGuid(),
        //        InboundRequestCode = inboundCode,
        //        FarmerId = farmer.FarmerId,
        //        BatchId = dto.BatchId ?? Guid.Empty,
        //        RequestedQuantity = dto.RequestedQuantity,
        //        PreferredDeliveryDate = dto.PreferredDeliveryDate,
        //        Status = "Pending",
        //        Note = dto.Note,
        //        CreatedAt = DateTime.UtcNow,
        //        UpdatedAt = DateTime.UtcNow
        //    };

        //    await _unitOfWork.WarehouseInboundRequests.CreateAsync(newRequest);
        //    await _unitOfWork.SaveChangesAsync();

        //    await _notificationService.NotifyInboundRequestCreatedAsync(newRequest.InboundRequestId, farmer.FarmerId);

        //    return new ServiceResult(Const.SUCCESS_CREATE_CODE, "Tạo yêu cầu nhập kho thành công", newRequest.InboundRequestId);
        //}
        public async Task<IServiceResult> CreateRequestAsync(Guid userId, WarehouseInboundRequestCreateDto dto)
        {
            var farmer = await _unitOfWork.FarmerRepository.FindByUserIdAsync(userId);
            if (farmer == null)
                return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không tìm thấy Farmer tương ứng với User.");

            if (dto.BatchId == null || dto.BatchId == Guid.Empty)
                return new ServiceResult(Const.FAIL_CREATE_CODE, "Thiếu thông tin lô chế biến.");

            var batch = await _unitOfWork.ProcessingBatchRepository.GetByIdAsync(dto.BatchId.Value);
            if (batch == null)
                return new ServiceResult(Const.FAIL_CREATE_CODE, "Không tìm thấy lô chế biến.");

            if (batch.FarmerId != farmer.FarmerId)
                return new ServiceResult(Const.FAIL_CREATE_CODE, "Bạn không có quyền gửi yêu cầu nhập kho cho lô chế biến này.");

            // ✅ Kiểm tra ngày giao không được nhỏ hơn ngày hiện tại
            if (dto.PreferredDeliveryDate < DateOnly.FromDateTime(DateTime.UtcNow))
            {
                return new ServiceResult(Const.FAIL_CREATE_CODE, "Ngày giao dự kiến không được nằm trong quá khứ.");
            }

            var inboundCode = await _codeGenerator.GenerateInboundRequestCodeAsync();

            var newRequest = dto.ToEntityFromCreateDto(farmer.FarmerId, inboundCode);

            await _unitOfWork.WarehouseInboundRequests.CreateAsync(newRequest);
            await _unitOfWork.SaveChangesAsync();

            await _notificationService.NotifyInboundRequestCreatedAsync(newRequest.InboundRequestId, farmer.FarmerId);

            return new ServiceResult(Const.SUCCESS_CREATE_CODE, "Tạo yêu cầu nhập kho thành công", newRequest.InboundRequestId);
        }




        public async Task<IServiceResult> ApproveRequestAsync(Guid requestId, Guid staffUserId)
        {
            var request = await _unitOfWork.WarehouseInboundRequests.GetByIdAsync(requestId);
            if (request == null)
                return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không tìm thấy yêu cầu nhập kho.");

            if (request.Status != InboundRequestStatus.Pending.ToString())
                return new ServiceResult(Const.FAIL_UPDATE_CODE, "Yêu cầu đã được xử lý hoặc không hợp lệ.");

            var staff = await _unitOfWork.BusinessStaffRepository.FindByUserIdAsync(staffUserId);
            if (staff == null)
                return new ServiceResult(Const.FAIL_UPDATE_CODE, "Không xác định được nhân viên xử lý.");

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

        public async Task<IServiceResult> GetAllAsync()
        {
            var requests = await _unitOfWork.WarehouseInboundRequests.GetAllWithIncludesAsync();
            if (requests == null || !requests.Any())
            {
                return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không có yêu cầu nhập kho nào.", new List<WarehouseInboundRequestViewDto>());
            }

            var result = requests.Select(r => r.ToViewDto()).ToList();
            return new ServiceResult(Const.SUCCESS_READ_CODE, "Lấy danh sách yêu cầu nhập kho thành công", result);
        }

        public async Task<IServiceResult> GetByIdAsync(Guid requestId)
        {
            var request = await _unitOfWork.WarehouseInboundRequests.GetDetailByIdAsync(requestId);
            if (request == null)
            {
                return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không tìm thấy yêu cầu.", null);
            }

            var dto = request.ToDetailDto();
            return new ServiceResult(Const.SUCCESS_READ_CODE, "Lấy chi tiết thành công", dto);
        }

        public async Task<IServiceResult> CancelRequestAsync(Guid requestId, Guid farmerUserId)
        {
            var request = await _unitOfWork.WarehouseInboundRequests.GetByIdAsync(requestId);
            if (request == null || request.Status != InboundRequestStatus.Pending.ToString())
            {
                return new ServiceResult(Const.FAIL_UPDATE_CODE, "Yêu cầu không tồn tại hoặc không thể huỷ.");
            }

            var farmer = await _unitOfWork.FarmerRepository.FindByUserIdAsync(farmerUserId);
            if (farmer == null || request.FarmerId != farmer.FarmerId)
            {
                return new ServiceResult(Const.FAIL_UPDATE_CODE, "Không có quyền huỷ yêu cầu này.");
            }

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
            {
                return new ServiceResult(Const.FAIL_UPDATE_CODE, "Yêu cầu không tồn tại hoặc đã được xử lý.");
            }

            var staff = await _unitOfWork.BusinessStaffRepository.FindByUserIdAsync(staffUserId);
            if (staff == null)
            {
                return new ServiceResult(Const.FAIL_UPDATE_CODE, "Không xác định được nhân viên.");
            }

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

            var dto = request.ToFarmerDetailDto(); // ✅ dùng DTO mới
            return new ServiceResult(Const.SUCCESS_READ_CODE, "Lấy chi tiết thành công", dto);
        }

    }
}
