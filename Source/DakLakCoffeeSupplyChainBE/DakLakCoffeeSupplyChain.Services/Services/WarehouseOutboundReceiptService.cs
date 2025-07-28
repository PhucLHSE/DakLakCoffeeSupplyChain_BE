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

        //public async Task<IServiceResult> CreateAsync(Guid staffUserId, WarehouseOutboundReceiptCreateDto dto)
        //{
        //    var staff = await _unitOfWork.BusinessStaffRepository.FindByUserIdAsync(staffUserId);
        //    if (staff == null)
        //        return new ServiceResult(Const.FAIL_UPDATE_CODE, "Không tìm thấy nhân viên.");

        //    var request = await _unitOfWork.WarehouseOutboundRequests.GetByIdAsync(dto.OutboundRequestId);
        //    if (request == null)
        //        return new ServiceResult(Const.FAIL_UPDATE_CODE, "Yêu cầu xuất kho không tồn tại.");

        //    var warehouse = await _unitOfWork.Warehouses.GetByIdAsync(request.WarehouseId);
        //    if (warehouse == null || warehouse.ManagerId != staff.SupervisorId)
        //        return new ServiceResult(Const.FAIL_UPDATE_CODE, "Bạn không có quyền tạo phiếu xuất cho kho này.");

        //    if (request.Status != WarehouseOutboundRequestStatus.Accepted.ToString())
        //        return new ServiceResult(Const.FAIL_UPDATE_CODE, "Yêu cầu chưa được tiếp nhận hoặc đã xử lý.");

        //    var existing = await _unitOfWork.WarehouseOutboundReceipts.GetByOutboundRequestIdAsync(dto.OutboundRequestId);
        //    if (existing != null)
        //        return new ServiceResult(Const.FAIL_CREATE_CODE, "Yêu cầu này đã có phiếu xuất.");

        //    var inventory = await _unitOfWork.Inventories.FindByIdAsync(request.InventoryId);
        //    if (inventory == null)
        //        return new ServiceResult(Const.FAIL_UPDATE_CODE, "Tồn kho không tồn tại.");

        //    if (dto.ExportedQuantity != request.RequestedQuantity)
        //    {
        //        return new ServiceResult(Const.ERROR_VALIDATION_CODE,
        //            $"Số lượng xuất ({dto.ExportedQuantity}kg) không khớp với yêu cầu ({request.RequestedQuantity}kg).");
        //    }

        //    if (dto.ExportedQuantity > inventory.Quantity)
        //    {
        //        return new ServiceResult(Const.ERROR_VALIDATION_CODE,
        //            $"Tồn kho không đủ. Còn {inventory.Quantity}kg, yêu cầu xuất {dto.ExportedQuantity}kg.");
        //    }

        //    // ✅ Dùng mapper để tạo entity
        //    var outboundReceiptId = Guid.NewGuid();
        //    var receiptCode = "WOR-" + outboundReceiptId.ToString("N")[..8];
        //    var receipt = dto.MapFromCreateDto(
        //        outboundReceiptId: outboundReceiptId,
        //        receiptCode: receiptCode,
        //        staffId: staff.StaffId,
        //        batchId: inventory.BatchId
        //    );

        //    await _unitOfWork.WarehouseOutboundReceipts.CreateAsync(receipt);
        //    await _unitOfWork.SaveChangesAsync();

        //    return new ServiceResult(Const.SUCCESS_CREATE_CODE, "Tạo phiếu xuất kho thành công", receipt.OutboundReceiptId);
        //}
        public async Task<IServiceResult> CreateAsync(Guid staffUserId, WarehouseOutboundReceiptCreateDto dto)
        {
            var staff = await _unitOfWork.BusinessStaffRepository.FindByUserIdAsync(staffUserId);
            if (staff == null)
                return new ServiceResult(Const.FAIL_UPDATE_CODE, "Không tìm thấy nhân viên.");

            var request = await _unitOfWork.WarehouseOutboundRequests.GetByIdAsync(dto.OutboundRequestId);
            if (request == null)
                return new ServiceResult(Const.FAIL_UPDATE_CODE, "Yêu cầu xuất kho không tồn tại.");

            // ✅ Kiểm tra kho được chọn từ FE có khớp với yêu cầu không
            if (dto.WarehouseId != request.WarehouseId)
                return new ServiceResult(Const.ERROR_VALIDATION_CODE, "Kho được chọn không khớp với yêu cầu xuất kho.");

            // ✅ Kiểm tra tồn kho được chọn từ FE có khớp với yêu cầu không
            if (dto.InventoryId != request.InventoryId)
                return new ServiceResult(Const.ERROR_VALIDATION_CODE, "Tồn kho được chọn không khớp với yêu cầu xuất kho.");

            var warehouse = await _unitOfWork.Warehouses.GetByIdAsync(dto.WarehouseId);
            if (warehouse == null || warehouse.ManagerId != staff.SupervisorId)
                return new ServiceResult(Const.FAIL_UPDATE_CODE, "Bạn không có quyền tạo phiếu xuất cho kho này.");

            if (request.Status != WarehouseOutboundRequestStatus.Accepted.ToString())
                return new ServiceResult(Const.FAIL_UPDATE_CODE, "Yêu cầu chưa được tiếp nhận hoặc đã xử lý.");

            var existing = await _unitOfWork.WarehouseOutboundReceipts.GetByOutboundRequestIdAsync(dto.OutboundRequestId);
            if (existing != null)
                return new ServiceResult(Const.FAIL_CREATE_CODE, "Yêu cầu này đã có phiếu xuất.");

            var inventory = await _unitOfWork.Inventories.FindByIdAsync(dto.InventoryId);
            if (inventory == null)
                return new ServiceResult(Const.FAIL_UPDATE_CODE, "Tồn kho không tồn tại.");

            if (dto.ExportedQuantity != request.RequestedQuantity)
            {
                return new ServiceResult(Const.ERROR_VALIDATION_CODE,
                    $"Số lượng xuất ({dto.ExportedQuantity}kg) không khớp với yêu cầu ({request.RequestedQuantity}kg).");
            }

            if (dto.ExportedQuantity > inventory.Quantity)
            {
                return new ServiceResult(Const.ERROR_VALIDATION_CODE,
                    $"Tồn kho không đủ. Còn {inventory.Quantity}kg, yêu cầu xuất {dto.ExportedQuantity}kg.");
            }

            // ✅ Tạo mã phiếu và mapping entity
            var outboundReceiptId = Guid.NewGuid();
            var receiptCode = "WOR-" + outboundReceiptId.ToString("N")[..8];
            var receipt = dto.MapFromCreateDto(
                outboundReceiptId: outboundReceiptId,
                receiptCode: receiptCode,
                staffId: staff.StaffId,
                batchId: inventory.BatchId
            );

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
            // ✅ Kiểm tra OrderItem nếu có
            if (request.OrderItemId.HasValue)
            {
                var orderItemId = request.OrderItemId.Value;
                var orderItem = await _unitOfWork.OrderItemRepository.GetByIdAsync(orderItemId);
                if (orderItem == null)
                    return new ServiceResult(Const.FAIL_READ_CODE, "Không tìm thấy dòng đơn hàng tương ứng.");

                var relatedReceipts = await _unitOfWork.WarehouseOutboundReceipts.GetByOrderItemIdAsync(orderItemId);
                var totalConfirmed = relatedReceipts
        .Where(r => !string.IsNullOrWhiteSpace(r.Note) && r.Note.StartsWith("Xác nhận"))
        .Sum(r => r.Quantity);
                var afterThis = totalConfirmed + dto.ConfirmedQuantity;

                if (afterThis > orderItem.Quantity)
                {
                    return new ServiceResult(Const.ERROR_VALIDATION_CODE,
                        $"Tổng lượng xuất sau khi xác nhận ({afterThis:n0}kg) vượt quá số lượng đơn hàng ({orderItem.Quantity:n0}kg).");
                }
            }
            if (dto.ConfirmedQuantity > request.RequestedQuantity)
            {
                return new ServiceResult(Const.ERROR_VALIDATION_CODE,
                    $"Số lượng xác nhận ({dto.ConfirmedQuantity}kg) vượt quá yêu cầu ({request.RequestedQuantity}kg).");
            }

            if (dto.ConfirmedQuantity > receipt.Quantity)
            {
                return new ServiceResult(Const.ERROR_VALIDATION_CODE,
                    $"Xác nhận vượt quá số lượng ghi nhận ({receipt.Quantity}kg).");
            }

            if (dto.ConfirmedQuantity > inventory.Quantity)
            {
                return new ServiceResult(Const.ERROR_VALIDATION_CODE,
                    $"Tồn kho không đủ. Chỉ còn {inventory.Quantity}kg.");
            }

            // ✅ Cập nhật tồn kho
            inventory.Quantity -= dto.ConfirmedQuantity;
            inventory.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Inventories.Update(inventory);

            // ✅ Ghi log xuất kho qua mapper
            var log = receipt.ToInventoryLogFromOutbound(inventory.InventoryId, dto.ConfirmedQuantity);
            await _unitOfWork.InventoryLogs.CreateAsync(log);

            // ✅ Cập nhật phiếu xuất & yêu cầu qua mapper
            receipt.UpdateAfterConfirm(dto.ConfirmedQuantity, dto.DestinationNote);
            _unitOfWork.WarehouseOutboundReceipts.Update(receipt);

            request.MarkAsCompleted();
            _unitOfWork.WarehouseOutboundRequests.Update(request);

            await _unitOfWork.SaveChangesAsync();

            return new ServiceResult(Const.SUCCESS_UPDATE_CODE, "Xác nhận phiếu xuất kho thành công.", receipt.OutboundReceiptId);
        }



    }
}
