using DakLakCoffeeSupplyChain.Common.DTOs.WarehouseOutboundRequestDTOs;
using DakLakCoffeeSupplyChain.Common.Enum.WarehouseOutboundRequestEnums;
using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Repositories.Models;
using DakLakCoffeeSupplyChain.Repositories.UnitOfWork;
using DakLakCoffeeSupplyChain.Services.Base;
using DakLakCoffeeSupplyChain.Services.IServices;
using DakLakCoffeeSupplyChain.Services.Generators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

            // ✅ Sinh mã code sử dụng generator
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

        public async Task<IServiceResult> GetDetailAsync(Guid outboundRequestId)
        {
            var request = await _unitOfWork.WarehouseOutboundRequests.GetByIdAsync(outboundRequestId);

            if (request == null || request.IsDeleted)
                return new ServiceResult(Const.FAIL_READ_CODE, "Không tìm thấy yêu cầu xuất kho.");

            var dto = new WarehouseOutboundRequestDetailDto
            {
                OutboundRequestId = request.OutboundRequestId,
                OutboundRequestCode = request.OutboundRequestCode,
                WarehouseId = request.WarehouseId,
                WarehouseName = request.Warehouse?.Name,
                InventoryId = request.InventoryId,
                InventoryName = request.Inventory?.Products?.FirstOrDefault()?.ProductName,
                RequestedQuantity = request.RequestedQuantity,
                Unit = request.Unit,
                Purpose = request.Purpose,
                Reason = request.Reason,
                OrderItemId = request.OrderItemId,
                RequestedBy = request.RequestedBy,
                RequestedByName = request.RequestedByNavigation?.CompanyName,
                Status = request.Status,
                CreatedAt = request.CreatedAt,
                UpdatedAt = request.UpdatedAt
            };

            return new ServiceResult(Const.SUCCESS_READ_CODE, "Lấy chi tiết yêu cầu thành công", dto);
        }

        public async Task<IServiceResult> GetAllAsync()
        {
            var requests = await _unitOfWork.WarehouseOutboundRequests.GetAllAsync();

            var result = requests.Select(x => new WarehouseOutboundRequestListItemDto
            {
                OutboundRequestId = x.OutboundRequestId,
                OutboundRequestCode = x.OutboundRequestCode,
                Status = x.Status,
                WarehouseName = x.Warehouse?.Name,
                RequestedQuantity = x.RequestedQuantity,
                Unit = x.Unit,
                CreatedAt = x.CreatedAt
            }).ToList();

            return new ServiceResult(Const.SUCCESS_READ_CODE, "Lấy danh sách yêu cầu thành công", result);
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
