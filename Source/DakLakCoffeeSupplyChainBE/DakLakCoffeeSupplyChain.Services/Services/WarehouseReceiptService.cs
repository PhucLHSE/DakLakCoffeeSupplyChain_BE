using DakLakCoffeeSupplyChain.Common.DTOs.WarehouseReceiptDTOs;
using DakLakCoffeeSupplyChain.Common.Enum.WarehouseInboundRequestEnums;
using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Repositories.Models;
using DakLakCoffeeSupplyChain.Repositories.UnitOfWork;
using DakLakCoffeeSupplyChain.Services.Base;
using DakLakCoffeeSupplyChain.Services.IServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

            var receipt = new WarehouseReceipt
            {
                ReceiptId = Guid.NewGuid(),
                ReceiptCode = "WR-" + Guid.NewGuid().ToString("N").Substring(0, 8),
                InboundRequestId = request.InboundRequestId,
                WarehouseId = dto.WarehouseId,
                BatchId = request.BatchId,
                ReceivedBy = staff.StaffId,
                ReceivedQuantity = dto.ReceivedQuantity,
                ReceivedAt = DateTime.UtcNow,
                Note = dto.Note,
                QrcodeUrl = ""
            };

            await _unitOfWork.WarehouseReceipts.CreateAsync(receipt);

            request.Status = InboundRequestStatus.Completed.ToString();
            request.ActualDeliveryDate = DateOnly.FromDateTime(DateTime.UtcNow);
            request.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.WarehouseInboundRequests.Update(request);

            await _unitOfWork.SaveChangesAsync();

            return new ServiceResult(Const.SUCCESS_CREATE_CODE, "Tạo phiếu nhập kho thành công", receipt.ReceiptId);
        }
        public async Task<IServiceResult> ConfirmReceiptAsync(Guid receiptId)
        {
            var receipt = await _unitOfWork.WarehouseReceipts.GetByIdAsync(receiptId);
            if (receipt == null)
                return new ServiceResult(Const.FAIL_READ_CODE, Const.FAIL_READ_MSG);

            if (receipt.ReceivedQuantity == null || receipt.ReceivedQuantity <= 0)
                return new ServiceResult(Const.ERROR_VALIDATION_CODE, Const.FAIL_UPDATE_MSG);

            var request = await _unitOfWork.WarehouseInboundRequests.GetByIdAsync(receipt.InboundRequestId);
            if (request == null)
                return new ServiceResult(Const.FAIL_READ_CODE, "Không tìm thấy yêu cầu nhập kho tương ứng.");

            // ⚠️ Cảnh báo nếu số lượng nhận lớn hơn số lượng yêu cầu
            if (receipt.ReceivedQuantity > request.RequestedQuantity)
            {
                var warningNote = $"[Cảnh báo: Nhận {receipt.ReceivedQuantity}kg > yêu cầu {request.RequestedQuantity}kg]";
                receipt.Note = string.IsNullOrWhiteSpace(receipt.Note) ? warningNote : receipt.Note + " " + warningNote;
            }

            var inventory = await _unitOfWork.Inventories.FindByWarehouseAndBatchAsync(receipt.WarehouseId, receipt.BatchId);

            if (inventory != null)
            {
                inventory.Quantity += receipt.ReceivedQuantity.Value;
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
                    Quantity = receipt.ReceivedQuantity.Value,
                    Unit = "kg",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsDeleted = false
                };

                await _unitOfWork.Inventories.CreateAsync(newInventory);
            }

            receipt.Note = (receipt.Note ?? "") + $" [Confirmed at {DateTime.UtcNow:yyyy-MM-dd HH:mm}]";
            receipt.ReceivedAt ??= DateTime.UtcNow;

            _unitOfWork.WarehouseReceipts.UpdateAsync(receipt);
            await _unitOfWork.SaveChangesAsync();

            return new ServiceResult(Const.SUCCESS_UPDATE_CODE, Const.SUCCESS_UPDATE_MSG);
        }

    }
}