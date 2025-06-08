using DakLakCoffeeSupplyChain.Common.DTOs.Flow4DTOs;
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
    public class WarehouseInboundRequestService : IWarehouseInboundRequestService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly INotificationService _notificationService;

        public WarehouseInboundRequestService(IUnitOfWork unitOfWork, INotificationService notificationService)
        {
            _unitOfWork = unitOfWork;
            _notificationService = notificationService;
        }

        public async Task<IServiceResult> CreateRequestAsync(Guid userId, WarehouseInboundRequestCreateDto dto)
        {
            var farmer = await _unitOfWork.Farmers.FindByUserIdAsync(userId);

            if (farmer == null)
                return new ServiceResult(404, "Không tìm thấy Farmer tương ứng với User.");

            var newRequest = new WarehouseInboundRequest
            {
                InboundRequestId = Guid.NewGuid(),
                InboundRequestCode = "IR-" + DateTime.UtcNow.Ticks, // ✅ thêm dòng này để tránh lỗi duplicate NULL
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

            // Gửi email và thông báo nội bộ
            await _notificationService.NotifyInboundRequestCreatedAsync(newRequest.InboundRequestId, farmer.FarmerId);

            return new ServiceResult(201, "Yêu cầu nhập kho đã được tạo", newRequest.InboundRequestId);
        }
        public async Task<IServiceResult> ApproveInboundRequestAsync(Guid requestId, Guid staffUserId)
        {
            var staff = await _unitOfWork.BusinessStaffs.FindByUserIdAsync(staffUserId);
            if (staff == null)
                return new ServiceResult(404, "Không tìm thấy nhân viên doanh nghiệp.");

            var request = await _unitOfWork.WarehouseInboundRequests.GetByIdAsync(requestId);
            if (request == null)
                return new ServiceResult(404, "Không tìm thấy yêu cầu nhập kho.");

            if (request.Status != "Pending")
                return new ServiceResult(400, "Yêu cầu này không ở trạng thái chờ duyệt.");

            request.Status = "Approved";
            request.BusinessStaffId = staff.StaffId;
            request.UpdatedAt = DateTime.UtcNow;

            // ✅ THÊM DÒNG NÀY
            _unitOfWork.WarehouseInboundRequests.Update(request);

            await _unitOfWork.CompleteAsync();
            return new ServiceResult(200, "Đã duyệt yêu cầu nhập kho.");
        }
        public async Task<IServiceResult> GetAllRequestsAsync()
        {
            var requests = await _unitOfWork.WarehouseInboundRequests.GetAllAsync();

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

            return new ServiceResult(200, "Lấy danh sách yêu cầu nhập kho thành công", result);
        }



    }
}