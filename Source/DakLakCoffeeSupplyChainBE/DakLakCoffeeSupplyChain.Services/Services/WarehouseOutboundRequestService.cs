using DakLakCoffeeSupplyChain.Common.DTOs.WarehouseOutboundRequestDTOs;
using DakLakCoffeeSupplyChain.Common.Enum.WarehouseOutboundRequestEnums;
using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Repositories.Models;
using DakLakCoffeeSupplyChain.Repositories.UnitOfWork;
using DakLakCoffeeSupplyChain.Services.Base;
using DakLakCoffeeSupplyChain.Services.IServices;
using DakLakCoffeeSupplyChain.Services.Generators;
using DakLakCoffeeSupplyChain.Services.Mappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Services.Services
{
    public class WarehouseOutboundRequestService : IWarehouseOutboundRequestService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly INotificationService _notificationService;
        private readonly ICodeGenerator _codeGenerator;

        public WarehouseOutboundRequestService(
            IUnitOfWork unitOfWork,
            INotificationService notificationService,
            ICodeGenerator codeGenerator)
        {
            _unitOfWork = unitOfWork;
            _notificationService = notificationService;
            _codeGenerator = codeGenerator;
        }

        //public async Task<IServiceResult> CreateRequestAsync(Guid managerUserId, WarehouseOutboundRequestCreateDto dto)
        //{
        //    var manager = await _unitOfWork.BusinessManagerRepository.FindByUserIdAsync(managerUserId);
        //    if (manager == null)
        //        return new ServiceResult(Const.FAIL_READ_CODE, "Không tìm thấy người dùng yêu cầu.");

        //    // 🔍 Validate: kiểm tra tồn kho có đủ không
        //    var inventory = await _unitOfWork.Inventories.GetByIdAsync(dto.InventoryId);
        //    if (inventory == null || inventory.IsDeleted)
        //        return new ServiceResult(Const.FAIL_READ_CODE, "Không tìm thấy thông tin tồn kho.");

        //    if (inventory.WarehouseId != dto.WarehouseId)
        //        return new ServiceResult(Const.ERROR_VALIDATION_CODE, "Tồn kho không thuộc kho được chọn.");

        //    if (dto.RequestedQuantity <= 0)
        //        return new ServiceResult(Const.ERROR_VALIDATION_CODE, "Số lượng yêu cầu phải lớn hơn 0.");

        //    if (dto.RequestedQuantity > inventory.Quantity)
        //        return new ServiceResult(Const.ERROR_VALIDATION_CODE,
        //            $"Tồn kho hiện tại chỉ còn {inventory.Quantity:n0} {inventory.Unit}, không thể yêu cầu xuất {dto.RequestedQuantity:n0}.");

        //    var generatedCode = await _codeGenerator.GenerateOutboundRequestCodeAsync();

        //    var request = new WarehouseOutboundRequest
        //    {
        //        OutboundRequestId = Guid.NewGuid(),
        //        OutboundRequestCode = generatedCode,
        //        WarehouseId = dto.WarehouseId,
        //        InventoryId = dto.InventoryId,
        //        RequestedQuantity = dto.RequestedQuantity,
        //        Unit = dto.Unit,
        //        Purpose = dto.Purpose,
        //        Reason = dto.Reason,
        //        OrderItemId = dto.OrderItemId,
        //        RequestedBy = manager.ManagerId,
        //        Status = WarehouseOutboundRequestStatus.Pending.ToString(),
        //        CreatedAt = DateTime.UtcNow,
        //        UpdatedAt = DateTime.UtcNow,
        //        IsDeleted = false
        //    };

        //    await _unitOfWork.WarehouseOutboundRequests.CreateAsync(request);
        //    await _unitOfWork.SaveChangesAsync();

        //    await _notificationService.NotifyOutboundRequestCreatedAsync(request.OutboundRequestId, manager.ManagerId);

        //    return new ServiceResult(Const.SUCCESS_CREATE_CODE, "Tạo yêu cầu xuất kho thành công", request.OutboundRequestId);
        //}
        public async Task<IServiceResult> CreateRequestAsync(Guid managerUserId, WarehouseOutboundRequestCreateDto dto)
        {
            var manager = await _unitOfWork.BusinessManagerRepository.FindByUserIdAsync(managerUserId);
            if (manager == null)
                return new ServiceResult(Const.FAIL_READ_CODE, "Không tìm thấy người dùng yêu cầu.");

            // 🔍 Validate tồn kho (gọi GetDetailByIdAsync để có Batch và CoffeeType)
            var inventory = await _unitOfWork.Inventories.GetDetailByIdAsync(dto.InventoryId);
            if (inventory == null || inventory.IsDeleted)
                return new ServiceResult(Const.FAIL_READ_CODE, "Không tìm thấy thông tin tồn kho.");

            if (inventory.WarehouseId != dto.WarehouseId)
                return new ServiceResult(Const.ERROR_VALIDATION_CODE, "Tồn kho không thuộc kho được chọn.");

            if (dto.RequestedQuantity <= 0)
                return new ServiceResult(Const.ERROR_VALIDATION_CODE, "Số lượng yêu cầu phải lớn hơn 0.");

            if (dto.RequestedQuantity > inventory.Quantity)
                return new ServiceResult(Const.ERROR_VALIDATION_CODE,
                    $"Tồn kho hiện tại chỉ còn {inventory.Quantity:n0} {inventory.Unit}, không thể yêu cầu xuất {dto.RequestedQuantity:n0}.");

            // ✅ Nếu có liên kết với OrderItem => kiểm tra vượt quá hợp đồng và loại cà phê
            if (dto.OrderItemId.HasValue)
            {
                var orderItem = await _unitOfWork.OrderItemRepository.GetByIdAsync(
                    predicate: oi =>
                        oi.OrderItemId == dto.OrderItemId.Value &&
                        !oi.IsDeleted,
                    asNoTracking: true
                );

                if (orderItem == null)
                    return new ServiceResult(Const.FAIL_CREATE_CODE, "Không tìm thấy dòng đơn hàng tương ứng.");

                // 🔍 Lấy Product để so sánh loại cà phê
                var product = await _unitOfWork.ProductRepository.GetByIdAsync(orderItem.ProductId);
                if (product == null)
                    return new ServiceResult(Const.FAIL_CREATE_CODE, "Không tìm thấy sản phẩm tương ứng trong đơn hàng.");

                var productCoffeeTypeId = product.CoffeeTypeId;
                var inventoryCoffeeTypeId = inventory.Batch?.CoffeeTypeId;

                if (inventoryCoffeeTypeId == null)
                    return new ServiceResult(Const.FAIL_CREATE_CODE, "Không xác định được loại cà phê trong tồn kho.");

                if (productCoffeeTypeId != inventoryCoffeeTypeId)
                {
                    return new ServiceResult(Const.ERROR_VALIDATION_CODE,
                        "Loại cà phê trong tồn kho không khớp với sản phẩm trong đơn hàng.");
                }

                // ⚠️ Tổng số lượng đã Completed (không tính Pending)
                var completedRequests = await _unitOfWork.WarehouseOutboundRequests.GetAllAsync(
                    predicate: r =>
                        r.OrderItemId == dto.OrderItemId.Value &&
                        !r.IsDeleted &&
                        r.Status == WarehouseOutboundRequestStatus.Completed.ToString()
                );

                double totalCompleted = completedRequests.Sum(r => r.RequestedQuantity);
                double allowedQuantity = orderItem.Quantity ?? 0.0;
                double newQuantity = dto.RequestedQuantity;

                if (totalCompleted + newQuantity > allowedQuantity)
                {
                    return new ServiceResult(Const.ERROR_VALIDATION_CODE,
                        $"Số lượng yêu cầu vượt quá giới hạn hợp đồng ({allowedQuantity:n0}). " +
                        $"Đã xuất: {totalCompleted:n0}, lần này: {newQuantity:n0}.");
                }
            }

            // ✅ Tạo yêu cầu
            var requestId = Guid.NewGuid();
            var requestCode = await _codeGenerator.GenerateOutboundRequestCodeAsync();
            var request = dto.ToEntityCreate(requestId, requestCode, manager.ManagerId);

            await _unitOfWork.WarehouseOutboundRequests.CreateAsync(request);
            await _unitOfWork.SaveChangesAsync();

            await _notificationService.NotifyOutboundRequestCreatedAsync(request.OutboundRequestId, manager.ManagerId);

            return new ServiceResult(Const.SUCCESS_CREATE_CODE, "Tạo yêu cầu xuất kho thành công", request.OutboundRequestId);
        }


        public async Task<IServiceResult> GetAllAsync(Guid userId)
        {
            // Ưu tiên xác định là staff trước
            var staff = await _unitOfWork.BusinessStaffRepository.FindByUserIdAsync(userId);
            if (staff != null && !staff.IsDeleted)
            {
                // Lọc theo supervisorId (tức manager của công ty)
                var allRequests = await _unitOfWork.WarehouseOutboundRequests.GetAllAsync();
                var filtered = allRequests
                    .Where(r => r.RequestedBy == staff.SupervisorId)
                    .ToList();

                if (!filtered.Any())
                    return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không có yêu cầu xuất kho nào thuộc công ty bạn.", new List<WarehouseOutboundRequestListItemDto>());

                return new ServiceResult(Const.SUCCESS_READ_CODE, "Lấy danh sách yêu cầu thành công", filtered.Select(x => x.ToListItemDto()).ToList());
            }

            // Nếu không phải là staff, thử xem có phải manager không
            var manager = await _unitOfWork.BusinessManagerRepository.FindByUserIdAsync(userId);
            if (manager != null && !manager.IsDeleted)
            {
                var allRequests = await _unitOfWork.WarehouseOutboundRequests.GetAllAsync();
                var filtered = allRequests
                    .Where(r => r.RequestedBy == manager.ManagerId)
                    .ToList();

                if (!filtered.Any())
                    return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không có yêu cầu xuất kho nào thuộc công ty bạn.", new List<WarehouseOutboundRequestListItemDto>());

                return new ServiceResult(Const.SUCCESS_READ_CODE, "Lấy danh sách yêu cầu thành công", filtered.Select(x => x.ToListItemDto()).ToList());
            }

            return new ServiceResult(Const.FAIL_READ_CODE, "Không xác định được người dùng.");
        }


        public async Task<IServiceResult> GetDetailAsync(Guid outboundRequestId)
        {
            var request = await _unitOfWork.WarehouseOutboundRequests.GetByIdAsync(outboundRequestId);

            if (request == null || request.IsDeleted)
                return new ServiceResult(Const.FAIL_READ_CODE, "Không tìm thấy yêu cầu xuất kho.");

            return new ServiceResult(Const.SUCCESS_READ_CODE, "Lấy chi tiết yêu cầu thành công", request.ToDetailDto());
        }

        public async Task<IServiceResult> AcceptRequestAsync(Guid requestId, Guid staffUserId)
        {
            var request = await _unitOfWork.WarehouseOutboundRequests.GetByIdAsync(requestId);
            if (request == null || request.Status != WarehouseOutboundRequestStatus.Pending.ToString())
                return new ServiceResult(Const.FAIL_UPDATE_CODE, "Yêu cầu không tồn tại hoặc đã xử lý.");

            var staff = await _unitOfWork.BusinessStaffRepository.FindByUserIdAsync(staffUserId);
            if (staff == null)
                return new ServiceResult(Const.FAIL_UPDATE_CODE, "Không xác định được nhân viên xử lý.");

            var inventory = await _unitOfWork.Inventories.GetByIdAsync(request.InventoryId);
            if (inventory == null || inventory.IsDeleted)
                return new ServiceResult(Const.FAIL_UPDATE_CODE, "Không tìm thấy tồn kho tương ứng.");

            if (request.RequestedQuantity > inventory.Quantity)
            {
                return new ServiceResult(Const.FAIL_UPDATE_CODE,
                    $"Tồn kho hiện tại chỉ còn {inventory.Quantity:n0} {inventory.Unit}, không thể duyệt yêu cầu {request.RequestedQuantity:n0}.");
            }

            // ✅ Kiểm tra hợp đồng nếu có
            if (request.OrderItemId.HasValue)
            {
                var orderItem = await _unitOfWork.OrderItemRepository.GetByIdAsync(
                    predicate: oi =>
                        oi.OrderItemId == request.OrderItemId.Value &&
                        !oi.IsDeleted,
                    asNoTracking: true
                );

                if (orderItem == null)
                    return new ServiceResult(Const.FAIL_UPDATE_CODE, "Không tìm thấy dòng đơn hàng tương ứng.");

                var approvedRequests = await _unitOfWork.WarehouseOutboundRequests.GetAllAsync(
                    predicate: r =>
                        r.OrderItemId == request.OrderItemId.Value &&
                        !r.IsDeleted &&
                        (r.Status == WarehouseOutboundRequestStatus.Accepted.ToString() ||
                         r.Status == WarehouseOutboundRequestStatus.Completed.ToString())
                );

                double totalApproved = approvedRequests.Sum(r => r.RequestedQuantity);
                double allowedQuantity = orderItem.Quantity ?? 0.0;
                double newQuantity = request.RequestedQuantity;

                if (totalApproved + newQuantity > allowedQuantity)
                {
                    return new ServiceResult(Const.ERROR_VALIDATION_CODE,
                        $"Duyệt yêu cầu này sẽ vượt quá giới hạn hợp đồng ({allowedQuantity:n0}). " +
                        $"Đã duyệt: {totalApproved:n0}, lần này: {newQuantity:n0}.");
                }
            }

            // ✅ Cập nhật trạng thái
            request.Status = WarehouseOutboundRequestStatus.Accepted.ToString();
            request.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.WarehouseOutboundRequests.Update(request);
            await _unitOfWork.SaveChangesAsync();

            return new ServiceResult(Const.SUCCESS_UPDATE_CODE, "Đã tiếp nhận yêu cầu xuất kho.");
        }


        public async Task<IServiceResult> CancelRequestAsync(Guid requestId, Guid managerUserId)
        {
            var request = await _unitOfWork.WarehouseOutboundRequests.GetByIdAsync(requestId);
            if (request == null || request.IsDeleted)
                return new ServiceResult(Const.FAIL_UPDATE_CODE, "Không tìm thấy yêu cầu.");

            if (request.Status != WarehouseOutboundRequestStatus.Pending.ToString())
                return new ServiceResult(Const.FAIL_UPDATE_CODE, "Chỉ có thể hủy yêu cầu khi đang ở trạng thái chờ xử lý.");

            var manager = await _unitOfWork.BusinessManagerRepository.FindByUserIdAsync(managerUserId);
            if (manager == null || manager.ManagerId != request.RequestedBy)
                return new ServiceResult(Const.FAIL_UPDATE_CODE, "Bạn không có quyền hủy yêu cầu này.");

            request.Status = WarehouseOutboundRequestStatus.Cancelled.ToString();
            request.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.WarehouseOutboundRequests.Update(request);
            await _unitOfWork.SaveChangesAsync();

            return new ServiceResult(Const.SUCCESS_UPDATE_CODE, "Hủy yêu cầu xuất kho thành công.");
        }
        public async Task<IServiceResult> RejectRequestAsync(Guid requestId, Guid staffUserId, string rejectReason)
        {
            var request = await _unitOfWork.WarehouseOutboundRequests.GetByIdAsync(requestId);
            if (request == null || request.Status != WarehouseOutboundRequestStatus.Pending.ToString())
                return new ServiceResult(Const.FAIL_UPDATE_CODE, "Yêu cầu không tồn tại hoặc đã xử lý.");

            var staff = await _unitOfWork.BusinessStaffRepository.FindByUserIdAsync(staffUserId);
            if (staff == null)
                return new ServiceResult(Const.FAIL_UPDATE_CODE, "Không xác định được nhân viên xử lý.");

            // ✅ Ghi đè lý do từ chối vào field Reason
            request.Status = WarehouseOutboundRequestStatus.Rejected.ToString();
            request.Reason = rejectReason;
            request.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.WarehouseOutboundRequests.Update(request);
            await _unitOfWork.SaveChangesAsync();

            return new ServiceResult(Const.SUCCESS_UPDATE_CODE, "Đã từ chối yêu cầu xuất kho.");
        }


    }
}
