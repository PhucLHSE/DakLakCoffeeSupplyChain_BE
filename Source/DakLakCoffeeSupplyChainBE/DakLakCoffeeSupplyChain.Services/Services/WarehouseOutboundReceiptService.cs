using DakLakCoffeeSupplyChain.Common.DTOs.WarehouseOutboundReceiptDTOs;
using DakLakCoffeeSupplyChain.Common.Enum.WarehouseOutboundRequestEnums;
using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Repositories.Models;
using DakLakCoffeeSupplyChain.Repositories.UnitOfWork;
using DakLakCoffeeSupplyChain.Services.Base;
using DakLakCoffeeSupplyChain.Services.IServices;
using System;
using System.Threading.Tasks;
using DakLakCoffeeSupplyChain.Services.Mappers;
using System.Linq;
using System.Collections.Generic;

namespace DakLakCoffeeSupplyChain.Services.Services
{
    public class WarehouseOutboundReceiptService : IWarehouseOutboundReceiptService
    {
        private readonly IUnitOfWork _unitOfWork;

        public WarehouseOutboundReceiptService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IServiceResult> GetAllAsync(Guid userId)
        {
            var staff = await _unitOfWork.BusinessStaffRepository.FindByUserIdAsync(userId);
            if (staff == null || staff.IsDeleted)
                return new ServiceResult(Const.FAIL_READ_CODE, "Không xác định được nhân viên.");

            var receipts = await _unitOfWork.WarehouseOutboundReceipts.GetAllWithIncludesAsync();
            var filtered = receipts
                .Where(r => r.Warehouse?.ManagerId == staff.SupervisorId)
                .ToList();

            if (!filtered.Any())
                return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không có phiếu xuất kho nào thuộc công ty bạn.", new List<WarehouseOutboundReceiptListItemDto>());

            var result = filtered.Select(r => r.ToListItemDto()).ToList();
            return new ServiceResult(Const.SUCCESS_READ_CODE, "Lấy danh sách phiếu xuất kho thành công", result);
        }

        public async Task<IServiceResult> GetByIdAsync(Guid receiptId, Guid userId)
        {
            var staff = await _unitOfWork.BusinessStaffRepository.FindByUserIdAsync(userId);
            if (staff == null || staff.IsDeleted)
                return new ServiceResult(Const.FAIL_READ_CODE, "Không xác định được nhân viên.");

            var receipt = await _unitOfWork.WarehouseOutboundReceipts.GetDetailByIdAsync(receiptId);
            if (receipt == null)
                return new ServiceResult(Const.FAIL_READ_CODE, "Không tìm thấy phiếu xuất kho.");

            if (receipt.Warehouse?.ManagerId != staff.SupervisorId)
                return new ServiceResult(Const.FAIL_READ_CODE, "Bạn không có quyền truy cập phiếu xuất kho này.");

            return new ServiceResult(Const.SUCCESS_READ_CODE, "Lấy chi tiết phiếu xuất kho thành công", receipt.ToDetailDto());
        }


        public async Task<IServiceResult> CreateAsync(Guid staffUserId, WarehouseOutboundReceiptCreateDto dto)
        {
            var staff = await _unitOfWork.BusinessStaffRepository.FindByUserIdAsync(staffUserId);
            if (staff == null)
                return new ServiceResult(Const.FAIL_UPDATE_CODE, "Không tìm thấy nhân viên.");

            var request = await _unitOfWork.WarehouseOutboundRequests.GetByIdAsync(dto.OutboundRequestId);
            if (request == null)
                return new ServiceResult(Const.FAIL_UPDATE_CODE, "Yêu cầu xuất kho không tồn tại.");

            if (dto.WarehouseId != request.WarehouseId)
                return new ServiceResult(Const.ERROR_VALIDATION_CODE, "Kho được chọn không khớp với yêu cầu xuất kho.");
            if (dto.InventoryId != request.InventoryId)
                return new ServiceResult(Const.ERROR_VALIDATION_CODE, "Tồn kho được chọn không khớp với yêu cầu xuất kho.");

            var warehouse = await _unitOfWork.Warehouses.GetByIdAsync(dto.WarehouseId);
            if (warehouse == null || warehouse.ManagerId != staff.SupervisorId)
                return new ServiceResult(Const.FAIL_UPDATE_CODE, "Bạn không có quyền tạo phiếu xuất cho kho này.");

            if (request.Status != WarehouseOutboundRequestStatus.Accepted.ToString())
                return new ServiceResult(Const.FAIL_UPDATE_CODE, "Yêu cầu chưa được tiếp nhận hoặc đã xử lý.");

            var inventory = await _unitOfWork.Inventories.FindByIdAsync(dto.InventoryId);
            if (inventory == null)
                return new ServiceResult(Const.FAIL_UPDATE_CODE, "Tồn kho không tồn tại.");

            if (dto.ExportedQuantity <= 0)
                return new ServiceResult(Const.ERROR_VALIDATION_CODE, "Số lượng xuất phải lớn hơn 0.");

            // CHỐT: chỉ trừ phần đã CONFIRMED
            var existingReceipts = await _unitOfWork.WarehouseOutboundReceipts
                .GetAllAsync(r => r.OutboundRequestId == dto.OutboundRequestId && !r.IsDeleted);

            double confirmedSoFar = existingReceipts
                .SelectMany(r => ParseConfirmedFromNote(r.Note))
                .Sum();

            double remainingOfRequest = request.RequestedQuantity - confirmedSoFar;
            if (dto.ExportedQuantity > remainingOfRequest)
                return new ServiceResult(Const.ERROR_VALIDATION_CODE,
                    $"Số lượng vượt quá phần còn lại của yêu cầu: {remainingOfRequest:n0}kg.");

            if (dto.ExportedQuantity > inventory.Quantity)
                return new ServiceResult(Const.ERROR_VALIDATION_CODE,
                    $"Tồn kho không đủ. Còn {inventory.Quantity:n0} {inventory.Unit}.");

            var outboundReceiptId = Guid.NewGuid();
            var receiptCode = "WOR-" + outboundReceiptId.ToString("N")[..8];
            var receipt = dto.MapFromCreateDto(outboundReceiptId, receiptCode, staff.StaffId, inventory.BatchId);

            await _unitOfWork.WarehouseOutboundReceipts.CreateAsync(receipt);
            await _unitOfWork.SaveChangesAsync();

            return new ServiceResult(Const.SUCCESS_CREATE_CODE, "Tạo phiếu xuất kho thành công", receipt.OutboundReceiptId);
        }


        public async Task<IServiceResult> ConfirmReceiptAsync(Guid receiptId, WarehouseOutboundReceiptConfirmDto dto)
        {
            var receipt = await _unitOfWork.WarehouseOutboundReceipts.GetByIdWithoutIncludesAsync(receiptId);
            if (receipt == null)
                return new ServiceResult(Const.FAIL_READ_CODE, "Không tìm thấy phiếu xuất kho.");

            var warehouse = await _unitOfWork.Warehouses.GetByIdAsync(receipt.WarehouseId);
            if (warehouse == null)
                return new ServiceResult(Const.FAIL_READ_CODE, "Không tìm thấy kho.");

            var inventory = await _unitOfWork.Inventories.FindByIdAsync(receipt.InventoryId);
            if (inventory == null)
                return new ServiceResult(Const.FAIL_READ_CODE, "Không tìm thấy tồn kho tương ứng.");

            var request = await _unitOfWork.WarehouseOutboundRequests.GetByIdWithoutIncludesAsync(receipt.OutboundRequestId);
            if (request == null)
                return new ServiceResult(Const.FAIL_READ_CODE, "Không tìm thấy yêu cầu xuất kho.");

            if (receipt.ExportedBy == null)
                return new ServiceResult(Const.FAIL_READ_CODE, "Không xác định người xuất kho.");

            var staff = await _unitOfWork.BusinessStaffRepository.GetByIdAsync(receipt.ExportedBy);
            if (staff == null || staff.SupervisorId != warehouse.ManagerId)
                return new ServiceResult(Const.FAIL_READ_CODE, "Bạn không có quyền xác nhận phiếu này.");

            if (dto.ConfirmedQuantity <= 0)
                return new ServiceResult(Const.ERROR_VALIDATION_CODE, "Số lượng xác nhận phải lớn hơn 0.");

            // Đã confirmed bao nhiêu trên chính receipt này?
            var confirmedOnThisReceipt = ParseConfirmedFromNote(receipt.Note).Sum();
            var remainingOnThisReceipt = receipt.Quantity - confirmedOnThisReceipt;
            if (dto.ConfirmedQuantity > remainingOnThisReceipt)
                return new ServiceResult(Const.ERROR_VALIDATION_CODE,
                    $"Xác nhận vượt quá phần còn lại của phiếu ({remainingOnThisReceipt:n0}kg).");

            // Tổng confirmed của toàn request trước khi xác nhận lần này
            var allReceipts = await _unitOfWork.WarehouseOutboundReceipts
                .GetAllAsync(r => r.OutboundRequestId == request.OutboundRequestId && !r.IsDeleted);

            double totalConfirmedSoFar = allReceipts
                .SelectMany(r => ParseConfirmedFromNote(r.Note))
                .Sum();

            double totalAfterThis = totalConfirmedSoFar + dto.ConfirmedQuantity;
            if (totalAfterThis > request.RequestedQuantity)
                return new ServiceResult(Const.ERROR_VALIDATION_CODE,
                    $"Tổng xác nhận sẽ vượt yêu cầu ({request.RequestedQuantity:n0}kg).");

            // Nếu request gắn OrderItem → ràng buộc theo OrderItem
            if (request.OrderItemId.HasValue)
            {
                var orderItemId = request.OrderItemId.Value;
                var orderItem = await _unitOfWork.OrderItemRepository.GetByIdAsync(orderItemId);
                if (orderItem == null)
                    return new ServiceResult(Const.FAIL_READ_CODE, "Không tìm thấy dòng đơn hàng tương ứng.");

                var receiptsByOrderItem = await _unitOfWork.WarehouseOutboundReceipts.GetByOrderItemIdAsync(orderItemId);
                var totalConfirmedOrderItem = receiptsByOrderItem
                    .SelectMany(r => ParseConfirmedFromNote(r.Note))
                    .Sum();

                var afterThisOrderItem = totalConfirmedOrderItem + dto.ConfirmedQuantity;
                var allowedQuantity = orderItem.Quantity ?? 0.0;
                if (afterThisOrderItem > allowedQuantity)
                    return new ServiceResult(Const.ERROR_VALIDATION_CODE,
                        $"Tổng lượng xác nhận cho dòng đơn sẽ vượt mức ({allowedQuantity:n0}kg).");
            }

            if (dto.ConfirmedQuantity > inventory.Quantity)
                return new ServiceResult(Const.ERROR_VALIDATION_CODE,
                    $"Tồn kho không đủ. Chỉ còn {inventory.Quantity:n0} {inventory.Unit}.");

            // Trừ tồn kho + log
            inventory.Quantity -= dto.ConfirmedQuantity;
            inventory.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Inventories.Update(inventory);

            var log = receipt.ToInventoryLogFromOutbound(inventory.InventoryId, dto.ConfirmedQuantity);
            await _unitOfWork.InventoryLogs.CreateAsync(log);

            // Append tag xác nhận vào Note
            receipt.UpdateAfterConfirm(dto.ConfirmedQuantity, dto.DestinationNote);
            _unitOfWork.WarehouseOutboundReceipts.Update(receipt);

            // Update trạng thái request
            if (Math.Abs((request.RequestedQuantity - totalAfterThis)) < 1e-9)
                request.MarkAsCompleted();
            else
                request.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.WarehouseOutboundRequests.Update(request);

            await _unitOfWork.SaveChangesAsync();

            return new ServiceResult(Const.SUCCESS_UPDATE_CODE, "Xác nhận phiếu xuất kho thành công.", receipt.OutboundReceiptId);
        }

        // Helper: parse các đoạn [CONFIRMED:x] trong Note để lấy list double
        private IEnumerable<double> ParseConfirmedFromNote(string? note)
        {
            if (string.IsNullOrWhiteSpace(note)) yield break;

            // Tách theo token
            var tokens = note.Split("[CONFIRMED:", StringSplitOptions.RemoveEmptyEntries);
            // Bỏ phần trước token đầu tiên (Skip(1))
            foreach (var token in tokens.Skip(1))
            {
                var end = token.IndexOf(']');
                if (end <= 0) continue;

                var valStr = token.Substring(0, end).Trim();
                if (double.TryParse(valStr, out var val))
                    yield return val;
            }
        }
        // Service (trong WarehouseOutboundRequestsService hoặc ReceiptService)
        public async Task<IServiceResult> GetSummaryAsync(Guid requestId)
        {
            var req = await _unitOfWork.WarehouseOutboundRequests.GetByIdAsync(requestId);
            if (req == null) return new ServiceResult(Const.FAIL_READ_CODE, "Không tìm thấy yêu cầu.");

            var receipts = await _unitOfWork.WarehouseOutboundReceipts
                .GetAllAsync(r => r.OutboundRequestId == requestId && !r.IsDeleted);

            double confirmed = receipts.SelectMany(r => ParseConfirmedFromNote(r.Note)).Sum();
            double created = receipts.Sum(r => r.Quantity);
            double draft = Math.Max(0, created - confirmed);

            double remainingByConfirm = Math.Max(0, req.RequestedQuantity - confirmed);
            double remainingHardCap = Math.Max(0, req.RequestedQuantity - created);

            var inv = await _unitOfWork.Inventories.FindByIdAsync(req.InventoryId);
            double inventoryAvailable = inv?.Quantity ?? 0;

            return new ServiceResult(Const.SUCCESS_READ_CODE, "OK", new
            {
                requestedQuantity = req.RequestedQuantity,
                confirmedQuantity = confirmed,
                createdQuantity = created,
                draftQuantity = draft,
                remainingByConfirm,
                remainingHardCap,
                inventoryAvailable
            });
        }

    }
}
