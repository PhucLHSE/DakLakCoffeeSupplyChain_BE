﻿using DakLakCoffeeSupplyChain.Common.Helpers;
using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Repositories.UnitOfWork;
using DakLakCoffeeSupplyChain.Services.Base;
using DakLakCoffeeSupplyChain.Services.IServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using DakLakCoffeeSupplyChain.Common.DTOs.OrderDTOs.OrderItemDTOs;
using DakLakCoffeeSupplyChain.Services.Mappers;

namespace DakLakCoffeeSupplyChain.Services.Services
{
    public class OrderItemService : IOrderItemService
    {
        private readonly IUnitOfWork _unitOfWork;

        public OrderItemService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork
                ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task<IServiceResult> Create(OrderItemCreateDto orderItemCreateDto, Guid userId)
        {
            try
            {
                // Xác định managerId từ userId
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
                        predicate:
                           s => s.UserId == userId &&
                           !s.IsDeleted,
                        asNoTracking: true
                    );

                    if (staff != null)
                        managerId = staff.SupervisorId;
                }

                if (managerId == null)
                {
                    return new ServiceResult(
                        Const.WARNING_NO_DATA_CODE,
                        "Không xác định được Manager hoặc Supervisor từ userId."
                    );
                }

                // Kiểm tra Order có tồn tại không
                var order = await _unitOfWork.OrderRepository.GetByIdAsync(
                    predicate: o =>
                       o.OrderId == orderItemCreateDto.OrderId &&
                       !o.IsDeleted,
                    asNoTracking: true
                );

                if (order == null)
                {
                    return new ServiceResult(
                        Const.WARNING_NO_DATA_CODE,
                        "Không tìm thấy đơn hàng tương ứng."
                    );
                }

                // Kiểm tra ContractDeliveryItem có tồn tại không và thuộc cùng hợp đồng với Order
                var contractDeliveryItem = await _unitOfWork.ContractDeliveryItemRepository.GetByIdAsync(
                    predicate: cdi =>
                       cdi.DeliveryItemId == orderItemCreateDto.ContractDeliveryItemId &&
                       !cdi.IsDeleted,
                    include: query => query
                       .Include(cdi => cdi.ContractItem),
                    asNoTracking: true
                );

                if (contractDeliveryItem == null)
                {
                    return new ServiceResult(
                        Const.FAIL_CREATE_CODE,
                        "Không tìm thấy dòng đợt giao hàng tương ứng."
                    );
                }

                // Xác định ContractItem tương ứng
                var contractItem = contractDeliveryItem.ContractItem;

                if (contractItem == null)
                {
                    return new ServiceResult(
                        Const.FAIL_CREATE_CODE, 
                        "Không xác định được dòng hợp đồng tương ứng."
                    );
                }

                // Kiểm tra quyền sở hữu hợp đồng (qua contractItem -> contract -> sellerId)
                var contract = await _unitOfWork.ContractRepository.GetByIdAsync(
                    predicate: c =>
                        c.ContractId == contractItem.ContractId &&
                        !c.IsDeleted,
                    asNoTracking: true
                );

                if (contract == null || 
                    contract.SellerId != managerId)
                {
                    return new ServiceResult(
                        Const.WARNING_NO_DATA_CODE,
                        "Bạn không có quyền tạo mục đơn hàng cho hợp đồng này."
                    );
                }

                // Kiểm tra Product có tồn tại không
                var product = await _unitOfWork.ProductRepository.GetByIdAsync(
                    predicate: p =>
                        p.ProductId == orderItemCreateDto.ProductId &&
                        !p.IsDeleted,
                    asNoTracking: true
                );

                if (product == null)
                {
                    return new ServiceResult(
                        Const.WARNING_NO_DATA_CODE,
                        "Không tìm thấy sản phẩm tương ứng."
                    );
                }

                // Kiểm tra OrderItem trùng trong đơn hàng (OrderId + ContractDeliveryItemId + ProductId)
                bool isDuplicateOrderItem = await _unitOfWork.OrderItemRepository.AnyAsync(
                    predicate: oi =>
                        oi.OrderId == orderItemCreateDto.OrderId &&
                        oi.ContractDeliveryItemId == orderItemCreateDto.ContractDeliveryItemId &&
                        oi.ProductId == orderItemCreateDto.ProductId &&
                        !oi.IsDeleted
                );

                if (isDuplicateOrderItem)
                {
                    return new ServiceResult(
                        Const.FAIL_CREATE_CODE,
                        "Sản phẩm này đã tồn tại trong đơn hàng theo đợt giao tương ứng."
                    );
                }

                // Kiểm tra tổng quantity các OrderItem trong ContractDeliveryItem không vượt quá PlannedQuantity
                double totalOrdered = await _unitOfWork.OrderItemRepository.SumQuantityByContractDeliveryItemAsync(
                    orderItemCreateDto.ContractDeliveryItemId
                );

                double currentQuantity = orderItemCreateDto.Quantity ?? 0;
                double plannedQuantity = contractDeliveryItem.PlannedQuantity;

                if ((totalOrdered + currentQuantity) > plannedQuantity)
                {
                    return new ServiceResult(
                        Const.FAIL_CREATE_CODE,
                        $"Số lượng vượt quá số lượng dự kiến trong đợt giao ({plannedQuantity} kg)."
                    );
                }

                // Lấy UnitPrice và DiscountAmount từ ContractDeliveryItem nếu DTO không cung cấp
                double? unitPrice = orderItemCreateDto.UnitPrice 
                    ?? contractItem.UnitPrice;
                double? discount = orderItemCreateDto.DiscountAmount 
                    ?? contractItem.DiscountAmount;

                if (unitPrice is null || 
                    unitPrice <= 0)
                {
                    return new ServiceResult(
                        Const.FAIL_CREATE_CODE,
                        "Không xác định được đơn giá từ DTO hoặc ContractDeliveryItem."
                    );
                }

                if (discount < 0)
                {
                    return new ServiceResult(
                        Const.FAIL_CREATE_CODE,
                        "Giảm giá không hợp lệ."
                    );
                }

                // Tạo OrderItem mới từ DTO
                var newOrderItem = orderItemCreateDto.MapToNewOrderItem(
                    unitPrice.Value, 
                    discount.GetValueOrDefault()
                );

                // Tạo OrderItem ở repository
                await _unitOfWork.OrderItemRepository.CreateAsync(newOrderItem);

                // Lưu thay đổi vào database
                var result = await _unitOfWork.SaveChangesAsync();

                if (result > 0)
                {
                    // Truy vấn lại để trả về thông tin đầy đủ (có ProductName)
                    var createdOrderItem = await _unitOfWork.OrderItemRepository.GetByIdAsync(
                        predicate: oi => oi.OrderItemId == newOrderItem.OrderItemId,
                        include: query => query
                           .Include(oi => oi.Product),
                        asNoTracking: true
                    );

                    if (createdOrderItem != null)
                    {
                        // Ánh xạ thực thể đã lưu sang DTO phản hồi
                        var responseDto = createdOrderItem.MapToOrderItemViewDto();

                        return new ServiceResult(
                            Const.SUCCESS_CREATE_CODE,
                            Const.SUCCESS_CREATE_MSG,
                            responseDto
                        );
                    }

                    return new ServiceResult(
                        Const.FAIL_CREATE_CODE,
                        "Tạo thành công nhưng không truy xuất được dữ liệu vừa tạo."
                    );
                }
                else
                {
                    return new ServiceResult(
                        Const.FAIL_CREATE_CODE,
                        Const.FAIL_CREATE_MSG
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

        public async Task<IServiceResult> Update(OrderItemUpdateDto orderItemUpdateDto, Guid userId)
        {
            try
            {
                // Xác định managerId từ userId
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
                        predicate:
                           s => s.UserId == userId &&
                           !s.IsDeleted,
                        asNoTracking: true
                    );

                    if (staff != null)
                        managerId = staff.SupervisorId;
                }

                if (managerId == null)
                {
                    return new ServiceResult(
                        Const.WARNING_NO_DATA_CODE,
                        "Không xác định được Manager hoặc Supervisor từ userId."
                    );
                }

                // Tìm orderItem theo ID
                var orderItem = await _unitOfWork.OrderItemRepository.GetByIdAsync(
                    predicate: oi =>
                       oi.OrderItemId == orderItemUpdateDto.OrderItemId &&
                       !oi.IsDeleted,
                    include: query => query
                           .Include(oi => oi.ContractDeliveryItem),
                    asNoTracking: false
                );

                // Nếu không tìm thấy
                if (orderItem == null || 
                    orderItem.IsDeleted)
                {
                    return new ServiceResult(
                        Const.FAIL_UPDATE_CODE,
                        "Không tìm thấy mục đơn hàng cần cập nhật."
                    );
                }

                // Kiểm tra đơn hàng có tồn tại không
                var order = await _unitOfWork.OrderRepository.GetByIdAsync(
                    predicate: o =>
                        o.OrderId == orderItemUpdateDto.OrderId &&
                        !o.IsDeleted,
                    asNoTracking: true
                );

                if (order == null)
                {
                    return new ServiceResult(
                        Const.WARNING_NO_DATA_CODE, 
                        "Không tìm thấy đơn hàng tương ứng."
                    );
                }

                // Kiểm tra ContractDeliveryItem
                var contractDeliveryItem = await _unitOfWork.ContractDeliveryItemRepository.GetByIdAsync(
                    predicate: cdi =>
                        cdi.DeliveryItemId == orderItemUpdateDto.ContractDeliveryItemId &&
                        !cdi.IsDeleted,
                    include: query => query
                       .Include(cdi => cdi.ContractItem),
                    asNoTracking: true
                );

                if (contractDeliveryItem == null)
                {
                    return new ServiceResult(
                        Const.FAIL_UPDATE_CODE,
                        "Không tìm thấy đợt giao hàng tương ứng."
                    );
                }

                // Xác định ContractItem tương ứng
                var contractItem = contractDeliveryItem.ContractItem;

                if (contractItem == null)
                {
                    return new ServiceResult(
                        Const.FAIL_UPDATE_CODE, 
                        "Không xác định được dòng hợp đồng.");
                }

                // Kiểm tra quyền sở hữu
                var contract = await _unitOfWork.ContractRepository.GetByIdAsync(
                    predicate: 
                       c => c.ContractId == contractItem.ContractId && 
                       !c.IsDeleted,
                    asNoTracking: true
                );

                if (contract == null || 
                    contract.SellerId != managerId)
                {
                    return new ServiceResult(
                        Const.WARNING_NO_DATA_CODE,
                        "Bạn không có quyền cập nhật mục đơn hàng này."
                    );
                }

                // Kiểm tra Product có tồn tại không
                var product = await _unitOfWork.ProductRepository.GetByIdAsync(
                    predicate: p =>
                        p.ProductId == orderItemUpdateDto.ProductId &&
                        !p.IsDeleted,
                    asNoTracking: true
                );

                if (product == null)
                {
                    return new ServiceResult(
                        Const.WARNING_NO_DATA_CODE, 
                        "Không tìm thấy sản phẩm tương ứng."
                    );
                }

                // Kiểm tra trùng OrderItem (trừ chính nó)
                bool isDuplicate = await _unitOfWork.OrderItemRepository.AnyAsync(
                    predicate: oi =>
                        oi.OrderId == orderItemUpdateDto.OrderId &&
                        oi.ContractDeliveryItemId == orderItemUpdateDto.ContractDeliveryItemId &&
                        oi.ProductId == orderItemUpdateDto.ProductId &&
                        oi.OrderItemId != orderItem.OrderItemId &&
                        !oi.IsDeleted
                );

                if (isDuplicate)
                {
                    return new ServiceResult(
                        Const.FAIL_UPDATE_CODE,
                        "Sản phẩm này đã tồn tại trong đơn hàng theo đợt giao tương ứng."
                    );
                }

                // Kiểm tra tổng Quantity không vượt quá PlannedQuantity
                double totalOrdered = await _unitOfWork.OrderItemRepository
                    .SumQuantityByContractDeliveryItemAsync(orderItemUpdateDto.ContractDeliveryItemId, excludeOrderItemId: orderItem.OrderItemId);

                double newQuantity = orderItemUpdateDto.Quantity ?? 0;
                double plannedQuantity = contractDeliveryItem.PlannedQuantity;

                if ((totalOrdered + newQuantity) > plannedQuantity)
                {
                    return new ServiceResult(
                        Const.FAIL_UPDATE_CODE,
                        $"Số lượng vượt quá số lượng dự kiến trong đợt giao ({plannedQuantity} kg)."
                    );
                }

                // Lấy UnitPrice và DiscountAmount
                double? unitPrice = orderItemUpdateDto.UnitPrice 
                    ?? contractItem.UnitPrice;
                double? discount = orderItemUpdateDto.DiscountAmount 
                    ?? contractItem.DiscountAmount;

                if (unitPrice is null || 
                    unitPrice <= 0)
                {
                    return new ServiceResult(
                        Const.FAIL_UPDATE_CODE, 
                        "Không xác định được đơn giá hợp lệ."
                    );
                }

                if (discount < 0)
                {
                    return new ServiceResult(
                        Const.FAIL_UPDATE_CODE, 
                        "Giảm giá không hợp lệ."
                    );
                }

                // Cập nhật OrderItem từ DTO
                orderItemUpdateDto.MapToUpdateOrderItem(
                    orderItem, 
                    unitPrice.Value, 
                    discount.GetValueOrDefault()
                );

                // Cập nhật OrderItem ở repository
                await _unitOfWork.OrderItemRepository
                    .UpdateAsync(orderItem);

                // Lưu thay đổi vào database
                var result = await _unitOfWork.SaveChangesAsync();

                if (result > 0)
                {
                    // Lấy lại dữ liệu mới để trả về
                    var updatedItem = await _unitOfWork.OrderItemRepository.GetByIdAsync(
                        predicate: oi => 
                           oi.OrderItemId == orderItem.OrderItemId &&
                           !oi.IsDeleted,
                        include: query => query
                           .Include(oi => oi.Product),
                        asNoTracking: true
                    );

                    if (updatedItem != null)
                    {
                        // Ánh xạ thực thể đã lưu sang DTO phản hồi
                        var responseDto = updatedItem.MapToOrderItemViewDto();

                        return new ServiceResult(
                            Const.SUCCESS_UPDATE_CODE,
                            Const.SUCCESS_UPDATE_MSG,
                            responseDto
                        );
                    }

                    return new ServiceResult(
                        Const.FAIL_UPDATE_CODE,
                        "Cập nhật thành công nhưng không truy xuất được dữ liệu vừa cập nhật."
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

        public async Task<IServiceResult> DeleteOrderItemById(Guid orderItemId, Guid userId)
        {
            try
            {
                // Tìm BusinessManager hiện tại từ userId
                var manager = await _unitOfWork.BusinessManagerRepository.GetByIdAsync(
                    predicate: m =>
                       m.UserId == userId &&
                       !m.IsDeleted,
                    asNoTracking: true
                );

                if (manager == null)
                {
                    return new ServiceResult(
                        Const.WARNING_NO_DATA_CODE,
                        "Không tìm thấy BusinessManager tương ứng với tài khoản."
                    );
                }

                var managerId = manager.ManagerId;

                // Tìm orderItem và xác minh quyền sở hữu
                var orderItem = await _unitOfWork.OrderItemRepository.GetByIdAsync(
                    predicate: oi =>
                       oi.OrderItemId == orderItemId &&
                       oi.Order != null &&
                       oi.Order.DeliveryBatch != null &&
                       oi.Order.DeliveryBatch.Contract != null &&
                       oi.Order.DeliveryBatch.Contract.SellerId == managerId,
                    include: query => query
                       .Include(oi => oi.Order)
                          .ThenInclude(o => o.DeliveryBatch)
                             .ThenInclude(db => db.Contract)
                       .Include(oi => oi.ContractDeliveryItem),
                    asNoTracking: false
                );

                // Kiểm tra nếu không tồn tại
                if (orderItem == null)
                {
                    return new ServiceResult(
                        Const.WARNING_NO_DATA_CODE,
                        "Không tìm thấy OrderItem hoặc bạn không có quyền truy cập."
                    );
                }
                else
                {
                    // Xóa orderItem khỏi repository
                    await _unitOfWork.OrderItemRepository
                        .RemoveAsync(orderItem);

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

        public async Task<IServiceResult> SoftDeleteOrderItemById(Guid orderItemId, Guid userId)
        {
            try
            {
                // Tìm BusinessManager hiện tại từ userId
                var manager = await _unitOfWork.BusinessManagerRepository.GetByIdAsync(
                    predicate: m =>
                       m.UserId == userId &&
                       !m.IsDeleted,
                    asNoTracking: true
                );

                if (manager == null)
                {
                    return new ServiceResult(
                        Const.WARNING_NO_DATA_CODE,
                        "Không tìm thấy BusinessManager tương ứng với tài khoản."
                    );
                }

                var managerId = manager.ManagerId;

                // Tìm OrderItem + xác thực quyền sở hữu qua SellerId
                var orderItem = await _unitOfWork.OrderItemRepository.GetByIdAsync(
                    predicate: oi => 
                       oi.OrderItemId == orderItemId && 
                       !oi.IsDeleted &&
                       oi.Order != null &&
                       oi.Order.DeliveryBatch != null &&
                       oi.Order.DeliveryBatch.Contract != null &&
                       oi.Order.DeliveryBatch.Contract.SellerId == managerId,
                    include: query => query
                       .Include(oi => oi.Order)
                          .ThenInclude(o => o.DeliveryBatch)
                             .ThenInclude(db => db.Contract),
                    asNoTracking: false
                );

                // Kiểm tra nếu không tồn tại
                if (orderItem == null)
                {
                    return new ServiceResult(
                        Const.WARNING_NO_DATA_CODE,
                        "Không tìm thấy OrderItem hoặc bạn không có quyền truy cập."
                    );
                }
                else
                {
                    // Đánh dấu orderItem là đã xóa
                    orderItem.IsDeleted = true;
                    orderItem.UpdatedAt = DateHelper.NowVietnamTime();

                    // Cập nhật xoá mềm orderItem ở repository
                    await _unitOfWork.OrderItemRepository
                        .UpdateAsync(orderItem);

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
