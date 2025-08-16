using DakLakCoffeeSupplyChain.Common.DTOs.WarehouseReceiptDTOs;
using DakLakCoffeeSupplyChain.Common.Enum.WarehouseInboundRequestEnums;
using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Repositories.Models;
using DakLakCoffeeSupplyChain.Repositories.UnitOfWork;
using DakLakCoffeeSupplyChain.Services.Base;
using DakLakCoffeeSupplyChain.Services.IServices;
using System;
using System.Threading.Tasks;
using System.Linq; // Cần cho .Sum() và .Select()
using System.Collections.Generic; // Cần cho List<object>
using DakLakCoffeeSupplyChain.Services.Generators;
using DakLakCoffeeSupplyChain.Services.Mappers;

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
            var staff = await _unitOfWork.BusinessStaffRepository
                .FindByUserIdAsync(staffUserId);

            if (staff == null)
                return new ServiceResult(Const.FAIL_UPDATE_CODE, "Không tìm thấy nhân viên.");

            var request = await _unitOfWork.WarehouseInboundRequests
                .GetByIdWithBatchAsync(dto.InboundRequestId);

            if (request == null || request.Status != InboundRequestStatus.Approved.ToString())
                return new ServiceResult(Const.FAIL_UPDATE_CODE, "Yêu cầu nhập kho không hợp lệ hoặc chưa được duyệt.");

            // KIỂM TRA: Số lượng còn lại trước khi cho phép tạo receipt mới
            var existingReceipts = await _unitOfWork.WarehouseReceipts
                .GetByInboundRequestIdAsync(dto.InboundRequestId);

            double totalReceivedSoFar = existingReceipts?.Sum(r => r.ReceivedQuantity ?? 0) ?? 0;
            double remainingQuantity = (request.RequestedQuantity ?? 0) - totalReceivedSoFar;

            // Nếu đã nhập đủ số lượng yêu cầu thì không cho tạo thêm
            if (remainingQuantity <= 0)
                return new ServiceResult(Const.FAIL_CREATE_CODE, 
                    $"Yêu cầu này đã được nhập đủ số lượng ({request.RequestedQuantity}kg).");

            var warehouse = await _unitOfWork.Warehouses.GetByIdAsync(dto.WarehouseId);

            if (warehouse == null)
                return new ServiceResult(Const.FAIL_READ_CODE, "Không tìm thấy kho.");

            var receiptCode = await _codeGenerator.GenerateWarehouseReceiptCodeAsync();
            var receiptId = Guid.NewGuid();

            var receipt = dto.ToEntityFromCreateDto(
                receiptId,
                receiptCode,
                staff.StaffId,
                request.BatchId ?? new Guid()
            );

            // Để trống số lượng đến khi xác nhận
            receipt.ReceivedQuantity = null;
            receipt.ReceivedAt = null;

            await _unitOfWork.WarehouseReceipts.CreateAsync(receipt);
            await _unitOfWork.SaveChangesAsync();

            return new ServiceResult(
                Const.SUCCESS_CREATE_CODE, 
                $"Tạo phiếu nhập kho thành công. Đã nhập: {totalReceivedSoFar}kg, Còn lại: {remainingQuantity}kg", receipt.ReceiptId
            );
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

            // Kiểm tra số lượng còn lại của request (giống hệt outbound)
            var existingReceipts = await _unitOfWork.WarehouseReceipts.GetByInboundRequestIdAsync(receipt.InboundRequestId);
            double totalReceivedSoFar = existingReceipts?.Sum(r => r.ReceivedQuantity ?? 0) ?? 0;
            double remainingQuantity = (request.RequestedQuantity ?? 0) - totalReceivedSoFar;

            if (dto.ConfirmedQuantity > remainingQuantity)
                return new ServiceResult(Const.ERROR_VALIDATION_CODE,
                    $"Số lượng xác nhận ({dto.ConfirmedQuantity}kg) vượt quá số lượng còn lại của yêu cầu ({remainingQuantity}kg).");

            // Chuyển status về Completed khi đủ số lượng
            double totalReceivedAfterThis = totalReceivedSoFar + dto.ConfirmedQuantity;
            
            if (totalReceivedAfterThis >= (request.RequestedQuantity ?? 0))
            {
                // Đã nhập đủ số lượng yêu cầu → CHUYỂN VỀ COMPLETED
                request.Status = InboundRequestStatus.Completed.ToString();
                request.ActualDeliveryDate = DateOnly.FromDateTime(DateTime.UtcNow);
                request.UpdatedAt = DateTime.UtcNow;
                
                // Cập nhật cả request
                _unitOfWork.WarehouseInboundRequests.UpdateAsync(request);
            }
            // Nếu chưa đủ -> GIỮ NGUYÊN STATUS (vẫn có thể tạo thêm receipts)

            // Ghi chú xác nhận
            var confirmNote = WarehouseReceiptMapper.BuildConfirmationNote(
                request.RequestedQuantity??0,
                dto.ConfirmedQuantity,
                dto.Note
            );

            // Cập nhật thông tin phiếu
            receipt.Note = string.IsNullOrWhiteSpace(receipt.Note)
                ? confirmNote
                : $"{receipt.Note} {confirmNote}";
            receipt.ReceivedQuantity = dto.ConfirmedQuantity; // DB chỉ 1 cột -> ghi tại bước confirm
            receipt.ReceivedAt ??= DateTime.UtcNow;

            // Kiểm tra sức chứa tại thời điểm xác nhận
            var warehouse = await _unitOfWork.Warehouses
                .GetByIdAsync(receipt.WarehouseId);

            if (warehouse == null)
                return new ServiceResult(Const.FAIL_READ_CODE, "Không tìm thấy kho.");

            var currentInventories = await _unitOfWork.Inventories
                .GetAllAsync(i => i.WarehouseId == receipt.WarehouseId && !i.IsDeleted);

            double totalCurrentQuantity = currentInventories.Sum(i => i.Quantity);
            double available = (warehouse.Capacity ?? 0) - totalCurrentQuantity;

            if (dto.ConfirmedQuantity > available)
            {
                return new ServiceResult(
                    Const.ERROR_VALIDATION_CODE,
                    $"Kho \"{warehouse.Name}\" chỉ còn trống {available:n0}kg, không thể nhập {dto.ConfirmedQuantity}kg."
                );
            }

            // Cập nhật tồn kho (find-or-create theo WarehouseId + BatchId)
            Inventory inventory = await _unitOfWork.Inventories.FindByWarehouseAndBatchAsync(receipt.WarehouseId, receipt.BatchId ?? new Guid());
            if (inventory != null)
            {
                inventory.Quantity += dto.ConfirmedQuantity;
                inventory.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.Inventories.Update(inventory);
            }
            else
            {
                var inventoryCode = await _codeGenerator.GenerateInventoryCodeAsync();
                inventory = WarehouseReceiptMapper.ToNewInventory(
                    receipt.WarehouseId,
                    receipt.BatchId ?? new Guid(),
                    dto.ConfirmedQuantity,
                    inventoryCode
                );
                await _unitOfWork.Inventories.CreateAsync(inventory);
            }

            // Ghi log
            var log = receipt.ToInventoryLogFromInbound(
                inventory.InventoryId,
                dto.ConfirmedQuantity,
                confirmNote
            );
            await _unitOfWork.InventoryLogs.CreateAsync(log);

            // KHÔNG thay đổi status của request - giữ nguyên để vẫn hiển thị
            // request.Status = InboundRequestStatus.Completed.ToString();
            // request.ActualDeliveryDate = DateOnly.FromDateTime(DateTime.UtcNow);
            // request.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.WarehouseReceipts.UpdateAsync(receipt);
            // await _unitOfWork.WarehouseInboundRequests.UpdateAsync(request); // Bỏ dòng này
            await _unitOfWork.SaveChangesAsync();

            return new ServiceResult(
                Const.SUCCESS_UPDATE_CODE,
                "Xác nhận phiếu nhập thành công", 
                receipt.ReceiptId
            );
        }

        public async Task<IServiceResult> GetAllAsync(Guid userId)
        {
            // Xác định managerId từ userId (dành cho cả manager và staff)
            Guid? managerId = null;

            var manager = await _unitOfWork.BusinessManagerRepository
                .FindByUserIdAsync(userId);

            if (manager != null && !manager.IsDeleted)
            {
                managerId = manager.ManagerId;
            }
            else
            {
                var staff = await _unitOfWork.BusinessStaffRepository
                    .FindByUserIdAsync(userId);

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

            var filtered = receipts
                .Where(r => r.Warehouse?.ManagerId == managerId)
                .Select(r => new WarehouseReceiptListItemDto
                {
                    ReceiptId = r.ReceiptId,
                    ReceiptCode = r.ReceiptCode,
                    WarehouseName = r.Warehouse?.Name,
                    BatchCode = r.Batch?.BatchCode,
                    ReceivedQuantity = (double)(r.ReceivedQuantity ?? 0), // tránh null
                    ReceivedAt = r.ReceivedAt,
                    StaffName = r.ReceivedByNavigation?.User?.Name,
                    Note = r.Note
                }).ToList();

            return new ServiceResult(
                Const.SUCCESS_READ_CODE, 
                "Lấy danh sách phiếu nhập kho thành công",
                filtered
            );
        }

        public async Task<IServiceResult> GetByIdAsync(Guid receiptId)
        {
            var receipt = await _unitOfWork.WarehouseReceipts
                .GetDetailByIdAsync(receiptId);

            if (receipt == null)
                return new ServiceResult(Const.FAIL_READ_CODE, "Không tìm thấy phiếu nhập kho.");

            var dto = new WarehouseReceiptDetailDto
            {
                ReceiptId = receipt.ReceiptId,
                ReceiptCode = receipt.ReceiptCode,
                WarehouseId = receipt.WarehouseId,
                WarehouseName = receipt.Warehouse?.Name,
                BatchId = receipt.BatchId ?? new Guid(),
                BatchCode = receipt.Batch?.BatchCode,
                ReceivedQuantity = (double)(receipt.ReceivedQuantity ?? 0),
                ReceivedAt = receipt.ReceivedAt,
                StaffName = receipt.ReceivedByNavigation?.User?.Name,
                Note = receipt.Note,
                // Thêm số lượng yêu cầu nhập từ inbound request
                RequestedQuantity = receipt.InboundRequest?.RequestedQuantity ?? 0
            };

            return new ServiceResult(Const.SUCCESS_READ_CODE, "Lấy chi tiết phiếu nhập thành công", dto);
        }

        public async Task<IServiceResult> SoftDeleteAsync(Guid receiptId, Guid userId)
        {
            var receipt = await _unitOfWork.WarehouseReceipts
                .GetDetailByIdAsync(receiptId);

            if (receipt == null || receipt.IsDeleted)
                return new ServiceResult(Const.FAIL_READ_CODE, "Không tìm thấy phiếu nhập kho.");

            Guid? managerId = null;

            var manager = await _unitOfWork.BusinessManagerRepository
                .FindByUserIdAsync(userId);

            if (manager != null && !manager.IsDeleted)
                managerId = manager.ManagerId;
            else
            {
                var staff = await _unitOfWork.BusinessStaffRepository
                    .FindByUserIdAsync(userId);

                if (staff != null && !staff.IsDeleted)
                    managerId = staff.SupervisorId;
            }

            if (managerId == null || receipt.Warehouse?.ManagerId != managerId)
                return new ServiceResult(Const.FAIL_READ_CODE, "Không có quyền xóa phiếu nhập kho này.");

            receipt.IsDeleted = true;
            await _unitOfWork.WarehouseReceipts.UpdateAsync(receipt);
            await _unitOfWork.SaveChangesAsync();

            return new ServiceResult(
                Const.SUCCESS_DELETE_CODE, 
                "Xóa mềm phiếu nhập kho thành công."
            );
        }

        public async Task<IServiceResult> HardDeleteAsync(Guid receiptId)
        {
            var receipt = await _unitOfWork.WarehouseReceipts.GetByIdAsync(receiptId);

            if (receipt == null)
                return new ServiceResult(Const.FAIL_READ_CODE, "Không tìm thấy phiếu nhập kho.");

            await _unitOfWork.WarehouseReceipts.RemoveAsync(receipt);
            await _unitOfWork.SaveChangesAsync();

            return new ServiceResult(
                Const.SUCCESS_DELETE_CODE,
                "Đã xóa vĩnh viễn phiếu nhập kho."
            );
        }

        // Method để lấy thông tin summary của inbound request (giống outbound)
        public async Task<IServiceResult> GetInboundRequestSummaryAsync(Guid inboundRequestId)
        {
            try
            {
                var request = await _unitOfWork.WarehouseInboundRequests
                    .GetByIdWithBatchAsync(inboundRequestId);

                if (request == null)
                    return new ServiceResult(Const.FAIL_READ_CODE, "Không tìm thấy yêu cầu nhập kho");

                // Lấy tất cả receipts của request này
                var receipts = await _unitOfWork.WarehouseReceipts
                    .GetByInboundRequestIdAsync(inboundRequestId);
                
                // Tính toán thống kê
                var totalReceivedQuantity = receipts?.Sum(r => r.ReceivedQuantity ?? 0) ?? 0;
                var remainingQuantity = (request.RequestedQuantity ?? 0) - totalReceivedQuantity;
                
                // Xác định status
                string status;
                if (totalReceivedQuantity == 0) status = "Pending";
                else if (remainingQuantity <= 0) status = "Completed";
                else status = "Partial";

                // Tạo response object (giống outbound)
                var result = new
                {
                    InboundRequestId = request.InboundRequestId,
                    RequestCode = request.InboundRequestCode,
                    RequestedQuantity = request.RequestedQuantity,
                    TotalReceivedQuantity = totalReceivedQuantity,
                    RemainingQuantity = remainingQuantity,
                    Status = status,
                    Unit = "kg", // Đơn vị mặc định
                    ReceiptsCount = receipts?.Count ?? 0
                };

                return new ServiceResult(
                    Const.SUCCESS_READ_CODE, 
                    "Lấy thông tin summary yêu cầu thành công", 
                    result
                );
            }
            catch (Exception ex)
            {
                return new ServiceResult(
                    Const.FAIL_READ_CODE, 
                    $"Lỗi: {ex.Message}"
                );
            }
        }
    }
}
