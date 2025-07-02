using DakLakCoffeeSupplyChain.Common.DTOs.WarehouseReceiptDTOs;
using DakLakCoffeeSupplyChain.Common.Enum.WarehouseInboundRequestEnums;
using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Repositories.Models;
using DakLakCoffeeSupplyChain.Repositories.UnitOfWork;
using DakLakCoffeeSupplyChain.Services.Base;
using DakLakCoffeeSupplyChain.Services.IServices;
using System;
using System.Threading.Tasks;
using DakLakCoffeeSupplyChain.Services.Generators;

namespace DakLakCoffeeSupplyChain.Services.Services
{
    public class WarehouseReceiptService : IWarehouseReceiptService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICodeGenerator _codeGenerator;

        public WarehouseReceiptService(IUnitOfWork unitOfWork, ICodeGenerator codeGenerator)
        {
            _unitOfWork = unitOfWork;
            _codeGenerator = codeGenerator;
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

            if (dto.ReceivedQuantity != request.RequestedQuantity)
            {
                return new ServiceResult(Const.ERROR_VALIDATION_CODE,
                    $"Số lượng nhập ({dto.ReceivedQuantity}kg) không khớp với số lượng yêu cầu ({request.RequestedQuantity}kg).");
            }

            var warehouse = await _unitOfWork.Warehouses.GetByIdAsync(dto.WarehouseId);
            if (warehouse == null)
                return new ServiceResult(Const.FAIL_READ_CODE, "Không tìm thấy kho.");

            var currentInventories = await _unitOfWork.Inventories
                .GetAllAsync(i => i.WarehouseId == dto.WarehouseId && !i.IsDeleted);
            double totalCurrentQuantity = currentInventories.Sum(i => i.Quantity);
            double available = (warehouse.Capacity ?? 0) - totalCurrentQuantity;

            if (dto.ReceivedQuantity > available)
            {
                return new ServiceResult(Const.ERROR_VALIDATION_CODE,
                    $"Kho \"{warehouse.Name}\" chỉ còn trống {available:n0}kg, không thể tạo phiếu nhập với {dto.ReceivedQuantity}kg.");
            }

            var receiptCode = await _codeGenerator.GenerateWarehouseReceiptCodeAsync();

            var receipt = new WarehouseReceipt
            {
                ReceiptId = Guid.NewGuid(),
                ReceiptCode = receiptCode,
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
                return new ServiceResult(Const.FAIL_READ_CODE, "Không tìm thấy phiếu nhập kho.");

            if (dto.ConfirmedQuantity <= 0)
                return new ServiceResult(Const.ERROR_VALIDATION_CODE, "Số lượng xác nhận không hợp lệ.");

            var request = await _unitOfWork.WarehouseInboundRequests.GetByIdAsync(receipt.InboundRequestId);
            if (request == null)
                return new ServiceResult(Const.FAIL_READ_CODE, "Không tìm thấy yêu cầu nhập kho tương ứng.");

            if (dto.ConfirmedQuantity > request.RequestedQuantity)
                return new ServiceResult(Const.ERROR_VALIDATION_CODE,
                    $"Số lượng xác nhận ({dto.ConfirmedQuantity}kg) vượt quá yêu cầu ({request.RequestedQuantity}kg).");

            if (dto.ConfirmedQuantity > receipt.ReceivedQuantity)
                return new ServiceResult(Const.ERROR_VALIDATION_CODE,
                    $"Số lượng xác nhận ({dto.ConfirmedQuantity}kg) vượt quá số lượng đã tạo phiếu ({receipt.ReceivedQuantity}kg).");

            // Ghi chú vào phiếu nhập
            var noteParts = new List<string>();

            if (dto.ConfirmedQuantity != receipt.ReceivedQuantity)
                noteParts.Add($"[Chênh lệch: tạo {receipt.ReceivedQuantity}kg, xác nhận {dto.ConfirmedQuantity}kg]");

            if (!string.IsNullOrWhiteSpace(dto.Note))
                noteParts.Add($"[Lý do: {dto.Note}]");

            noteParts.Add($"[Confirmed at {DateTime.UtcNow:yyyy-MM-dd HH:mm}]");

            if (!string.IsNullOrWhiteSpace(receipt.Note))
                receipt.Note += " " + string.Join(" ", noteParts);
            else
                receipt.Note = string.Join(" ", noteParts);

            receipt.ReceivedQuantity = dto.ConfirmedQuantity;
            receipt.ReceivedAt ??= DateTime.UtcNow;

            var warehouse = await _unitOfWork.Warehouses.GetByIdAsync(receipt.WarehouseId);
            if (warehouse == null)
                return new ServiceResult(Const.FAIL_READ_CODE, "Không tìm thấy kho.");

            var currentInventories = await _unitOfWork.Inventories
                .GetAllAsync(i => i.WarehouseId == receipt.WarehouseId && !i.IsDeleted);
            double totalCurrentQuantity = currentInventories.Sum(i => i.Quantity);
            double available = (warehouse.Capacity ?? 0) - totalCurrentQuantity;

            if (dto.ConfirmedQuantity > available)
            {
                return new ServiceResult(Const.ERROR_VALIDATION_CODE,
                    $"Kho \"{warehouse.Name}\" chỉ còn trống {available:n0}kg, không thể nhập {dto.ConfirmedQuantity}kg.");
            }

            Inventory inventory = await _unitOfWork.Inventories.FindByWarehouseAndBatchAsync(receipt.WarehouseId, receipt.BatchId);
            if (inventory != null)
            {
                inventory.Quantity += dto.ConfirmedQuantity;
                inventory.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.Inventories.Update(inventory);
            }
            else
            {
                var inventoryCode = await _codeGenerator.GenerateInventoryCodeAsync();
                inventory = new Inventory
                {
                    InventoryId = Guid.NewGuid(),
                    InventoryCode = inventoryCode,
                    WarehouseId = receipt.WarehouseId,
                    BatchId = receipt.BatchId,
                    Quantity = dto.ConfirmedQuantity,
                    Unit = "kg",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsDeleted = false
                };
                await _unitOfWork.Inventories.CreateAsync(inventory);
            }

            // ✅ Ghi chú log chi tiết
            var logNote = $"Nhập kho từ phiếu {receipt.ReceiptCode}";
            if (dto.ConfirmedQuantity != receipt.ReceivedQuantity)
                logNote += $" | Chênh lệch: tạo {receipt.ReceivedQuantity}kg, xác nhận {dto.ConfirmedQuantity}kg";

            if (!string.IsNullOrWhiteSpace(dto.Note))
                logNote += $" | Lý do: {dto.Note}";

            var log = new InventoryLog
            {
                LogId = Guid.NewGuid(),
                InventoryId = inventory.InventoryId,
                ActionType = "ConfirmInbound",
                QuantityChanged = dto.ConfirmedQuantity,
                TriggeredBySystem = true,
                Note = logNote,
                LoggedAt = DateTime.UtcNow,
                IsDeleted = false
            };
            await _unitOfWork.InventoryLogs.CreateAsync(log);

            request.Status = InboundRequestStatus.Completed.ToString();
            request.ActualDeliveryDate = DateOnly.FromDateTime(DateTime.UtcNow);
            request.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.WarehouseReceipts.UpdateAsync(receipt);
            await _unitOfWork.WarehouseInboundRequests.UpdateAsync(request);
            await _unitOfWork.SaveChangesAsync();

            return new ServiceResult(Const.SUCCESS_UPDATE_CODE, "Xác nhận phiếu nhập thành công", receipt.ReceiptId);
        }

        public async Task<IServiceResult> GetAllAsync(Guid userId)
        {
            // Xác định managerId từ userId (dành cho cả manager và staff)
            Guid? managerId = null;

            var manager = await _unitOfWork.BusinessManagerRepository.FindByUserIdAsync(userId);
            if (manager != null && !manager.IsDeleted)
            {
                managerId = manager.ManagerId;
            }
            else
            {
                var staff = await _unitOfWork.BusinessStaffRepository.FindByUserIdAsync(userId);
                if (staff != null && !staff.IsDeleted)
                {
                    managerId = staff.SupervisorId;
                }
            }

            if (managerId == null)
            {
                return new ServiceResult(Const.FAIL_READ_CODE, "Không xác định được người dùng thuộc công ty nào.");
            }

            var receipts = await _unitOfWork.WarehouseReceipts.GetAllWithIncludesAsync();

            // Lọc ra những receipt mà warehouse.ManagerId == managerId
            var filtered = receipts
                .Where(r => r.Warehouse?.ManagerId == managerId)
                .Select(r => new WarehouseReceiptListItemDto
                {
                    ReceiptId = r.ReceiptId,
                    ReceiptCode = r.ReceiptCode,
                    WarehouseName = r.Warehouse?.Name,
                    BatchCode = r.Batch?.BatchCode,
                    ReceivedQuantity = (double)r.ReceivedQuantity,
                    ReceivedAt = r.ReceivedAt,
                    StaffName = r.ReceivedByNavigation?.User?.Name
                }).ToList();

            return new ServiceResult(Const.SUCCESS_READ_CODE, "Lấy danh sách phiếu nhập kho thành công", filtered);
        }

        public async Task<IServiceResult> GetByIdAsync(Guid receiptId)
        {
            var receipt = await _unitOfWork.WarehouseReceipts.GetDetailByIdAsync(receiptId);
            if (receipt == null)
                return new ServiceResult(Const.FAIL_READ_CODE, "Không tìm thấy phiếu nhập kho.");

            var dto = new WarehouseReceiptDetailDto
            {
                ReceiptId = receipt.ReceiptId,
                ReceiptCode = receipt.ReceiptCode,
                WarehouseId = receipt.WarehouseId,
                WarehouseName = receipt.Warehouse?.Name,
                BatchId = receipt.BatchId,
                BatchCode = receipt.Batch?.BatchCode,
                ReceivedQuantity = (double)receipt.ReceivedQuantity,
                ReceivedAt = receipt.ReceivedAt,
                StaffName = receipt.ReceivedByNavigation?.User?.Name,
                Note = receipt.Note
            };

            return new ServiceResult(Const.SUCCESS_READ_CODE, "Lấy chi tiết phiếu nhập thành công", dto);
        }
    }
}
