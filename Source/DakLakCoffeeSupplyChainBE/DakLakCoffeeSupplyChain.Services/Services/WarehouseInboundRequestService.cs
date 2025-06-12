using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.WarehouseInboundRequestDTOs;
using DakLakCoffeeSupplyChain.Common.Enum.WarehouseInboundRequestEnums;
using DakLakCoffeeSupplyChain.Repositories.Models;
using DakLakCoffeeSupplyChain.Repositories.UnitOfWork;
using DakLakCoffeeSupplyChain.Services.Base;
using DakLakCoffeeSupplyChain.Services.IServices;
using DakLakCoffeeSupplyChain.Services.Mappers;
using System;
using System.Collections;
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
            var farmer = await _unitOfWork.FarmerRepository.FindByUserIdAsync(userId);
            if (farmer == null)
                return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không tìm thấy Farmer tương ứng với User.");

            var newRequest = new WarehouseInboundRequest
            {
                InboundRequestId = Guid.NewGuid(),
                InboundRequestCode = "IR-" + DateTime.UtcNow.Ticks,
                FarmerId = farmer.FarmerId,
                BusinessStaffId = dto.BusinessStaffId,
                BatchId = dto.BatchId ?? Guid.Empty,
                RequestedQuantity = dto.RequestedQuantity,
                PreferredDeliveryDate = dto.PreferredDeliveryDate,
                Status = "Pending",
                Note = dto.Note,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _unitOfWork.WarehouseInboundRequests.CreateAsync(newRequest);
            await _unitOfWork.SaveChangesAsync();

            await _notificationService.NotifyInboundRequestCreatedAsync(newRequest.InboundRequestId, farmer.FarmerId);

            return new ServiceResult(Const.SUCCESS_CREATE_CODE, "Tạo yêu cầu nhập kho thành công", newRequest.InboundRequestId);
        }
        public async Task<IServiceResult> ApproveRequestAsync(Guid requestId, Guid staffUserId)
        {
            // 1. Tìm yêu cầu nhập kho
            var request = await _unitOfWork.WarehouseInboundRequests.GetByIdAsync(requestId);
            if (request == null)
                return new ServiceResult(404, "Không tìm thấy yêu cầu nhập kho.");

            // ✅ So sánh enum bằng string an toàn
            if (request.Status != InboundRequestStatus.Pending.ToString())
                return new ServiceResult(400, "Yêu cầu đã được xử lý.");

            // 2. Tìm thông tin nhân viên
            var staff = await _unitOfWork.BusinessStaffRepository.FindByUserIdAsync(staffUserId);
            if (staff == null)
                return new ServiceResult(403, "Tài khoản không hợp lệ.");

            // 3. Cập nhật thông tin yêu cầu
            request.Status = InboundRequestStatus.Approved.ToString(); // ✅ dùng enum
            request.BusinessStaffId = staff.StaffId;
            request.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.WarehouseInboundRequests.Update(request);
            await _unitOfWork.SaveChangesAsync();

            // 4. Gửi thông báo cho Farmer
            await _notificationService.NotifyInboundRequestApprovedAsync(
                request.InboundRequestId,
                request.Farmer.UserId // đảm bảo đã Include Farmer.User
            );

            return new ServiceResult(200, "Duyệt yêu cầu thành công");
        }




    }
}
