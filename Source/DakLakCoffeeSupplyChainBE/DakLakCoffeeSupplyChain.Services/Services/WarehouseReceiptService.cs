using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.Flow4DTOs;
using DakLakCoffeeSupplyChain.Repositories.Models;
using DakLakCoffeeSupplyChain.Repositories.UnitOfWork;
using DakLakCoffeeSupplyChain.Services.Base;
using DakLakCoffeeSupplyChain.Services.IServices;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Services.Services
{
    public class WarehouseReceiptService : IWarehouseReceiptService
    {
        private readonly IUnitOfWork _unitOfWork;

        public WarehouseReceiptService(IUnitOfWork unitOfWork)
            => _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));

        public async Task<IServiceResult> CreateWarehouseReceiptAsync(WarehouseReceiptCreateDto dto, Guid staffUserId)
        {
            var request = await _unitOfWork.WarehouseInboundRequests.GetByIdWithBatchAsync(dto.InboundRequestId);
            if (request == null)
                return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không tìm thấy yêu cầu nhập kho.");

            if (request.Status != "Approved")
                return new ServiceResult(Const.FAIL_VALIDATE_CODE, "Yêu cầu chưa được duyệt hoặc đã xử lý.");

            if (request.BatchId == null || request.Batch == null)
                return new ServiceResult(Const.FAIL_VALIDATE_CODE, "Thiếu thông tin lô sơ chế để nhập kho.");

            if (request.BusinessStaffId == null)
            {
                request.BusinessStaffId = staffUserId;
                _unitOfWork.WarehouseInboundRequests.Update(request);
            }

            var receiptCode = await _unitOfWork.WarehouseReceiptRepository.GenerateReceiptCodeAsync();

            var receipt = new WarehouseReceipt
            {
                ReceiptId = Guid.NewGuid(),
                ReceiptCode = receiptCode,
                InboundRequestId = request.InboundRequestId,
                WarehouseId = dto.WarehouseId,
                BatchId = request.BatchId,
                ReceivedBy = request.BusinessStaffId.Value,
                ReceivedQuantity = dto.ReceivedQuantity,
                ReceivedAt = dto.ReceivedAt,
                Note = dto.Note,
                QrcodeUrl = null
            };

            await _unitOfWork.WarehouseReceiptRepository.CreateAsync(receipt);

            request.Status = "Received";
            request.ActualDeliveryDate = DateOnly.FromDateTime(dto.ReceivedAt);
            _unitOfWork.WarehouseInboundRequests.Update(request);

            await _unitOfWork.InventoryRepository.AddOrUpdateInventoryAsync(
                dto.WarehouseId,
                request.BatchId,
                dto.ReceivedQuantity,
                "kg"
            );

            await _unitOfWork.CompleteAsync();

            return new ServiceResult(Const.SUCCESS_CREATE_CODE, "Tạo phiếu nhập kho thành công", new
            {
                receipt.ReceiptId,
                receipt.ReceiptCode,
                receipt.ReceivedAt,
                receipt.ReceivedQuantity
            });
        }

        public async Task<IServiceResult> GetAllReceiptsAsync()
        {
            var receipts = await _unitOfWork.WarehouseReceiptRepository.GetAllWithIncludesAsync();

            if (receipts == null || !receipts.Any())
                return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không có phiếu nhập kho nào.", new object[] { });

            var result = receipts.Select(r => new WarehouseReceiptViewDto
            {
                ReceiptId = r.ReceiptId,
                ReceiptCode = r.ReceiptCode,
                WarehouseName = r.Warehouse?.Name,
                ReceivedQuantity = r.ReceivedQuantity,
                ReceivedAt = r.ReceivedAt,
                StaffName = r.ReceivedByNavigation?.User?.Name
            }).ToList();

            return new ServiceResult(Const.SUCCESS_READ_CODE, "Lấy danh sách phiếu nhập kho thành công", result);
        }
    }
}
