using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.ShipmentDTOs;
using DakLakCoffeeSupplyChain.Common.Helpers;
using DakLakCoffeeSupplyChain.Common.Enum.InventoryLogEnums;
using DakLakCoffeeSupplyChain.Repositories.Models;
using DakLakCoffeeSupplyChain.Repositories.UnitOfWork;
using DakLakCoffeeSupplyChain.Services.Base;
using DakLakCoffeeSupplyChain.Services.Generators;
using DakLakCoffeeSupplyChain.Services.IServices;
using DakLakCoffeeSupplyChain.Services.Mappers;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Services.Services
{
    public class ShipmentService : IShipmentService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICodeGenerator _codeGenerator;
        private readonly INotificationService _notificationService;

        public ShipmentService(
            IUnitOfWork unitOfWork, 
            ICodeGenerator codeGenerator, 
            INotificationService notificationService
        )
        {
            _unitOfWork = unitOfWork
                ?? throw new ArgumentNullException(nameof(unitOfWork));

            _codeGenerator = codeGenerator
                ?? throw new ArgumentNullException(nameof(codeGenerator));

            _notificationService = notificationService
                ?? throw new ArgumentNullException(nameof(notificationService));
        }

        public async Task<IServiceResult> GetAll(Guid userId)
        {
            Guid? managerId = null;
            bool isDeliveryStaff = false;

            // Kiểm tra BusinessManager
            var manager = await _unitOfWork.BusinessManagerRepository.GetByIdAsync(
                predicate: m => 
                   m.UserId == userId && 
                   !m.IsDeleted,
                asNoTracking: true
            );

            if (manager != null)
            {
                managerId = manager.ManagerId;
            }
            else
            {
                // Nếu không phải Manager thì kiểm tra Staff
                var staff = await _unitOfWork.BusinessStaffRepository.GetByIdAsync(
                    predicate: s => 
                       s.UserId == userId && 
                       !s.IsDeleted,
                    asNoTracking: true
                );

                if (staff != null)
                {
                    managerId = staff.SupervisorId;
                }
                else
                {
                    // Nếu không phải Manager hay Staff => DeliveryStaff
                    isDeliveryStaff = true;
                }
            }

            List<Shipment> shipments;

            if (isDeliveryStaff)
            {
                // Truy vấn shipment do user đảm nhận (DeliveryStaff)
                shipments = await _unitOfWork.ShipmentRepository.GetAllAsync(
                    predicate: s => 
                       !s.IsDeleted && 
                       s.DeliveryStaffId == userId,
                    include: query => query
                        .Include(s => s.Order)
                            .ThenInclude(o => o.DeliveryBatch)
                                .ThenInclude(db => db.Contract)
                                    .ThenInclude(c => c.Buyer)
                        .Include(s => s.Order)
                            .ThenInclude(o => o.DeliveryBatch)
                                .ThenInclude(db => db.Contract)
                                    .ThenInclude(c => c.Seller)
                                        .ThenInclude(sm => sm.Warehouses)
                        .Include(s => s.DeliveryStaff)
                        .Include(s => s.ShipmentDetails)
                            .ThenInclude(sd => sd.OrderItem)
                                .ThenInclude(oi => oi.Product),
                    orderBy: query => query.OrderByDescending(s => s.CreatedAt),
                    asNoTracking: true
                );
            }
            else if (managerId != null)
            {
                // Truy vấn shipment thuộc hợp đồng Manager phụ trách
                shipments = await _unitOfWork.ShipmentRepository.GetAllAsync(
                    predicate: s =>
                        !s.IsDeleted &&
                        s.Order != null &&
                        s.Order.DeliveryBatch != null &&
                        s.Order.DeliveryBatch.Contract != null &&
                        s.Order.DeliveryBatch.Contract.SellerId == managerId,
                    include: query => query
                        .Include(s => s.Order)
                            .ThenInclude(o => o.DeliveryBatch)
                                .ThenInclude(db => db.Contract)
                                    .ThenInclude(c => c.Buyer)
                        .Include(s => s.Order)
                            .ThenInclude(o => o.DeliveryBatch)
                                .ThenInclude(db => db.Contract)
                                    .ThenInclude(c => c.Seller)
                                        .ThenInclude(sm => sm.Warehouses)
                        .Include(s => s.DeliveryStaff)
                        .Include(s => s.ShipmentDetails)
                            .ThenInclude(sd => sd.OrderItem)
                                .ThenInclude(oi => oi.Product),
                    orderBy: query => query.OrderByDescending(s => s.CreatedAt),
                    asNoTracking: true
                );
            }
            else
            {
                return new ServiceResult(
                    Const.WARNING_NO_DATA_CODE,
                    "Không xác định được vai trò hợp lệ của tài khoản."
                );
            }

            // Kiểm tra nếu không có dữ liệu
            if (shipments == null ||
                !shipments.Any())
            {
                return new ServiceResult(
                    Const.WARNING_NO_DATA_CODE,
                    Const.WARNING_NO_DATA_MSG,
                    new List<ShipmentViewAllDto>()   // Trả về danh sách rỗng
                );
            }
            else
            {
                // Map danh sách entity sang DTO
                var shipmentDtos = shipments
                    .Select(shipment => shipment.MapToShipmentViewAllDto())
                    .ToList();

                return new ServiceResult(
                    Const.SUCCESS_READ_CODE,
                    Const.SUCCESS_READ_MSG,
                    shipmentDtos
                );
            }
        }

        public async Task<IServiceResult> GetById(Guid shipmentId, Guid userId)
        {
            Guid? managerId = null;
            bool isDeliveryStaff = false;

            // Kiểm tra BusinessManager
            var manager = await _unitOfWork.BusinessManagerRepository.GetByIdAsync(
                predicate: m => 
                   m.UserId == userId && 
                   !m.IsDeleted,
                asNoTracking: true
            );

            if (manager != null)
            {
                managerId = manager.ManagerId;
            }
            else
            {
                // Nếu không phải Manager thì kiểm tra Staff
                var staff = await _unitOfWork.BusinessStaffRepository.GetByIdAsync(
                    predicate: s => 
                       s.UserId == userId && 
                       !s.IsDeleted,
                    asNoTracking: true
                );

                if (staff != null)
                {
                    managerId = staff.SupervisorId;
                }
                else
                {
                    isDeliveryStaff = true;
                }
            }

            // Truy vấn Shipment theo ID, kèm các navigation cần thiết
            var shipment = await _unitOfWork.ShipmentRepository.GetByIdAsync(
                predicate: s =>
                    s.ShipmentId == shipmentId &&
                    !s.IsDeleted &&
                    s.Order != null &&
                    s.Order.DeliveryBatch != null &&
                    s.Order.DeliveryBatch.Contract != null,
                include: query => query
                    .Include(s => s.Order)
                       .ThenInclude(o => o.DeliveryBatch)
                          .ThenInclude(db => db.Contract)
                              .ThenInclude(c => c.Buyer)
                    .Include(s => s.Order)
                       .ThenInclude(o => o.DeliveryBatch)
                          .ThenInclude(db => db.Contract)
                              .ThenInclude(c => c.Seller)
                                  .ThenInclude(sm => sm.Warehouses)
                    .Include(s => s.DeliveryStaff)
                    .Include(s => s.CreatedByNavigation)
                    .Include(s => s.ShipmentDetails.Where(d => !d.IsDeleted))
                        .ThenInclude(sd => sd.OrderItem)
                            .ThenInclude(oi => oi.Product),
                asNoTracking: true
            );

            // Kiểm tra nếu không tìm thấy Shipment
            if (shipment == null)
            {
                return new ServiceResult(
                    Const.WARNING_NO_DATA_CODE,
                    Const.WARNING_NO_DATA_MSG,
                    new ShipmentViewDetailsDto()  // Trả về DTO rỗng
                );
            }
            else
            {
                // Kiểm duyệt quyền truy cập
                if (isDeliveryStaff)
                {
                    if (shipment.DeliveryStaffId != userId)
                    {
                        return new ServiceResult(
                            Const.WARNING_NO_DATA_CODE,
                            "Bạn không có quyền truy cập Shipment này.",
                            new ShipmentViewDetailsDto()
                        );
                    }
                }
                else if (managerId != null)
                {
                    var sellerId = shipment.Order?.DeliveryBatch?.Contract?.SellerId;

                    if (sellerId != managerId)
                    {
                        return new ServiceResult(
                            Const.WARNING_NO_DATA_CODE,
                            "Bạn không có quyền truy cập Shipment này.",
                            new ShipmentViewDetailsDto()
                        );
                    }
                }
                else
                {
                    return new ServiceResult(
                        Const.WARNING_NO_DATA_CODE,
                        "Không xác định được vai trò hợp lệ.",
                        new ShipmentViewDetailsDto()
                    );
                }

                // Sắp xếp danh sách ShipmentDetails theo CreatedAt tăng dần
                shipment.ShipmentDetails = shipment.ShipmentDetails
                    .OrderBy(sd => sd.CreatedAt)
                    .ToList();

                // Map sang DTO chi tiết để trả về
                var shipmentDto = shipment.MapToShipmentViewDetailsDto();

                return new ServiceResult(
                    Const.SUCCESS_READ_CODE,
                    Const.SUCCESS_READ_MSG,
                    shipmentDto
                );
            }
        }

        public async Task<IServiceResult> Create(ShipmentCreateDto shipmentCreateDto, Guid userId)
        {
            try
            {
                Guid? managerId = null;

                // Ưu tiên kiểm tra BusinessManager
                var manager = await _unitOfWork.BusinessManagerRepository.GetByIdAsync(
                    predicate: m =>
                       m.UserId == userId &&
                       !m.IsDeleted,
                    asNoTracking: true
                );

                if (manager != null)
                {
                    managerId = manager.ManagerId;
                }
                else
                {
                    // Nếu không phải Manager, kiểm tra BusinessStaff
                    var staff = await _unitOfWork.BusinessStaffRepository.GetByIdAsync(
                        predicate: s =>
                           s.UserId == userId &&
                           !s.IsDeleted,
                        asNoTracking: true
                    );

                    if (staff != null)
                    {
                        managerId = staff.SupervisorId;
                    }
                }

                if (managerId == null)
                {
                    return new ServiceResult(
                        Const.WARNING_NO_DATA_CODE,
                        "Không tìm thấy Manager hoặc Staff tương ứng với tài khoản."
                    );
                }

                // Kiểm tra Order có tồn tại không
                var order = await _unitOfWork.OrderRepository.GetByIdAsync(
                    predicate: o =>
                        o.OrderId == shipmentCreateDto.OrderId &&
                        !o.IsDeleted &&
                        o.DeliveryBatch != null &&
                        o.DeliveryBatch.Contract != null &&
                        o.DeliveryBatch.Contract.SellerId == managerId,
                    include: query => query
                        .Include(o => o.OrderItems)
                           .ThenInclude(oi => oi.Product)
                        .Include(o => o.DeliveryBatch)
                           .ThenInclude(db => db.Contract),
                    asNoTracking: true
                );

                if (order == null)
                {
                    return new ServiceResult(
                        Const.WARNING_NO_DATA_CODE,
                        "Không tìm thấy đơn hàng hoặc đơn hàng đã bị xoá."
                    );
                }

                // Kiểm tra OrderItemId trong shipment có thuộc Order không
                var validOrderItemIds = order.OrderItems
                    .Select(i => i.OrderItemId).ToHashSet();

                var invalidItems = shipmentCreateDto.ShipmentDetails
                    .Where(d => !validOrderItemIds.Contains(d.OrderItemId))
                    .Select(d => d.OrderItemId)
                    .ToList();

                if (invalidItems.Any())
                {
                    return new ServiceResult(
                        Const.FAIL_CREATE_CODE,
                        $"Một số sản phẩm không thuộc đơn hàng: {string.Join(", ", invalidItems)}"
                    );
                }

                // Tính tổng số lượng shipment từ chi tiết (nếu không được nhập)
                double calculatedTotalQuantity = shipmentCreateDto.ShipmentDetails
                    .Sum(d => d.Quantity ?? 0);

                if (!shipmentCreateDto.ShippedQuantity.HasValue)
                {
                    shipmentCreateDto.ShippedQuantity = calculatedTotalQuantity;
                }

                // Kiểm tra quá số lượng có thể giao
                var orderItemDeliveryMap = new Dictionary<Guid, double>();

                foreach (var item in shipmentCreateDto.ShipmentDetails)
                {
                    var orderItem = order.OrderItems
                        .First(i => i.OrderItemId == item.OrderItemId);

                    var deliveredQty = await _unitOfWork.ShipmentDetailRepository
                        .GetDeliveredQuantityByOrderItemId(item.OrderItemId);

                    var remainingQty = orderItem.Quantity - deliveredQty;

                    var productName = orderItem.Product?.ProductName 
                        ?? "Không rõ tên sản phẩm";

                    if ((item.Quantity ?? 0) > remainingQty)
                    {
                        return new ServiceResult(
                            Const.FAIL_CREATE_CODE,
                            $"Số lượng giao vượt quá số lượng còn lại của sản phẩm: {productName} (ID: {orderItem.OrderItemId})"
                        );
                    }
                }

                // Sinh mã ShipmentCode
                string shipmentCode = await _codeGenerator
                    .GenerateShipmentCodeAsync();

                // Ánh xạ dữ liệu từ DTO vào entity
                var newShipment = shipmentCreateDto
                    .MapToNewShipment(shipmentCode, userId);

                // Lưu vào DB
                await _unitOfWork.ShipmentRepository
                    .CreateAsync(newShipment);

                // Lưu thay đổi vào database
                var result = await _unitOfWork.SaveChangesAsync();

                if (result > 0)
                {
                    // Truy xuất lại dữ liệu để trả về
                    var createdShipment = await _unitOfWork.ShipmentRepository.GetByIdAsync(
                        predicate: s => s.ShipmentId == newShipment.ShipmentId,
                        include: query => query
                           .Include(s => s.Order)
                              .ThenInclude(o => o.DeliveryBatch)
                           .Include(s => s.ShipmentDetails)
                              .ThenInclude(sd => sd.OrderItem),
                        asNoTracking: true
                    );

                    if (createdShipment != null)
                    {
                        // Ánh xạ thực thể đã lưu sang DTO phản hồi
                        var responseDto = createdShipment.MapToShipmentViewDetailsDto();

                        return new ServiceResult(
                            Const.SUCCESS_CREATE_CODE,
                            Const.SUCCESS_CREATE_MSG,
                            responseDto
                        );
                    }

                    return new ServiceResult(
                        Const.FAIL_CREATE_CODE,
                        "Tạo thành công nhưng không truy xuất được dữ liệu để trả về."
                    );
                }

                return new ServiceResult(
                    Const.FAIL_CREATE_CODE,
                    Const.FAIL_CREATE_MSG
                );
            }
            catch (Exception ex)
            {
                // Xử lý ngoại lệ nếu có lỗi xảy ra trong quá trình
                return new ServiceResult(
                    Const.ERROR_EXCEPTION,
                    ex.Message
                );
            }
        }

        public async Task<IServiceResult> Update(ShipmentUpdateDto shipmentUpdateDto, Guid userId)
        {
            try
            {
                Guid? managerId = null;

                // Ưu tiên kiểm tra BusinessManager
                var manager = await _unitOfWork.BusinessManagerRepository.GetByIdAsync(
                    predicate: m =>
                       m.UserId == userId &&
                       !m.IsDeleted,
                    asNoTracking: true
                );

                if (manager != null)
                {
                    managerId = manager.ManagerId;
                }
                else
                {
                    // Nếu không phải Manager, kiểm tra BusinessStaff
                    var staff = await _unitOfWork.BusinessStaffRepository.GetByIdAsync(
                        predicate: s =>
                           s.UserId == userId &&
                           !s.IsDeleted,
                        asNoTracking: true
                    );

                    if (staff != null)
                    {
                        managerId = staff.SupervisorId;
                    }
                }

                if (managerId == null)
                {
                    return new ServiceResult(
                        Const.WARNING_NO_DATA_CODE,
                        "Không tìm thấy Manager hoặc Staff tương ứng với tài khoản."
                    );
                }

                // Truy vấn shipment cần cập nhật
                var shipment = await _unitOfWork.ShipmentRepository.GetByIdAsync(
                    predicate: s => 
                       s.ShipmentId == shipmentUpdateDto.ShipmentId && 
                       !s.IsDeleted &&
                       s.Order != null &&
                       s.Order.DeliveryBatch != null &&
                       s.Order.DeliveryBatch.Contract != null &&
                       s.Order.DeliveryBatch.Contract.SellerId == managerId,
                    include: query => query
                       .Include(s => s.ShipmentDetails)
                       .Include(s => s.Order)
                          .ThenInclude(o => o.OrderItems)
                       .Include(s => s.Order.DeliveryBatch)
                          .ThenInclude(db => db.Contract),
                    asNoTracking: false
                );

                if (shipment == null)
                {
                    return new ServiceResult(
                        Const.WARNING_NO_DATA_CODE,
                        "Không tìm thấy chuyến giao cần cập nhật."
                    );
                }

                // Lấy danh sách OrderItem thuộc Order
                var validOrderItemIds = shipment.Order?.OrderItems
                    .Select(i => i.OrderItemId).ToHashSet() ?? new();

                var invalidItems = shipmentUpdateDto.ShipmentDetails
                    .Where(d => !validOrderItemIds.Contains(d.OrderItemId))
                    .Select(d => d.OrderItemId)
                    .ToList();

                if (invalidItems.Any())
                {
                    return new ServiceResult(
                        Const.FAIL_UPDATE_CODE,
                        $"Một số sản phẩm không thuộc đơn hàng: {string.Join(", ", invalidItems)}"
                    );
                }

                // Validate số lượng từng item không vượt quá lượng còn lại
                foreach (var itemDto in shipmentUpdateDto.ShipmentDetails)
                {
                    var orderItem = shipment.Order?.OrderItems
                        .FirstOrDefault(i => i.OrderItemId == itemDto.OrderItemId);

                    if (orderItem == null) 
                        continue;

                    var deliveredQty = await _unitOfWork.ShipmentDetailRepository.GetDeliveredQuantityByOrderItemId(
                        itemDto.OrderItemId,
                        excludeShipmentId: shipment.ShipmentId // trừ chuyến hiện tại ra
                    );

                    var remainingQty = orderItem.Quantity - deliveredQty;

                    if ((itemDto.Quantity ?? 0) > remainingQty)
                    {
                        var productName = orderItem.Product?.ProductName 
                            ?? "Không rõ tên sản phẩm";

                        return new ServiceResult(
                            Const.FAIL_UPDATE_CODE,
                            $"Số lượng giao vượt quá số lượng còn lại của sản phẩm: {productName} (ID: {orderItem.OrderItemId})"
                        );
                    }
                }

                // Tính tổng khối lượng giao nếu không được truyền
                var calculatedTotalQty = shipmentUpdateDto.ShipmentDetails
                    .Sum(d => d.Quantity ?? 0);

                if (!shipmentUpdateDto.ShippedQuantity.HasValue)
                    shipmentUpdateDto.ShippedQuantity = calculatedTotalQty;

                // Ánh xạ các trường shipment chính
                shipmentUpdateDto.MapToUpdatedShipment(shipment);

                var now = DateHelper.NowVietnamTime();

                // Tập hợp các ID từ DTO
                var dtoDetailIds = shipmentUpdateDto.ShipmentDetails
                    .Where(x => x.ShipmentDetailId != Guid.Empty)
                    .Select(x => x.ShipmentDetailId)
                    .ToHashSet();

                // Xoá mềm các ShipmentDetail không còn
                foreach (var oldDetail in shipment.ShipmentDetails.Where(sd => !sd.IsDeleted))
                {
                    if (!dtoDetailIds.Contains(oldDetail.ShipmentDetailId))
                    {
                        oldDetail.IsDeleted = true;
                        oldDetail.UpdatedAt = now;

                        await _unitOfWork.ShipmentDetailRepository
                            .UpdateAsync(oldDetail);
                    }
                }

                // Cập nhật hoặc thêm mới các ShipmentDetail
                foreach (var detailDto in shipmentUpdateDto.ShipmentDetails)
                {
                    var existing = shipment.ShipmentDetails
                        .FirstOrDefault(sd => sd.ShipmentDetailId == detailDto.ShipmentDetailId);

                    if (existing != null)
                    {
                        // Cập nhật
                        existing.OrderItemId = detailDto.OrderItemId;
                        existing.Quantity = detailDto.Quantity ?? 0;
                        existing.Unit = detailDto.Unit.ToString();
                        existing.Note = detailDto.Note ?? string.Empty;
                        existing.IsDeleted = false;
                        existing.UpdatedAt = now;

                        await _unitOfWork.ShipmentDetailRepository
                            .UpdateAsync(existing);
                    }
                    else
                    {
                        var newDetail = new ShipmentDetail
                        {
                            ShipmentDetailId = Guid.NewGuid(),
                            ShipmentId = shipment.ShipmentId,
                            OrderItemId = detailDto.OrderItemId,
                            Quantity = detailDto.Quantity ?? 0,
                            Unit = detailDto.Unit.ToString(),
                            Note = detailDto.Note ?? string.Empty,
                            CreatedAt = now,
                            UpdatedAt = now,
                            IsDeleted = false
                        };

                        await _unitOfWork.ShipmentDetailRepository
                            .CreateAsync(newDetail);

                        shipment.ShipmentDetails.Add(newDetail);
                    }
                }

                // Cập nhật Shipment ở repository
                await _unitOfWork.ShipmentRepository
                    .UpdateAsync(shipment);

                // Lưu thay đổi vào database
                var result = await _unitOfWork.SaveChangesAsync();

                if (result > 0)
                {
                    // Lấy lại shipment sau update để trả DTO
                    var updatedShipment = await _unitOfWork.ShipmentRepository.GetByIdAsync(
                        predicate: s => s.ShipmentId == shipment.ShipmentId && !s.IsDeleted,
                        include: query => query
                           .Include(s => s.Order)
                              .ThenInclude(o => o.DeliveryBatch)
                           .Include(s => s.ShipmentDetails.Where(sd => !sd.IsDeleted))
                              .ThenInclude(sd => sd.OrderItem),
                        asNoTracking: true
                    );

                    if (updatedShipment != null)
                    {
                        // Ánh xạ thực thể đã lưu sang DTO phản hồi
                        var responseDto = updatedShipment.MapToShipmentViewDetailsDto();

                        return new ServiceResult(
                            Const.SUCCESS_UPDATE_CODE,
                            Const.SUCCESS_UPDATE_MSG,
                            responseDto
                        );
                    }

                    return new ServiceResult(
                        Const.FAIL_UPDATE_CODE,
                        "Cập nhật thành công nhưng không truy xuất được dữ liệu."
                    );
                }
                else
                {
                    return new ServiceResult(
                        Const.FAIL_UPDATE_CODE,
                        Const.FAIL_UPDATE_MSG
                    );
                }
            }
            catch (Exception ex)
            {
                // Xử lý ngoại lệ nếu có lỗi xảy ra trong quá trình
                return new ServiceResult(
                    Const.ERROR_EXCEPTION,
                    ex.ToString()
                );
            }
        }

        public async Task<IServiceResult> DeleteShipmentById(Guid shipmentId, Guid userId)
        {
            try
            {
                // Kiểm tra BusinessManager từ userId
                var manager = await _unitOfWork.BusinessManagerRepository.GetByIdAsync(
                    predicate: m =>
                        m.UserId == userId &&
                        !m.IsDeleted,
                    asNoTracking: true
                );

                if (manager == null)
                {
                    return new ServiceResult(
                        Const.FAIL_DELETE_CODE,
                        "Bạn không phải BusinessManager nên không có quyền xóa mềm shipment."
                    );
                }

                var managerId = manager.ManagerId;

                // Tìm Shipment theo ID
                var shipment = await _unitOfWork.ShipmentRepository.GetByIdAsync(
                    predicate: s =>
                        s.ShipmentId == shipmentId &&
                        !s.IsDeleted &&
                        s.Order != null &&
                        s.Order.DeliveryBatch != null &&
                        s.Order.DeliveryBatch.Contract != null &&
                        s.Order.DeliveryBatch.Contract.SellerId == managerId,
                    include: query => query
                        .Include(s => s.ShipmentDetails),
                    asNoTracking: false
                );

                // Kiểm tra nếu không tồn tại
                if (shipment == null)
                {
                    return new ServiceResult(
                        Const.WARNING_NO_DATA_CODE,
                        Const.WARNING_NO_DATA_MSG
                    );
                }
                else
                {
                    // Xóa từng ShipmentDetails trước (nếu có)
                    if (shipment.ShipmentDetails != null &&
                        shipment.ShipmentDetails.Any())
                    {
                        foreach (var item in shipment.ShipmentDetails)
                        {
                            // Xóa ShipmentDetails khỏi repository
                            await _unitOfWork.ShipmentDetailRepository
                                .RemoveAsync(item);
                        }
                    }

                    // Xóa Shipment khỏi repository
                    await _unitOfWork.ShipmentRepository
                        .RemoveAsync(shipment);

                    // Lưu thay đổi
                    var result = await _unitOfWork.SaveChangesAsync();

                    // Kiểm tra kết quả
                    if (result > 0)
                    {
                        return new ServiceResult(
                            Const.SUCCESS_DELETE_CODE,
                            Const.SUCCESS_DELETE_MSG
                        );
                    }
                    else
                    {
                        return new ServiceResult(
                            Const.FAIL_DELETE_CODE,
                            Const.FAIL_DELETE_MSG
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                // Trả về lỗi nếu có exception
                return new ServiceResult(
                    Const.ERROR_EXCEPTION,
                    ex.ToString()
                );
            }
        }

        public async Task<IServiceResult> SoftDeleteShipmentById(Guid shipmentId, Guid userId)
        {
            try
            {
                // Kiểm tra BusinessManager từ userId
                var manager = await _unitOfWork.BusinessManagerRepository.GetByIdAsync(
                    predicate: m => 
                       m.UserId == userId && 
                       !m.IsDeleted,
                    asNoTracking: true
                );

                if (manager == null)
                {
                    return new ServiceResult(
                        Const.FAIL_DELETE_CODE,
                        "Bạn không phải BusinessManager nên không có quyền xóa mềm shipment."
                    );
                }

                var managerId = manager.ManagerId;

                // Tìm shipment theo ID
                var shipment = await _unitOfWork.ShipmentRepository.GetByIdAsync(
                    predicate: s =>
                        s.ShipmentId == shipmentId &&
                        !s.IsDeleted &&
                        s.Order != null &&
                        s.Order.DeliveryBatch != null &&
                        s.Order.DeliveryBatch.Contract != null &&
                        s.Order.DeliveryBatch.Contract.SellerId == managerId,
                    include: query => query
                        .Include(s => s.ShipmentDetails),
                    asNoTracking: false
                );

                // Kiểm tra nếu không tồn tại
                if (shipment == null)
                {
                    return new ServiceResult(
                        Const.WARNING_NO_DATA_CODE,
                        Const.WARNING_NO_DATA_MSG
                    );
                }
                else
                {
                    // Đánh dấu shipment là đã xóa
                    shipment.IsDeleted = true;
                    shipment.UpdatedAt = DateHelper.NowVietnamTime();

                    // Đánh dấu tất cả ShipmentDetails là đã xóa
                    foreach (var shipmentDetail in shipment.ShipmentDetails.Where(d => !d.IsDeleted))
                    {
                        shipmentDetail.IsDeleted = true;
                        shipmentDetail.UpdatedAt = DateHelper.NowVietnamTime();

                        // Đảm bảo EF theo dõi thay đổi của shipmentDetail
                        await _unitOfWork.ShipmentDetailRepository
                            .UpdateAsync(shipmentDetail);
                    }

                    // Cập nhật xoá mềm shipment ở repository
                    await _unitOfWork.ShipmentRepository
                        .UpdateAsync(shipment);

                    // Lưu thay đổi
                    var result = await _unitOfWork.SaveChangesAsync();

                    // Kiểm tra kết quả
                    if (result > 0)
                    {
                        return new ServiceResult(
                            Const.SUCCESS_DELETE_CODE,
                            Const.SUCCESS_DELETE_MSG
                        );
                    }
                    else
                    {
                        return new ServiceResult(
                            Const.FAIL_DELETE_CODE,
                            Const.FAIL_DELETE_MSG
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                // Trả về lỗi nếu có exception
                return new ServiceResult(
                    Const.ERROR_EXCEPTION,
                    ex.ToString()
                );
            }
        }

        public async Task<IServiceResult> UpdateStatus(Guid shipmentId, ShipmentStatusUpdateDto statusUpdateDto, Guid userId)
        {
            try
            {
                // Kiểm tra xem user có phải là DeliveryStaff không
                var deliveryStaff = await _unitOfWork.UserAccountRepository.GetByIdAsync(
                    predicate: ds => 
                       ds.UserId == userId && 
                       !ds.IsDeleted,
                    asNoTracking: true
                );

                if (deliveryStaff == null)
                {
                    return new ServiceResult(
                        Const.FAIL_UPDATE_CODE,
                        "Bạn không phải DeliveryStaff nên không có quyền cập nhật trạng thái shipment."
                    );
                }

                // Tìm shipment theo ID và kiểm tra quyền sở hữu
                var shipment = await _unitOfWork.ShipmentRepository.GetByIdAsync(
                    predicate: s =>
                        s.ShipmentId == shipmentId &&
                        !s.IsDeleted &&
                        s.DeliveryStaffId == userId, // Chỉ DeliveryStaff được phân công mới có quyền update
                    include: query => query
                        .Include(s => s.Order)
                            .ThenInclude(o => o.DeliveryBatch)
                                .ThenInclude(db => db.Contract)
                        .Include(s => s.DeliveryStaff)
                        .Include(s => s.ShipmentDetails.Where(sd => !sd.IsDeleted)),
                    asNoTracking: false // Cần tracking để có thể cập nhật
                );

                if (shipment == null)
                {
                    return new ServiceResult(
                        Const.WARNING_NO_DATA_CODE,
                        "Không tìm thấy chuyến giao hoặc bạn không có quyền cập nhật."
                    );
                }

                // Kiểm tra dữ liệu cần thiết trước khi cập nhật
                if (shipment.Order == null)
                {
                    return new ServiceResult(
                        Const.FAIL_UPDATE_CODE,
                        "Không tìm thấy thông tin đơn hàng liên quan."
                    );
                }

                if (shipment.Order.DeliveryBatch == null)
                {
                    return new ServiceResult(
                        Const.FAIL_UPDATE_CODE,
                        "Không tìm thấy thông tin đợt giao hàng."
                    );
                }

                if (shipment.Order.DeliveryBatch.Contract == null)
                {
                    return new ServiceResult(
                        Const.FAIL_UPDATE_CODE,
                        "Không tìm thấy thông tin hợp đồng."
                    );
                }

                // Cập nhật trạng thái
                var oldStatus = shipment.DeliveryStatus;
                shipment.DeliveryStatus = statusUpdateDto.DeliveryStatus.ToString();
                shipment.UpdatedAt = DateHelper.NowVietnamTime();

                // Nếu status là Delivered, cập nhật ReceivedAt
                if (statusUpdateDto.DeliveryStatus == Common.Enum.ShipmentEnums.ShipmentDeliveryStatus.Delivered)
                {
                    shipment.ReceivedAt = DateHelper.NowVietnamTime();
                    
                    // Trừ số lượng từ inventory khi giao hàng thành công
                    await UpdateInventoryOnDelivery(shipment);
                    
                    // Cập nhật khối lượng đã giao trong ContractDeliveryItem
                    await UpdateContractDeliveryItemQuantity(shipment);
                }

                // Tự động cập nhật Order status dựa trên Shipment status
                if (shipment.Order != null)
                {
                    switch (statusUpdateDto.DeliveryStatus)
                    {
                        case Common.Enum.ShipmentEnums.ShipmentDeliveryStatus.InTransit:
                            shipment.Order.Status = Common.Enum.OrderEnums.OrderStatus.Shipped.ToString();
                            break;

                        case Common.Enum.ShipmentEnums.ShipmentDeliveryStatus.Delivered:
                            shipment.Order.Status = Common.Enum.OrderEnums.OrderStatus.Delivered.ToString();
                            break;

                        case Common.Enum.ShipmentEnums.ShipmentDeliveryStatus.Failed:
                            shipment.Order.Status = Common.Enum.OrderEnums.OrderStatus.Failed.ToString();
                            break;

                        case Common.Enum.ShipmentEnums.ShipmentDeliveryStatus.Canceled:
                            shipment.Order.Status = Common.Enum.OrderEnums.OrderStatus.Cancelled.ToString();
                            break;
                    }
                    
                    // Cập nhật thời gian của Order
                    var orderUpdateTime = DateHelper.NowVietnamTime();
                    
                    // Cập nhật Order entity đã được track
                    shipment.Order.Status = shipment.Order.Status;
                    shipment.Order.UpdatedAt = orderUpdateTime;
                    
                    // Cập nhật Order trong repository
                    await _unitOfWork.OrderRepository.UpdateAsync(shipment.Order);
                }

                // Gửi thông báo đến BusinessManager
                try
                {
                    if (shipment.Order?.DeliveryBatch?.Contract?.SellerId != null)
                    {
                        // Kiểm tra BusinessManager có tồn tại không và lấy UserId để gửi notification
                        var businessManager = await _unitOfWork.BusinessManagerRepository.GetByIdAsync(
                            predicate: bm => 
                               bm.ManagerId == shipment.Order.DeliveryBatch.Contract.SellerId && 
                               !bm.IsDeleted,
                            include: bm => bm
                               .Include(bm => bm.User),
                            asNoTracking: true
                        );

                        if (businessManager != null &&
                            businessManager.User != null)
                        {
                            // Lấy tên DeliveryStaff
                            var deliveryStaffName = shipment.DeliveryStaff?.Name ?? shipment.DeliveryStaff?.Name ?? "Không rõ";
                            
                            // Gửi notification đến UserId của BusinessManager
                            await _notificationService.NotifyShipmentStatusUpdatedAsync(
                                shipment.ShipmentId,
                                shipment.Order.OrderId,
                                shipment.ShipmentCode ?? "N/A",
                                shipment.Order.OrderCode ?? "N/A",
                                oldStatus,
                                statusUpdateDto.DeliveryStatus.ToString(),
                                businessManager.User.UserId,  // Sử dụng UserId thay vì ManagerId
                                deliveryStaffName  // Truyền tên DeliveryStaff
                            );
                        }
                        else
                        {
                            Console.WriteLine($"BusinessManager với ID {shipment.Order.DeliveryBatch.Contract.SellerId} không tồn tại hoặc không có User, bỏ qua notification");
                        }
                    }
                }
                catch (Exception notificationEx)
                {
                    // Log lỗi notification nhưng không làm gián đoạn quá trình cập nhật chính
                    // Có thể ghi log vào file hoặc database
                    Console.WriteLine($"Notification error: {notificationEx.Message}");
                    Console.WriteLine($"StackTrace: {notificationEx.StackTrace}");
                }

                // Cập nhật ghi chú nếu có
                if (!string.IsNullOrEmpty(statusUpdateDto.Note))
                {
                    // Có thể lưu note vào một field riêng hoặc log
                    // Tạm thời bỏ qua note vì model Shipment chưa có field này
                }

                // Lưu thay đổi
                try
                {
                    // Cập nhật entity đã được track
                    await _unitOfWork.ShipmentRepository.UpdateAsync(shipment);

                    // Lưu thay đổi
                    var result = await _unitOfWork.SaveChangesAsync();

                    if (result > 0)
                    {
                        // Trả về thông tin shipment đã cập nhật
                        var updatedShipment = await _unitOfWork.ShipmentRepository.GetByIdAsync(
                            predicate: s => 
                               s.ShipmentId == shipmentId && 
                               !s.IsDeleted,
                            include: query => query
                                .Include(s => s.Order)
                                .Include(s => s.DeliveryStaff)
                                .Include(s => s.ShipmentDetails.Where(sd => !sd.IsDeleted)),
                            asNoTracking: true
                        );

                        if (updatedShipment != null)
                        {
                            var shipmentDto = updatedShipment.MapToShipmentViewDetailsDto();
                            return new ServiceResult(
                                Const.SUCCESS_UPDATE_CODE,
                                "Cập nhật trạng thái thành công.",
                                shipmentDto
                            );
                        }

                        return new ServiceResult(
                            Const.SUCCESS_UPDATE_CODE,
                            "Cập nhật trạng thái thành công."
                        );
                    }
                    else
                    {
                        return new ServiceResult(
                            Const.FAIL_UPDATE_CODE,
                            "Cập nhật trạng thái thất bại - không có thay đổi nào được lưu."
                        );
                    }
                }
                catch (Exception saveEx)
                {
                    // Log chi tiết lỗi để debug
                    Console.WriteLine($"SaveChanges error: {saveEx.Message}");
                    Console.WriteLine($"StackTrace: {saveEx.StackTrace}");
                    
                    return new ServiceResult(
                        Const.FAIL_UPDATE_CODE,
                        $"Lỗi khi lưu thay đổi: {saveEx.Message}"
                    );
                }
            }
            catch (Exception ex)
            {
                return new ServiceResult(
                    Const.ERROR_EXCEPTION,
                    ex.ToString()
                );
            }
        }

        private async Task UpdateInventoryOnDelivery(Shipment shipment)
        {
            if (shipment.Order == null || 
                shipment.Order.DeliveryBatch == null || 
                shipment.Order.DeliveryBatch.Contract == null)
            {
                Console.WriteLine("Không thể cập nhật inventory vì thông tin đơn hàng không đầy đủ.");
                return;
            }

            var contract = shipment.Order.DeliveryBatch.Contract;
            
            // Lấy danh sách OrderItem trong đơn hàng với Product và Inventory
            var orderItems = await _unitOfWork.OrderItemRepository.GetAllAsync(
                predicate: oi =>
                    oi.OrderId == shipment.Order.OrderId &&
                    !oi.IsDeleted &&
                    oi.Product != null,
                include: query => query
                    .Include(oi => oi.Product)
                        .ThenInclude(p => p.Inventory),
                asNoTracking: true
            );

            foreach (var orderItem in orderItems)
            {
                var product = orderItem.Product;

                if (product == null || product.Inventory == null) 
                    continue;

                // Tính số lượng cần trừ dựa trên số lượng giao trong shipment
                var deliveredQuantity = shipment.ShipmentDetails
                    .Where(sd => sd.OrderItemId == orderItem.OrderItemId && !sd.IsDeleted)
                    .Sum(sd => sd.Quantity ?? 0);

                if (deliveredQuantity > 0)
                {
                    var inventory = product.Inventory;
                    
                    // ✅ BƯỚC 2: Shipper đến lấy hàng - TRỪ TỒN KHO THỰC TẾ
                    // Kiểm tra số lượng tồn kho có đủ để trừ không
                    if (inventory.Quantity < deliveredQuantity)
                    {
                        Console.WriteLine($"Warning: Inventory {inventory.InventoryCode} không đủ số lượng để trừ. " +
                                        $"Hiện tại: {inventory.Quantity}, Cần trừ: {deliveredQuantity}");
                        // Vẫn trừ nhưng ghi log cảnh báo
                    }

                    // Trừ số lượng từ inventory
                    var oldQuantity = inventory.Quantity;
                    inventory.Quantity = Math.Max(0, inventory.Quantity - deliveredQuantity);
                    inventory.UpdatedAt = DateHelper.NowVietnamTime();

                    // Chuẩn bị cập nhật inventory
                    _unitOfWork.Inventories.PrepareUpdate(inventory);

                    // Tạo InventoryLog để ghi lại thay đổi thực tế
                    var inventoryLog = new InventoryLog
                    {
                        LogId = Guid.NewGuid(),
                        InventoryId = inventory.InventoryId,
                        ActionType = InventoryLogActionType.decrease.ToString(),
                        QuantityChanged = -deliveredQuantity, // Số âm vì đang trừ thực tế
                        UpdatedBy = shipment.DeliveryStaffId,
                        TriggeredBySystem = false,
                        Note = $"Shipper lấy hàng thành công. Shipment: {shipment.ShipmentCode}, " +
                               $"Order: {shipment.Order.OrderCode}, Số lượng trừ: {deliveredQuantity}",
                        LoggedAt = DateHelper.NowVietnamTime(),
                        IsDeleted = false
                    };

                    _unitOfWork.InventoryLogs.PrepareCreate(inventoryLog);

                    // ✅ CẬP NHẬT TRẠNG THÁI PHIẾU XUẤT KHO
                    // Tìm phiếu xuất kho tương ứng với orderItem này
                    var outboundReceipts = await _unitOfWork.WarehouseOutboundReceipts.GetAllAsync(
                        predicate: r => 
                            r.OutboundRequest.OrderItemId == orderItem.OrderItemId && 
                            !r.IsDeleted &&
                            r.Note.Contains("[CONFIRMED:"), // Chỉ cập nhật những phiếu đã xác nhận
                        include: query => query
                            .Include(r => r.OutboundRequest),
                        asNoTracking: true
                    );

                    foreach (var receipt in outboundReceipts)
                    {
                        // Cập nhật note để báo hiệu đã hoàn thành
                        var originalNote = receipt.Note;
                        var completedNote = originalNote.Replace(
                            $"[CONFIRMED:{receipt.Quantity}]", 
                            $"[COMPLETED:{receipt.Quantity}]"
                        );
                        
                        receipt.Note = completedNote;
                        receipt.UpdatedAt = DateTime.UtcNow;
                        
                        _unitOfWork.WarehouseOutboundReceipts.PrepareUpdate(receipt);
                        
                        Console.WriteLine($"Đã chuẩn bị cập nhật phiếu xuất kho {receipt.OutboundReceiptCode} thành trạng thái hoàn thành");
                    }

                    Console.WriteLine($"Đã trừ {deliveredQuantity} từ inventory {inventory.InventoryCode}. " +
                                   $"Từ {oldQuantity} xuống {inventory.Quantity}");
                }
            }
        }

        /// <summary>
        /// Cập nhật khối lượng đã giao trong ContractDeliveryItem khi shipment được giao thành công
        /// </summary>
        private async Task UpdateContractDeliveryItemQuantity(Shipment shipment)
        {
            if (shipment.Order == null || 
                shipment.Order.DeliveryBatch == null || 
                shipment.Order.DeliveryBatch.Contract == null)
            {
                Console.WriteLine("❌ Không thể cập nhật ContractDeliveryItem vì thông tin đơn hàng không đầy đủ.");
                return;
            }

            try
            {
                var contract = shipment.Order.DeliveryBatch.Contract;
                var deliveryBatchId = shipment.Order.DeliveryBatch.DeliveryBatchId; // Chỉ lấy ID, không lấy entity

                Console.WriteLine($"🔄 Bắt đầu cập nhật ContractDeliveryItem cho:");
                Console.WriteLine($"   📋 Contract: {contract.ContractCode}");
                Console.WriteLine($"   🚚 DeliveryBatch ID: {deliveryBatchId}");
                Console.WriteLine($"   📦 Shipment: {shipment.ShipmentCode}");

                // Lấy danh sách ContractDeliveryItem của đợt giao hàng này
                var contractDeliveryItems = await _unitOfWork.ContractDeliveryItemRepository.GetAllAsync(
                    predicate: cdi => 
                        cdi.DeliveryBatchId == deliveryBatchId &&
                        !cdi.IsDeleted,
                    include: query => query
                        .Include(cdi => cdi.ContractItem)
                            .ThenInclude(ci => ci.CoffeeType),
                    asNoTracking: false
                );

                if (!contractDeliveryItems.Any())
                {
                    Console.WriteLine($"❌ Không tìm thấy ContractDeliveryItem cho DeliveryBatch {deliveryBatchId}");
                    return;
                }

                Console.WriteLine($"📦 Tìm thấy {contractDeliveryItems.Count()} ContractDeliveryItem cần cập nhật");

                // Lấy danh sách OrderItem của đơn hàng này
                var orderItems = await _unitOfWork.OrderItemRepository.GetAllAsync(
                    predicate: oi => 
                        oi.OrderId == shipment.Order.OrderId &&
                        !oi.IsDeleted,
                    include: query => query
                        .Include(oi => oi.Product)
                            .ThenInclude(p => p.CoffeeType),
                    asNoTracking: true
                );

                Console.WriteLine($"📋 Tìm thấy {orderItems.Count()} OrderItem trong đơn hàng");

                // Cập nhật từng ContractDeliveryItem
                foreach (var contractDeliveryItem in contractDeliveryItems)
                {
                    var coffeeTypeName = contractDeliveryItem.ContractItem?.CoffeeType?.TypeName ?? "Không rõ";
                    var plannedQuantity = contractDeliveryItem.PlannedQuantity;
                    var oldFulfilledQuantity = contractDeliveryItem.FulfilledQuantity ?? 0;

                    Console.WriteLine($"\n☕ Đang xử lý CoffeeType: {coffeeTypeName}");
                    Console.WriteLine($"   📋 Số lượng dự kiến: {plannedQuantity}");
                    Console.WriteLine($"   📦 Số lượng đã giao (cũ): {oldFulfilledQuantity}");

                    // Tìm OrderItem tương ứng với CoffeeType
                    var relatedOrderItems = orderItems
                        .Where(oi => oi.Product?.CoffeeTypeId == contractDeliveryItem.ContractItem?.CoffeeTypeId)
                        .ToList();

                    if (!relatedOrderItems.Any()) 
                    {
                        Console.WriteLine($"   ⚠️ Không tìm thấy OrderItem tương ứng với CoffeeType {coffeeTypeName}");
                        continue;
                    }

                    Console.WriteLine($"   🔗 Tìm thấy {relatedOrderItems.Count()} OrderItem tương ứng");

                    // Tính tổng số lượng đã giao cho CoffeeType này trong đơn hàng
                    var totalDeliveredQuantity = 0.0;

                    foreach (var orderItem in relatedOrderItems)
                    {
                        // Tính số lượng đã giao cho OrderItem này (bao gồm cả shipment hiện tại)
                        var deliveredQuantity = await _unitOfWork.ShipmentDetailRepository
                            .GetDeliveredQuantityByOrderItemId(orderItem.OrderItemId);

                        totalDeliveredQuantity += deliveredQuantity;
                        
                        Console.WriteLine($"      📦 OrderItem {orderItem.OrderItemId}: {deliveredQuantity} (Product: {orderItem.Product?.ProductName})");
                    }

                    Console.WriteLine($"   📊 Tổng số lượng đã giao: {totalDeliveredQuantity}");

                    // Cập nhật số lượng đã giao trong ContractDeliveryItem
                    contractDeliveryItem.FulfilledQuantity = totalDeliveredQuantity;
                    contractDeliveryItem.UpdatedAt = DateHelper.NowVietnamTime();

                    // Chuẩn bị cập nhật vào repository
                    _unitOfWork.ContractDeliveryItemRepository.PrepareUpdate(contractDeliveryItem);

                    Console.WriteLine($"   ✅ Đã cập nhật ContractDeliveryItem {contractDeliveryItem.DeliveryItemId}: " +
                                   $"Từ {oldFulfilledQuantity} thành {totalDeliveredQuantity}");

                    // Kiểm tra nếu đã giao đủ số lượng
                    if (totalDeliveredQuantity >= plannedQuantity)
                    {
                        Console.WriteLine($"   🎉 CoffeeType {coffeeTypeName} đã được giao đủ số lượng " +
                                       $"({totalDeliveredQuantity}/{plannedQuantity})");
                    }
                    else
                    {
                        var remaining = plannedQuantity - totalDeliveredQuantity;
                        Console.WriteLine($"   ⏳ CoffeeType {coffeeTypeName} còn thiếu: {remaining} " +
                                       $"({totalDeliveredQuantity}/{plannedQuantity})");
                    }
                }

                Console.WriteLine($"✅ Hoàn thành cập nhật ContractDeliveryItem cho Contract {contract.ContractCode}, " +
                               $"DeliveryBatch ID {deliveryBatchId}");
                
                // Không lưu thay đổi ở đây, để lưu cùng với các thay đổi khác
                Console.WriteLine($"💾 Đã chuẩn bị {contractDeliveryItems.Count()} thay đổi ContractDeliveryItem");
                
                Console.WriteLine($"\n🔄 Bắt đầu kiểm tra và cập nhật status của ContractDeliveryBatch...");

                // Kiểm tra và cập nhật status của ContractDeliveryBatch - truyền ID thay vì entity
                await UpdateContractDeliveryBatchStatus(deliveryBatchId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Lỗi khi cập nhật ContractDeliveryItem: {ex.Message}");
                Console.WriteLine($"📋 StackTrace: {ex.StackTrace}");
                // Không throw exception để không làm gián đoạn quá trình cập nhật trạng thái chính
            }
        }

        /// <summary>
        /// Cập nhật khối lượng đã giao cho ContractDeliveryBatch (không cập nhật status)
        /// </summary>
        private async Task UpdateContractDeliveryBatchStatus(Guid deliveryBatchId)
        {
            try
            {
                Console.WriteLine($"🔄 Bắt đầu kiểm tra khối lượng đã giao cho DeliveryBatch ID: {deliveryBatchId}");

                // Lấy lại dữ liệu mới nhất từ database để đảm bảo FulfilledQuantity đã được cập nhật
                Console.WriteLine($"🔄 Đang lấy dữ liệu mới nhất từ database cho DeliveryBatch {deliveryBatchId}...");
                
                var freshDeliveryBatch = await _unitOfWork.ContractDeliveryBatchRepository.GetByIdAsync(
                    predicate: db => db.DeliveryBatchId == deliveryBatchId && !db.IsDeleted,
                    asNoTracking: false
                );

                if (freshDeliveryBatch == null)
                {
                    Console.WriteLine($"❌ Không tìm thấy DeliveryBatch {deliveryBatchId} trong database");
                    return;
                }

                Console.WriteLine($"✅ Đã lấy dữ liệu mới nhất từ database");
                Console.WriteLine($"📋 Status hiện tại: {freshDeliveryBatch.Status}");

                // Lấy tất cả ContractDeliveryItem của đợt giao hàng này
                var contractDeliveryItems = await _unitOfWork.ContractDeliveryItemRepository.GetAllAsync(
                    predicate: cdi => 
                        cdi.DeliveryBatchId == freshDeliveryBatch.DeliveryBatchId &&
                        !cdi.IsDeleted,
                    include: query => query
                        .Include(cdi => cdi.ContractItem)
                            .ThenInclude(ci => ci.CoffeeType),
                    asNoTracking: false
                );

                if (!contractDeliveryItems.Any())
                {
                    Console.WriteLine($"❌ Không tìm thấy ContractDeliveryItem cho DeliveryBatch {deliveryBatchId}");
                    return;
                }

                Console.WriteLine($"📦 Tìm thấy {contractDeliveryItems.Count()} ContractDeliveryItem");

                // Tính tổng khối lượng đã giao
                var totalPlannedQuantity = 0.0;
                var totalFulfilledQuantity = 0.0;

                foreach (var item in contractDeliveryItems)
                {
                    var plannedQuantity = item.PlannedQuantity;
                    var fulfilledQuantity = item.FulfilledQuantity ?? 0;
                    
                    totalPlannedQuantity += plannedQuantity;
                    totalFulfilledQuantity += fulfilledQuantity;

                    var coffeeTypeName = item.ContractItem?.CoffeeType?.TypeName ?? "Không rõ";
                    var completionStatus = fulfilledQuantity >= plannedQuantity ? "✅ HOÀN THÀNH" : "⏳ CHƯA HOÀN THÀNH";
                    
                    Console.WriteLine($"   ☕ {coffeeTypeName}: {fulfilledQuantity}/{plannedQuantity} {completionStatus}");
                }

                Console.WriteLine($"📊 Tổng kết khối lượng đã giao:");
                Console.WriteLine($"   📋 Số lượng dự kiến: {totalPlannedQuantity}");
                Console.WriteLine($"   📦 Số lượng đã giao: {totalFulfilledQuantity}");
                
                var completionPercentage = totalPlannedQuantity > 0 ? (totalFulfilledQuantity / totalPlannedQuantity) * 100 : 0;
                Console.WriteLine($"📊 DeliveryBatch {deliveryBatchId}: " +
                               $"Tiến độ giao hàng: {completionPercentage:F1}% " +
                               $"({totalFulfilledQuantity}/{totalPlannedQuantity})");
                
                Console.WriteLine($"✅ Đã cập nhật khối lượng đã giao cho ContractDeliveryBatch {freshDeliveryBatch.DeliveryBatchCode}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Lỗi khi cập nhật khối lượng đã giao cho ContractDeliveryBatch: {ex.Message}");
                Console.WriteLine($"📋 StackTrace: {ex.StackTrace}");
            }
        }

        public async Task<IServiceResult> GetOrdersForShipment(Guid userId)
        {
            try
            {
                Guid? managerId = null;

                // Kiểm tra BusinessManager
                var manager = await _unitOfWork.BusinessManagerRepository.GetByIdAsync(
                    predicate: m =>
                       m.UserId == userId &&
                       !m.IsDeleted,
                    asNoTracking: true
                );

                if (manager != null)
                {
                    managerId = manager.ManagerId;
                }
                else
                {
                    // Nếu không phải Manager, kiểm tra BusinessStaff
                    var staff = await _unitOfWork.BusinessStaffRepository.GetByIdAsync(
                        predicate: s =>
                           s.UserId == userId &&
                           !s.IsDeleted,
                        asNoTracking: true
                    );

                    if (staff != null)
                    {
                        managerId = staff.SupervisorId;
                    }
                }

                if (managerId == null)
                {
                    return new ServiceResult(
                        Const.WARNING_NO_DATA_CODE,
                        "Không tìm thấy Manager hoặc Staff tương ứng với tài khoản."
                    );
                }

                // Lấy tất cả orders thuộc hợp đồng của Manager, có trạng thái cần giao hàng
                var orders = await _unitOfWork.OrderRepository.GetAllAsync(
                    predicate: o =>
                        !o.IsDeleted &&
                        o.DeliveryBatch != null &&
                        o.DeliveryBatch.Contract != null &&
                        o.DeliveryBatch.Contract.SellerId == managerId &&
                        (o.Status == Common.Enum.OrderEnums.OrderStatus.Preparing.ToString() ||
                         o.Status == Common.Enum.OrderEnums.OrderStatus.Shipped.ToString()),
                    include: query => query
                        .Include(o => o.OrderItems.Where(oi => !oi.IsDeleted))
                            .ThenInclude(oi => oi.Product)
                        .Include(o => o.DeliveryBatch)
                            .ThenInclude(db => db.Contract),
                    orderBy: query => query
                        .OrderByDescending(o => o.CreatedAt),
                    asNoTracking: true
                );

                if (orders == null || !orders.Any())
                {
                    return new ServiceResult(
                        Const.WARNING_NO_DATA_CODE,
                        "Không có đơn hàng nào cần tạo shipment.",
                        new List<OrderForShipmentDto>()
                    );
                }

                var ordersForShipment = new List<OrderForShipmentDto>();

                foreach (var order in orders)
                {
                    var orderItemsInfo = new List<OrderItemForShipmentDto>();

                    foreach (var orderItem in order.OrderItems)
                    {
                        // Tính số lượng đã giao cho từng OrderItem
                        var deliveredQuantity = await _unitOfWork.ShipmentDetailRepository
                            .GetDeliveredQuantityByOrderItemId(orderItem.OrderItemId);

                        var remainingQuantity = orderItem.Quantity - deliveredQuantity;

                        // Chỉ thêm vào danh sách nếu còn số lượng cần giao
                        if (remainingQuantity > 0)
                        {
                            orderItemsInfo.Add(new OrderItemForShipmentDto
                            {
                                OrderItemId = orderItem.OrderItemId,
                                ProductId = orderItem.Product?.ProductId,
                                ProductName = orderItem.Product?.ProductName ?? "Không rõ tên sản phẩm",
                                OrderQuantity = orderItem.Quantity ?? 0,
                                DeliveredQuantity = deliveredQuantity,
                                RemainingQuantity = remainingQuantity ?? 0,
                                Unit = orderItem.Product?.Unit ?? "kg"
                            });
                        }
                    }

                    // Chỉ thêm order vào danh sách nếu còn ít nhất một item cần giao
                    if (orderItemsInfo.Any())
                    {
                        // Tính tổng số lượng đã giao và còn lại
                        var totalDelivered = orderItemsInfo.Sum(oi => oi.DeliveredQuantity);
                        var totalRemaining = orderItemsInfo.Sum(oi => oi.RemainingQuantity);

                        ordersForShipment.Add(new OrderForShipmentDto
                        {
                            OrderId = order.OrderId,
                            OrderCode = order.OrderCode,
                            OrderDate = order.CreatedAt,
                            Status = order.Status,
                            TotalOrderQuantity = order.OrderItems.Sum(oi => oi.Quantity ?? 0),
                            TotalDeliveredQuantity = totalDelivered,
                            TotalRemainingQuantity = totalRemaining,
                            ContractCode = order.DeliveryBatch?.Contract?.ContractCode ?? string.Empty,
                            DeliveryBatchCode = order.DeliveryBatch?.DeliveryBatchCode ?? string.Empty,
                            OrderItems = orderItemsInfo
                        });
                    }
                }

                if (!ordersForShipment.Any())
                {
                    return new ServiceResult(
                        Const.WARNING_NO_DATA_CODE,
                        "Tất cả đơn hàng đã được giao đủ số lượng.",
                        new List<OrderForShipmentDto>()
                    );
                }

                return new ServiceResult(
                    Const.SUCCESS_READ_CODE,
                    "Lấy danh sách orders cần tạo shipment thành công.",
                    ordersForShipment
                );
            }
            catch (Exception ex)
            {
                return new ServiceResult(
                    Const.ERROR_EXCEPTION,
                    $"Lỗi khi lấy danh sách orders: {ex.Message}"
                );
            }
        }
    }
}
