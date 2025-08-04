using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.ShipmentDTOs;
using DakLakCoffeeSupplyChain.Common.Helpers;
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

        public ShipmentService(IUnitOfWork unitOfWork, ICodeGenerator codeGenerator)
        {
            _unitOfWork = unitOfWork
                ?? throw new ArgumentNullException(nameof(unitOfWork));

            _codeGenerator = codeGenerator
                ?? throw new ArgumentNullException(nameof(codeGenerator));
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
                        .Include(s => s.DeliveryStaff),
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
                        .Include(s => s.DeliveryStaff),
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
    }
}
