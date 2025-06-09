using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.Flow4DTOs;
using DakLakCoffeeSupplyChain.Repositories.Models;
using DakLakCoffeeSupplyChain.Repositories.UnitOfWork;
using DakLakCoffeeSupplyChain.Services.Base;
using DakLakCoffeeSupplyChain.Services.IServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Services.Services
{
    public class WarehouseInboundRequestService : IWarehouseInboundRequestService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly INotificationService _notificationService;

        public WarehouseInboundRequestService(IUnitOfWork unitOfWork, INotificationService notificationService)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        }

        public async Task<IServiceResult> CreateRequestAsync(Guid userId, WarehouseInboundRequestCreateDto dto)
        {
            var farmer = await _unitOfWork.Farmers.FindByUserIdAsync(userId);
            if (farmer == null)
                return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không tìm thấy Farmer tương ứng với User.");

            var newRequest = new WarehouseInboundRequest
            {
                InboundRequestId = Guid.NewGuid(),
                InboundRequestCode = "IR-" + DateTime.UtcNow.Ticks,
                FarmerId = farmer.FarmerId,
                BatchId = (Guid)dto.BatchId,
                RequestedQuantity = dto.RequestedQuantity,
                PreferredDeliveryDate = dto.PreferredDeliveryDate,
                Status = "Pending",
                Note = dto.Note,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _unitOfWork.WarehouseInboundRequests.CreateAsync(newRequest);
            await _unitOfWork.CompleteAsync();

            await _notificationService.NotifyInboundRequestCreatedAsync(newRequest.InboundRequestId, farmer.FarmerId);

            return new ServiceResult(Const.SUCCESS_CREATE_CODE, "Tạo yêu cầu nhập kho thành công", newRequest.InboundRequestId);
        }

        public async Task<IServiceResult> ApproveInboundRequestAsync(Guid requestId, Guid staffUserId)
        {
            var staff = await _unitOfWork.BusinessStaffs.FindByUserIdAsync(staffUserId);
            if (staff == null)
                return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không tìm thấy nhân viên doanh nghiệp.");

            var request = await _unitOfWork.WarehouseInboundRequests.GetByIdAsync(requestId);
            if (request == null)
                return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không tìm thấy yêu cầu nhập kho.");

            if (request.Status != "Pending")
                return new ServiceResult(Const.FAIL_VALIDATE_CODE, "Yêu cầu này không ở trạng thái chờ duyệt.");

            request.Status = "Approved";
            request.BusinessStaffId = staff.StaffId;
            request.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.WarehouseInboundRequests.Update(request);
            await _unitOfWork.CompleteAsync();

            return new ServiceResult(Const.SUCCESS_UPDATE_CODE, "Duyệt yêu cầu thành công.");
        }

        public async Task<IServiceResult> GetAllRequestsAsync()
        {
            var requests = await _unitOfWork.WarehouseInboundRequests.GetAllAsync();

            if (requests == null || !requests.Any())
                return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không có yêu cầu nhập kho nào.", new List<WarehouseInboundRequestViewDto>());

            var result = requests.Select(r => new WarehouseInboundRequestViewDto
            {
                InboundRequestId = r.InboundRequestId,
                InboundRequestCode = r.InboundRequestCode,
                FarmerId = r.FarmerId,
                BusinessStaffId = r.BusinessStaffId,
                RequestedQuantity = r.RequestedQuantity,
                PreferredDeliveryDate = r.PreferredDeliveryDate,
                Status = r.Status,
                CreatedAt = r.CreatedAt
            }).ToList();

            return new ServiceResult(Const.SUCCESS_READ_CODE, "Lấy danh sách yêu cầu thành công", result);
        }
    }
}
