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
    }
}