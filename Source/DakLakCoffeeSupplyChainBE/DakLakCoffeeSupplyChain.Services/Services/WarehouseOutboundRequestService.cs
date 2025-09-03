using DakLakCoffeeSupplyChain.Common.DTOs.WarehouseOutboundRequestDTOs;
using DakLakCoffeeSupplyChain.Common.Enum.WarehouseOutboundRequestEnums;
using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Repositories.Models;
using DakLakCoffeeSupplyChain.Repositories.UnitOfWork;
using DakLakCoffeeSupplyChain.Services.Base;
using DakLakCoffeeSupplyChain.Services.IServices;
using DakLakCoffeeSupplyChain.Services.Generators;
using DakLakCoffeeSupplyChain.Services.Mappers;
using Microsoft.EntityFrameworkCore;
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

        public async Task<IServiceResult> CreateRequestAsync(Guid managerUserId, WarehouseOutboundRequestCreateDto dto)
        {
            var manager = await _unitOfWork.BusinessManagerRepository.FindByUserIdAsync(managerUserId);
            if (manager == null)
                return new ServiceResult(Const.FAIL_READ_CODE, "Không tìm thấy người dùng yêu cầu.");

            var inventory = await _unitOfWork.Inventories.GetDetailByIdAsync(dto.InventoryId);
            if (inventory == null || inventory.IsDeleted)
                return new ServiceResult(Const.FAIL_READ_CODE, "Không tìm thấy thông tin tồn kho.");

            if (inventory.WarehouseId != dto.WarehouseId)
                return new ServiceResult(Const.ERROR_VALIDATION_CODE, "Tồn kho không thuộc kho được chọn.");

            if (dto.RequestedQuantity <= 0)
                return new ServiceResult(Const.ERROR_VALIDATION_CODE, "Số lượng yêu cầu phải lớn hơn 0.");

            // ✅ Kiểm tra tồn khả dụng = tồn hiện tại - tổng Requested của các request Accepted chưa Completed
            var acceptedSameInventory = await _unitOfWork.WarehouseOutboundRequests.GetAllAsync(
                r => r.InventoryId == dto.InventoryId
                     && !r.IsDeleted
                     && r.Status == WarehouseOutboundRequestStatus.Accepted.ToString());
            var reserved = acceptedSameInventory.Sum(r => r.RequestedQuantity);
            var available = inventory.Quantity - reserved;
            
            if (dto.RequestedQuantity > available)
                return new ServiceResult(Const.ERROR_VALIDATION_CODE,
                    $"Tồn khả dụng chỉ còn {available:n0} {inventory.Unit}, không thể yêu cầu xuất {dto.RequestedQuantity:n0}.");

            // Nếu gắn OrderItem → kiểm CoffeeType & hạn mức dựa trên receipts CONFIRMED
            if (dto.OrderItemId.HasValue)
            {
                var orderItem = await _unitOfWork.OrderItemRepository.GetByIdAsync(
                    predicate: oi => oi.OrderItemId == dto.OrderItemId.Value && !oi.IsDeleted,
                    asNoTracking: true
                );
                if (orderItem == null)
                    return new ServiceResult(Const.FAIL_CREATE_CODE, "Không tìm thấy dòng đơn hàng tương ứng.");

                var product = await _unitOfWork.ProductRepository.GetByIdAsync(orderItem.ProductId);
                if (product == null)
                    return new ServiceResult(Const.FAIL_CREATE_CODE, "Không tìm thấy sản phẩm tương ứng trong đơn hàng.");

                var productCoffeeTypeId = product.CoffeeTypeId;
                var inventoryCoffeeTypeId = inventory.Batch?.CoffeeTypeId;
                if (inventoryCoffeeTypeId == null)
                    return new ServiceResult(Const.FAIL_CREATE_CODE, "Không xác định được loại cà phê trong tồn kho.");

                if (productCoffeeTypeId != inventoryCoffeeTypeId)
                    return new ServiceResult(Const.ERROR_VALIDATION_CODE, "Loại cà phê trong tồn kho không khớp với sản phẩm trong đơn hàng.");

                // ✅ Hạn mức theo confirmed receipts + accepted requests (chưa completed)
                var receiptsByOrderItem = await _unitOfWork.WarehouseOutboundReceipts.GetByOrderItemIdAsync(orderItem.OrderItemId);
                double totalConfirmedOrderItem = receiptsByOrderItem
                    .SelectMany(r => ParseConfirmedFromNote(r.Note))
                    .Sum();

                var acceptedSameOrderItem = await _unitOfWork.WarehouseOutboundRequests.GetAllAsync(
                    r => r.OrderItemId == dto.OrderItemId
                         && !r.IsDeleted
                         && r.Status == WarehouseOutboundRequestStatus.Accepted.ToString());
                double totalAcceptedOutstanding = acceptedSameOrderItem.Sum(r => r.RequestedQuantity);

                var allowedQuantity = orderItem.Quantity ?? 0.0;
                if (totalConfirmedOrderItem + totalAcceptedOutstanding + dto.RequestedQuantity > allowedQuantity)
                {
                    return new ServiceResult(Const.ERROR_VALIDATION_CODE,
                        $"Số lượng yêu cầu vượt quá giới hạn đơn hàng ({allowedQuantity:n0}). Đã xác nhận: {totalConfirmedOrderItem:n0}, đã duyệt khác: {totalAcceptedOutstanding:n0}, yêu cầu mới: {dto.RequestedQuantity:n0}.");
                }
            }

            // Tạo yêu cầu
            var requestId = Guid.NewGuid();
            var requestCode = await _codeGenerator.GenerateOutboundRequestCodeAsync();
            var request = dto.ToEntityCreate(requestId, requestCode, manager.ManagerId);

            await _unitOfWork.WarehouseOutboundRequests.CreateAsync(request);
            await _unitOfWork.SaveChangesAsync();

             _notificationService.NotifyOutboundRequestCreatedAsync(request.OutboundRequestId, manager.ManagerId);

            return new ServiceResult(Const.SUCCESS_CREATE_CODE, "Tạo yêu cầu xuất kho thành công", request.OutboundRequestId);
        }

        // ✅ Method mới để tính toán số lượng còn lại cho order items
        private async Task<double> CalculateRemainingQuantityForOrderItemAsync(Guid orderItemId)
        {
            var orderItem = await _unitOfWork.OrderItemRepository.GetByIdAsync(
                predicate: oi => oi.OrderItemId == orderItemId && !oi.IsDeleted,
                asNoTracking: true
            );
            
            if (orderItem == null) return 0;

            var totalQuantity = orderItem.Quantity ?? 0.0;
            
            // Lấy tất cả receipts đã xác nhận cho order item này
            var receiptsByOrderItem = await _unitOfWork.WarehouseOutboundReceipts.GetByOrderItemIdAsync(orderItemId);
            double totalConfirmedQuantity = receiptsByOrderItem
                .SelectMany(r => ParseConfirmedFromNote(r.Note))
                .Sum();

            // Số lượng còn lại = Tổng số lượng - Đã xác nhận
            return Math.Max(0, totalQuantity - totalConfirmedQuantity);
        }

        // ✅ Method mới để lấy order items với thông tin số lượng còn lại
        public async Task<IServiceResult> GetOrderItemsWithRemainingQuantityAsync(Guid orderId, Guid userId)
        {
            var manager = await _unitOfWork.BusinessManagerRepository.FindByUserIdAsync(userId);
            if (manager == null)
                return new ServiceResult(Const.FAIL_READ_CODE, "Không tìm thấy người dùng yêu cầu.");

            // Tìm order với kiểm tra quyền truy cập theo công ty
            var order = await _unitOfWork.OrderRepository.GetByIdAsync(
                predicate: o => o.OrderId == orderId && !o.IsDeleted,
                include: query => query
                    .Include(o => o.DeliveryBatch)
                        .ThenInclude(db => db.Contract),
                asNoTracking: true
            );
            
            if (order == null)
                return new ServiceResult(Const.FAIL_READ_CODE, "Không tìm thấy đơn hàng.");

            // Kiểm tra quyền truy cập: order phải thuộc về công ty của manager
            if (order.DeliveryBatch?.Contract?.SellerId != manager.ManagerId)
                return new ServiceResult(Const.FAIL_READ_CODE, "Bạn không có quyền truy cập đơn hàng này.");

            var orderItems = await _unitOfWork.OrderItemRepository.GetAllAsync(
                predicate: oi => oi.OrderId == orderId && !oi.IsDeleted,
                include: query => query.Include(oi => oi.Product).ThenInclude(p => p.CoffeeType),
                asNoTracking: true
            );

            var result = new List<object>();
            foreach (var item in orderItems)
            {
                var remainingQuantity = await CalculateRemainingQuantityForOrderItemAsync(item.OrderItemId);
                var totalQuantity = item.Quantity ?? 0.0;
                var confirmedQuantity = totalQuantity - remainingQuantity;

                result.Add(new
                {
                    orderItemId = item.OrderItemId,
                    productName = item.Product?.ProductName ?? "Không xác định",
                    totalQuantity = totalQuantity,
                    confirmedQuantity = confirmedQuantity,
                    remainingQuantity = remainingQuantity,
                    coffeeTypeName = item.Product?.CoffeeType?.TypeName ?? "Không xác định"
                });
            }

            return new ServiceResult(Const.SUCCESS_READ_CODE, "Lấy danh sách mục hàng với số lượng còn lại thành công", result);
        }

                public async Task<IServiceResult> GetAllAsync(Guid userId, int page = 1, int pageSize = 20)
        {
            var staff = await _unitOfWork.BusinessStaffRepository.FindByUserIdAsync(userId);
            if (staff != null && !staff.IsDeleted)
            {
                // ✅ Tối ưu: Thêm pagination và chỉ load data cần thiết
                var query = _unitOfWork.WarehouseOutboundRequests.GetAllQueryable()
                    .Where(r => r.RequestedBy == staff.SupervisorId && !r.IsDeleted)
                    .Include(r => r.Warehouse)
                    .Include(r => r.Inventory)
                        .ThenInclude(inv => inv.Batch)
                    .Include(r => r.Inventory)
                        .ThenInclude(inv => inv.Detail)
                    .OrderByDescending(r => r.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize);

                var filtered = await query.ToListAsync();
                if (!filtered.Any())
                    return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không có yêu cầu xuất kho nào thuộc công ty bạn.", new List<WarehouseOutboundRequestListItemDto>());
                return new ServiceResult(Const.SUCCESS_READ_CODE, "Lấy danh sách yêu cầu thành công", filtered.Select(x => x.ToListItemDto()).ToList());
            }

            var manager = await _unitOfWork.BusinessManagerRepository.FindByUserIdAsync(userId);
            if (manager != null && !manager.IsDeleted)
            {
                // ✅ Tối ưu: Thêm pagination và chỉ load data cần thiết
                var query = _unitOfWork.WarehouseOutboundRequests.GetAllQueryable()
                    .Where(r => r.RequestedBy == manager.ManagerId && !r.IsDeleted)
                    .Include(r => r.Warehouse)
                    .Include(r => r.Inventory)
                        .ThenInclude(inv => inv.Batch)
                    .Include(r => r.Inventory)
                        .ThenInclude(inv => inv.Detail)
                    .OrderByDescending(r => r.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize);

                var filtered = await query.ToListAsync();
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

            // Quyền theo kho
            var warehouse = await _unitOfWork.Warehouses.GetByIdAsync(inventory.WarehouseId);
            if (warehouse?.ManagerId != staff.SupervisorId)
                return new ServiceResult(Const.FAIL_UPDATE_CODE, "Bạn không có quyền duyệt yêu cầu cho kho này.");

            // ✅ Tồn khả dụng = tồn hiện tại - tổng Requested của các request Accepted chưa Completed cùng Inventory
            // (Bao gồm cả các phiếu xuất kho đã tạo nhưng chưa trừ tồn kho)
            var acceptedSameInventory = await _unitOfWork.WarehouseOutboundRequests.GetAllAsync(
                r => r.InventoryId == request.InventoryId
                     && !r.IsDeleted
                     && r.Status == WarehouseOutboundRequestStatus.Accepted.ToString());
            var reserved = acceptedSameInventory.Sum(r => r.RequestedQuantity);
            var available = inventory.Quantity - reserved;
            if (request.RequestedQuantity > available)
                return new ServiceResult(Const.FAIL_UPDATE_CODE,
                    $"Tồn khả dụng chỉ còn {available:n0} {inventory.Unit}.");

            // Nếu gắn OrderItem → kiểm hạn mức: confirmed + accepted (kể cả request này) ≤ OrderItem.Quantity
            if (request.OrderItemId.HasValue)
            {
                var orderItem = await _unitOfWork.OrderItemRepository.GetByIdAsync(
                    predicate: oi => oi.OrderItemId == request.OrderItemId.Value && !oi.IsDeleted,
                    asNoTracking: true
                );
                if (orderItem == null)
                    return new ServiceResult(Const.FAIL_UPDATE_CODE, "Không tìm thấy dòng đơn hàng tương ứng.");

                var receiptsByOrderItem = await _unitOfWork.WarehouseOutboundReceipts.GetByOrderItemIdAsync(orderItem.OrderItemId);
                double totalConfirmedOrderItem = receiptsByOrderItem
                    .SelectMany(r => ParseConfirmedFromNote(r.Note))
                    .Sum();

                var acceptedSameOrderItem = await _unitOfWork.WarehouseOutboundRequests.GetAllAsync(
                    r => r.OrderItemId == request.OrderItemId
                         && !r.IsDeleted
                         && r.Status == WarehouseOutboundRequestStatus.Accepted.ToString());
                double totalAcceptedOutstanding = acceptedSameOrderItem.Sum(r => r.RequestedQuantity);

                var afterAccept = totalConfirmedOrderItem + totalAcceptedOutstanding + request.RequestedQuantity;
                var allowedQuantity = orderItem.Quantity ?? 0.0;

                if (afterAccept > allowedQuantity)
                    return new ServiceResult(Const.ERROR_VALIDATION_CODE,
                        $"Duyệt yêu cầu này sẽ vượt quá hạn mức của dòng đơn ({allowedQuantity:n0}). Đã xác nhận: {totalConfirmedOrderItem:n0}, đã duyệt khác: {totalAcceptedOutstanding:n0}, lần này: {request.RequestedQuantity:n0}.");
            }

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

            request.Status = WarehouseOutboundRequestStatus.Rejected.ToString();
            request.Reason = rejectReason;
            request.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.WarehouseOutboundRequests.Update(request);
            await _unitOfWork.SaveChangesAsync();

            return new ServiceResult(Const.SUCCESS_UPDATE_CODE, "Đã từ chối yêu cầu xuất kho.");
        }

        // Helper parse [CONFIRMED:x] như bên ReceiptService
        private IEnumerable<double> ParseConfirmedFromNote(string? note)
        {
            if (string.IsNullOrWhiteSpace(note)) yield break;

            var tokens = note.Split("[CONFIRMED:", StringSplitOptions.RemoveEmptyEntries);
            foreach (var token in tokens.Skip(1))
            {
                var end = token.IndexOf(']');
                if (end <= 0) continue;
                var valStr = token.Substring(0, end).Trim();
                if (double.TryParse(valStr, out var val))
                    yield return val;
            }
        }
    }
}
