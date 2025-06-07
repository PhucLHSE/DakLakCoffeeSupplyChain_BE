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
                BusinessManagerId = dto.BusinessManagerId,
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
    }
}