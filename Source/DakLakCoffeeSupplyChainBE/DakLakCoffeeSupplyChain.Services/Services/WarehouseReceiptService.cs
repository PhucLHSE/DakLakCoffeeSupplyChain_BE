using DakLakCoffeeSupplyChain.Common.DTOs.WarehouseReceiptDTOs;
using DakLakCoffeeSupplyChain.Common.Enum.WarehouseInboundRequestEnums;
using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Repositories.Models;
using DakLakCoffeeSupplyChain.Repositories.UnitOfWork;
using DakLakCoffeeSupplyChain.Services.Base;
using DakLakCoffeeSupplyChain.Services.IServices;
using System;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Services.Services
{
    public class WarehouseReceiptService : IWarehouseReceiptService
    {
        private readonly IUnitOfWork _unitOfWork;

        public WarehouseReceiptService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IServiceResult> CreateReceiptAsync(Guid staffUserId, WarehouseReceiptCreateDto dto)
        {
            var staff = await _unitOfWork.BusinessStaffRepository.FindByUserIdAsync(staffUserId);
            if (staff == null)
                return new ServiceResult(Const.FAIL_UPDATE_CODE, "Không tìm thấy nhân viên.");

            var request = await _unitOfWork.WarehouseInboundRequests.GetByIdWithBatchAsync(dto.InboundRequestId);
            if (request == null || request.Status != InboundRequestStatus.Approved.ToString())
                return new ServiceResult(Const.FAIL_UPDATE_CODE, "Yêu cầu nhập kho không hợp lệ hoặc chưa được duyệt.");

            var existing = await _unitOfWork.WarehouseReceipts.GetByInboundRequestIdAsync(dto.InboundRequestId);
            if (existing != null)
                return new ServiceResult(Const.FAIL_CREATE_CODE, "Yêu cầu này đã có phiếu nhập.");

            // ✅ Kiểm tra số lượng nhập phải bằng số lượng yêu cầu
            if (dto.ReceivedQuantity != request.RequestedQuantity)
            {
                return new ServiceResult(Const.ERROR_VALIDATION_CODE,
                    $"Số lượng nhập ({dto.ReceivedQuantity}kg) không khớp với số lượng yêu cầu ({request.RequestedQuantity}kg).");
            }

            var receipt = new WarehouseReceipt
            {
                ReceiptId = Guid.NewGuid(),
                ReceiptCode = "WR-" + Guid.NewGuid().ToString("N")[..8],
                InboundRequestId = request.InboundRequestId,
                WarehouseId = dto.WarehouseId,
                BatchId = request.BatchId,
                ReceivedBy = staff.StaffId,
                ReceivedQuantity = dto.ReceivedQuantity,
                ReceivedAt = DateTime.UtcNow,
                Note = dto.Note,
                QrcodeUrl = "",
                IsDeleted = false
            };

            await _unitOfWork.WarehouseReceipts.CreateAsync(receipt);
            await _unitOfWork.SaveChangesAsync();

            return new ServiceResult(Const.SUCCESS_CREATE_CODE, "Tạo phiếu nhập kho thành công", receipt.ReceiptId);
        }

        public async Task<IServiceResult> ConfirmReceiptAsync(Guid receiptId, WarehouseReceiptConfirmDto dto)
        {
            var receipt = await _unitOfWork.WarehouseReceipts.GetByIdAsync(receiptId);
            if (receipt == null)
                return new ServiceResult(Const.FAIL_READ_CODE, Const.FAIL_READ_MSG);

            if (dto.ConfirmedQuantity <= 0)
                return new ServiceResult(Const.ERROR_VALIDATION_CODE, "Số lượng xác nhận không hợp lệ.");

            var request = await _unitOfWork.WarehouseInboundRequests.GetByIdAsync(receipt.InboundRequestId);
            if (request == null)
                return new ServiceResult(Const.FAIL_READ_CODE, "Không tìm thấy yêu cầu nhập kho tương ứng.");

            // ❌ Không được xác nhận vượt quá số lượng yêu cầu
            if (dto.ConfirmedQuantity > request.RequestedQuantity)
            {
                return new ServiceResult(Const.ERROR_VALIDATION_CODE,
                    $"Số lượng xác nhận ({dto.ConfirmedQuantity}kg) vượt quá yêu cầu ({request.RequestedQuantity}kg).");
            }

            // ❌ Không được xác nhận vượt quá số lượng đã ghi nhận trong phiếu
            if ((double)dto.ConfirmedQuantity > receipt.ReceivedQuantity)
            {
                return new ServiceResult(Const.ERROR_VALIDATION_CODE,
                    $"Số lượng xác nhận ({dto.ConfirmedQuantity}kg) vượt quá số lượng đã tạo phiếu ({receipt.ReceivedQuantity}kg).");
            }

            // ⚠️ Ghi chú nếu có chênh lệch
            if ((double)dto.ConfirmedQuantity != receipt.ReceivedQuantity)
            {
                var diffNote = $"[Chênh lệch: tạo {receipt.ReceivedQuantity}kg, xác nhận {dto.ConfirmedQuantity}kg]";
                receipt.Note = string.IsNullOrWhiteSpace(receipt.Note) ? diffNote : receipt.Note + " " + diffNote;
            }

            // Cập nhật lại số lượng xác nhận (có thể nhỏ hơn)
            receipt.ReceivedQuantity = (double)dto.ConfirmedQuantity;
            receipt.Note = (receipt.Note ?? "") + $" [Confirmed at {DateTime.UtcNow:yyyy-MM-dd HH:mm}]";
            receipt.ReceivedAt ??= DateTime.UtcNow;

            // Tồn kho
            var inventory = await _unitOfWork.Inventories.FindByWarehouseAndBatchAsync(receipt.WarehouseId, receipt.BatchId);
            if (inventory != null)
            {
                inventory.Quantity += (double)dto.ConfirmedQuantity;
                inventory.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.Inventories.Update(inventory);
            }
            else
            {
                var newInventory = new Inventory
                {
                    InventoryId = Guid.NewGuid(),
                    InventoryCode = "INV-" + Guid.NewGuid().ToString("N")[..8],
                    WarehouseId = receipt.WarehouseId,
                    BatchId = receipt.BatchId,
                    Quantity = (double)dto.ConfirmedQuantity,
                    Unit = "kg",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsDeleted = false
                };
                await _unitOfWork.Inventories.CreateAsync(newInventory);
            }

            request.Status = InboundRequestStatus.Completed.ToString();
            request.ActualDeliveryDate = DateOnly.FromDateTime(DateTime.UtcNow);
            request.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.WarehouseReceipts.UpdateAsync(receipt);
            _unitOfWork.WarehouseInboundRequests.Update(request);
            await _unitOfWork.SaveChangesAsync();

            return new ServiceResult(Const.SUCCESS_UPDATE_CODE, "Xác nhận thành công", receipt.ReceiptId);
        }



    }
}
