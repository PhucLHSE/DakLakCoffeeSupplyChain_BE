using DakLakCoffeeSupplyChain.Common.DTOs.WarehouseOutboundRequestDTOs;
using DakLakCoffeeSupplyChain.Common.Enum.WarehouseOutboundRequestEnums;
using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Repositories.Models;
using DakLakCoffeeSupplyChain.Repositories.UnitOfWork;
using DakLakCoffeeSupplyChain.Services.Base;
using DakLakCoffeeSupplyChain.Services.IServices;
using DakLakCoffeeSupplyChain.Services.Generators;
using DakLakCoffeeSupplyChain.Services.Mappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Services.Services
{
    public class WarehouseOutboundRequestService : IWarehouseOutboundRequestService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly INotificationService _notificationService;
        private readonly ICodeGenerator _codeGenerator;

        public WarehouseOutboundRequestService(
            IUnitOfWork unitOfWork,
            INotificationService notificationService,
            ICodeGenerator codeGenerator)
        {
            _unitOfWork = unitOfWork;
            _notificationService = notificationService;
            _codeGenerator = codeGenerator;
        }

        public async Task<IServiceResult> CreateRequestAsync(Guid managerUserId, WarehouseOutboundRequestCreateDto dto)
        {
            var manager = await _unitOfWork.BusinessManagerRepository.FindByUserIdAsync(managerUserId);
            if (manager == null)
                return new ServiceResult(Const.FAIL_READ_CODE, "Không tìm thấy người dùng yêu cầu.");

            var generatedCode = await _codeGenerator.GenerateOutboundRequestCodeAsync();

            var request = new WarehouseOutboundRequest
            {
                OutboundRequestId = Guid.NewGuid(),
                OutboundRequestCode = generatedCode,
                WarehouseId = dto.WarehouseId,
                InventoryId = dto.InventoryId,
                RequestedQuantity = dto.RequestedQuantity,
                Unit = dto.Unit,
                Purpose = dto.Purpose,
                Reason = dto.Reason,
                OrderItemId = dto.OrderItemId,
                RequestedBy = manager.ManagerId,
                Status = WarehouseOutboundRequestStatus.Pending.ToString(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            await _unitOfWork.WarehouseOutboundRequests.CreateAsync(request);
            await _unitOfWork.SaveChangesAsync();

            await _notificationService.NotifyOutboundRequestCreatedAsync(request.OutboundRequestId, manager.ManagerId);

            return new ServiceResult(Const.SUCCESS_CREATE_CODE, "Tạo yêu cầu xuất kho thành công", request.OutboundRequestId);
        }

        public async Task<IServiceResult> GetAllAsync(Guid userId)
        {
            // Ưu tiên xác định là staff trước
            var staff = await _unitOfWork.BusinessStaffRepository.FindByUserIdAsync(userId);
            if (staff != null && !staff.IsDeleted)
            {
                // Lọc theo supervisorId (tức manager của công ty)
                var allRequests = await _unitOfWork.WarehouseOutboundRequests.GetAllAsync();
                var filtered = allRequests
                    .Where(r => r.RequestedBy == staff.SupervisorId)
                    .ToList();

                if (!filtered.Any())
                    return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không có yêu cầu xuất kho nào thuộc công ty bạn.", new List<WarehouseOutboundRequestListItemDto>());

                return new ServiceResult(Const.SUCCESS_READ_CODE, "Lấy danh sách yêu cầu thành công", filtered.Select(x => x.ToListItemDto()).ToList());
            }

            // Nếu không phải là staff, thử xem có phải manager không
            var manager = await _unitOfWork.BusinessManagerRepository.FindByUserIdAsync(userId);
            if (manager != null && !manager.IsDeleted)
            {
                var allRequests = await _unitOfWork.WarehouseOutboundRequests.GetAllAsync();
                var filtered = allRequests
                    .Where(r => r.RequestedBy == manager.ManagerId)
                    .ToList();

                if (!filtered.Any())
                    return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không có yêu cầu xuất kho nào thuộc công ty bạn.", new List<WarehouseOutboundRequestListItemDto>());

                return new ServiceResult(Const.SUCCESS_READ_CODE, "Lấy danh sách yêu cầu thành công", filtered.Select(x => x.ToListItemDto()).ToList());
            }

            return new ServiceResult(Const.FAIL_READ_CODE, "Không xác định được người dùng.");
        }


        public async Task<IServiceResult> GetDetailAsync(Guid outboundRequestId)
        {
            var request = await _unitOfWork.WarehouseOutboundRequests.GetByIdAsync(outboundRequestId);

            if (request == null || request.IsDeleted)
                return new ServiceResult(Const.FAIL_READ_CODE, "Không tìm thấy yêu cầu xuất kho.");

            return new ServiceResult(Const.SUCCESS_READ_CODE, "Lấy chi tiết yêu cầu thành công", request.ToDetailDto());
        }

        public async Task<IServiceResult> AcceptRequestAsync(Guid requestId, Guid staffUserId)
        {
            var request = await _unitOfWork.WarehouseOutboundRequests.GetByIdAsync(requestId);
            if (request == null || request.Status != WarehouseOutboundRequestStatus.Pending.ToString())
                return new ServiceResult(Const.FAIL_UPDATE_CODE, "Yêu cầu không tồn tại hoặc đã xử lý.");

            var staff = await _unitOfWork.BusinessStaffRepository.FindByUserIdAsync(staffUserId);
            if (staff == null)
                return new ServiceResult(Const.FAIL_UPDATE_CODE, "Không xác định được nhân viên xử lý.");

            request.Status = WarehouseOutboundRequestStatus.Accepted.ToString();
            request.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.WarehouseOutboundRequests.Update(request);
            await _unitOfWork.SaveChangesAsync();

            return new ServiceResult(Const.SUCCESS_UPDATE_CODE, "Đã tiếp nhận yêu cầu xuất kho.");
        }
    }
}
