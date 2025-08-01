﻿using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.OrderDTOs;
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
    public class OrderService : IOrderService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICodeGenerator _codeGenerator;

        public OrderService(IUnitOfWork unitOfWork, ICodeGenerator codeGenerator)
        {
            _unitOfWork = unitOfWork
                ?? throw new ArgumentNullException(nameof(unitOfWork));

            _codeGenerator = codeGenerator
                ?? throw new ArgumentNullException(nameof(codeGenerator));
        }

        public async Task<IServiceResult> GetAll(Guid userId)
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

            // Truy vấn Order thuộc các DeliveryBatch trong hợp đồng của Manager hiện tại
            var orders = await _unitOfWork.OrderRepository.GetAllAsync(
                predicate: o =>
                    !o.IsDeleted &&
                    o.DeliveryBatch != null &&
                    o.DeliveryBatch.Contract != null &&
                    o.DeliveryBatch.Contract.SellerId == managerId,
                include: query => query
                    .Include(o => o.DeliveryBatch)
                        .ThenInclude(db => db.Contract),
                orderBy: query => query.OrderBy(o => o.OrderDate),
                asNoTracking: true
            );

            // Kiểm tra nếu không có dữ liệu
            if (orders == null || 
                !orders.Any())
            {
                return new ServiceResult(
                    Const.WARNING_NO_DATA_CODE,
                    Const.WARNING_NO_DATA_MSG,
                    new List<OrderViewAllDto>()   // Trả về danh sách rỗng
                );
            }
            else
            {
                // Map danh sách entity sang DTO
                var orderDtos = orders
                    .Select(order => order.MapToOrderViewAllDto())
                    .ToList();

                return new ServiceResult(
                    Const.SUCCESS_READ_CODE,
                    Const.SUCCESS_READ_MSG,
                    orderDtos
                );
            }
        }

        public async Task<IServiceResult> GetById(Guid orderId, Guid userId)
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

            // Tìm order theo ID
            var order = await _unitOfWork.OrderRepository.GetByIdAsync(
                predicate: o =>
                   o.OrderId == orderId &&
                   !o.IsDeleted &&
                   o.DeliveryBatch != null &&
                   o.DeliveryBatch.Contract != null &&
                   o.DeliveryBatch.Contract.SellerId == managerId,
                include: query => query
                   .Include(o => o.DeliveryBatch)
                      .ThenInclude(db => db.Contract)
                   .Include(o => o.OrderItems.Where(oi => !oi.IsDeleted))
                      .ThenInclude(oi => oi.Product),
                asNoTracking: true
            );

            // Kiểm tra nếu không tìm thấy Order
            if (order == null)
            {
                return new ServiceResult(
                    Const.WARNING_NO_DATA_CODE,
                    Const.WARNING_NO_DATA_MSG,
                    new OrderViewDetailsDto()  // Trả về DTO rỗng
                );
            }
            else
            {
                // Sắp xếp danh sách OrderItems theo CreatedAt tăng dần
                order.OrderItems = order.OrderItems
                    .OrderBy(oi => oi.CreatedAt)
                    .ToList();

                // Map sang DTO chi tiết để trả về
                var orderDto = order.MapToOrderViewDetailsDto();

                return new ServiceResult(
                    Const.SUCCESS_READ_CODE,
                    Const.SUCCESS_READ_MSG,
                    orderDto
                );
            }
        }

        public async Task<IServiceResult> Create(OrderCreateDto orderCreateDto, Guid userId)
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

                // Kiểm tra DeliveryBatch có tồn tại và chưa bị xoá
                var deliveryBatch = await _unitOfWork.ContractDeliveryBatchRepository.GetByIdAsync(
                    predicate: b =>
                        b.DeliveryBatchId == orderCreateDto.DeliveryBatchId &&
                        !b.IsDeleted,
                    include: query => query
                        .Include(b => b.Contract),
                    asNoTracking: true
                );

                if (deliveryBatch == null)
                {
                    return new ServiceResult(
                        Const.WARNING_NO_DATA_CODE,
                        "Không tìm thấy lô giao hàng hoặc lô đã bị xoá."
                    );
                }

                if (deliveryBatch.Contract.SellerId != managerId)
                {
                    return new ServiceResult(
                        Const.FAIL_CREATE_CODE,
                        "Bạn không có quyền tạo đơn hàng cho lô giao hàng này."
                    );
                }

                // Kiểm tra số lượng đơn hàng đã tồn tại cho lô này chưa (optional)
                var isOrderExists = await _unitOfWork.OrderRepository.AnyAsync(
                    predicate: o =>
                        o.DeliveryBatchId == orderCreateDto.DeliveryBatchId &&
                        o.IsDeleted == false
                );

                if (isOrderExists)
                {
                    return new ServiceResult(
                        Const.FAIL_CREATE_CODE,
                        "Lô giao hàng này đã có đơn hàng được tạo trước đó."
                    );
                }

                // Sinh mã đơn hàng
                string orderCode = await _codeGenerator.GenerateOrderCodeAsync();

                // Ánh xạ dữ liệu từ DTO vào entity
                var newOrder = orderCreateDto.MapToNewOrder(orderCode);

                // Tính tổng tiền và gán lại
                newOrder.TotalAmount = newOrder.OrderItems
                    .Where(i => !i.IsDeleted)
                    .Sum(i => i.TotalPrice);

                // Lưu vào DB
                await _unitOfWork.OrderRepository.CreateAsync(newOrder);

                // Lưu thay đổi vào database
                var result = await _unitOfWork.SaveChangesAsync();

                if (result > 0)
                {
                    // Truy xuất lại dữ liệu để trả về
                    var createdOrder = await _unitOfWork.OrderRepository.GetByIdAsync(
                        predicate: o => o.OrderId == newOrder.OrderId,
                        include: query => query
                           .Include(o => o.DeliveryBatch)
                              .ThenInclude(b => b.Contract)
                           .Include(o => o.OrderItems)
                              .ThenInclude(i => i.Product),
                        asNoTracking: true
                    );

                    if (createdOrder != null)
                    {
                        // Ánh xạ thực thể đã lưu sang DTO phản hồi
                        var responseDto = createdOrder.MapToOrderViewDetailsDto();

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

        public async Task<IServiceResult> Update(OrderUpdateDto orderUpdateDto, Guid userId)
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

                // Lấy đơn hàng cần cập nhật
                var order = await _unitOfWork.OrderRepository.GetByIdAsync(
                    predicate: o => 
                       o.OrderId == orderUpdateDto.OrderId && 
                       !o.IsDeleted &&
                       o.DeliveryBatch != null &&
                       o.DeliveryBatch.Contract != null &&
                       o.DeliveryBatch.Contract.SellerId == managerId,
                    include: query => query
                        .Include(o => o.OrderItems)
                        .Include(o => o.DeliveryBatch)
                           .ThenInclude(b => b.Contract),
                    asNoTracking: false
                );

                if (order == null)
                {
                    return new ServiceResult(
                        Const.WARNING_NO_DATA_CODE,
                        "Không tìm thấy đơn hàng hoặc bạn không có quyền truy cập."
                    );
                }

                // Ánh xạ dữ liệu từ DTO vào entity
                orderUpdateDto.MapToUpdatedOrder(order);

                // Tập hợp các ID gửi từ client
                var dtoItemIds = orderUpdateDto.OrderItems
                    .Where(i => i.OrderItemId != Guid.Empty)
                    .Select(i => i.OrderItemId)
                    .ToHashSet();

                var now = DateHelper.NowVietnamTime();

                // Xoá mềm những item cũ không còn trong DTO
                foreach (var oldItem in order.OrderItems.Where(i => !i.IsDeleted))
                {
                    if (!dtoItemIds.Contains(oldItem.OrderItemId))
                    {
                        oldItem.IsDeleted = true;
                        oldItem.UpdatedAt = now;

                        await _unitOfWork.OrderItemRepository.UpdateAsync(oldItem);
                    }
                }

                // Xử lý thêm/cập nhật các item
                foreach (var itemDto in orderUpdateDto.OrderItems)
                {
                    OrderItem? existingItem = null;

                    if (itemDto.OrderItemId != Guid.Empty)
                    {
                        existingItem = order.OrderItems
                            .FirstOrDefault(i => i.OrderItemId == itemDto.OrderItemId);
                    }

                    if (existingItem == null)
                    {
                        existingItem = order.OrderItems
                            .FirstOrDefault(i =>
                                i.ProductId == itemDto.ProductId &&
                                i.ContractDeliveryItemId == itemDto.ContractDeliveryItemId &&
                                !i.IsDeleted);
                    }

                    if (existingItem != null)
                    {
                        // Update
                        existingItem.Quantity = itemDto.Quantity ?? 0;
                        existingItem.UnitPrice = itemDto.UnitPrice ?? 0;
                        existingItem.DiscountAmount = itemDto.DiscountAmount ?? 0;
                        existingItem.TotalPrice = existingItem.Quantity * existingItem.UnitPrice - existingItem.DiscountAmount;
                        existingItem.Note = itemDto.Note;
                        existingItem.IsDeleted = false;
                        existingItem.UpdatedAt = now;

                        await _unitOfWork.OrderItemRepository.UpdateAsync(existingItem);
                    }
                    else
                    {
                        // Create mới
                        var newItem = new OrderItem
                        {
                            OrderItemId = Guid.NewGuid(),
                            OrderId = order.OrderId,
                            ProductId = itemDto.ProductId,
                            ContractDeliveryItemId = itemDto.ContractDeliveryItemId,
                            Quantity = itemDto.Quantity ?? 0,
                            UnitPrice = itemDto.UnitPrice ?? 0,
                            DiscountAmount = itemDto.DiscountAmount ?? 0,
                            TotalPrice = (itemDto.Quantity ?? 0) * (itemDto.UnitPrice ?? 0) - (itemDto.DiscountAmount ?? 0),
                            Note = itemDto.Note,
                            CreatedAt = now,
                            UpdatedAt = now,
                            IsDeleted = false
                        };

                        await _unitOfWork.OrderItemRepository.CreateAsync(newItem);
                        order.OrderItems.Add(newItem);
                    }
                }

                // Cập nhật lại tổng tiền đơn hàng
                order.TotalAmount = orderUpdateDto.OrderItems
                    .Sum(i => (i.Quantity ?? 0) * (i.UnitPrice ?? 0) - (i.DiscountAmount ?? 0));

                order.UpdatedAt = now;

                // Cập nhật Order ở repository
                await _unitOfWork.OrderRepository.UpdateAsync(order);

                // Lưu thay đổi vào database
                var result = await _unitOfWork.SaveChangesAsync();

                if (result > 0)
                {
                    // Lấy lại order sau update để trả DTO
                    var updatedOrder = await _unitOfWork.OrderRepository.GetByIdAsync(
                        predicate: o => o.OrderId == orderUpdateDto.OrderId,
                        include: query => query
                           .Include(o => o.DeliveryBatch)
                              .ThenInclude(b => b.Contract)
                           .Include(o => o.OrderItems.Where(i => !i.IsDeleted))
                              .ThenInclude(i => i.Product),
                        asNoTracking: true
                    );

                    if (updatedOrder != null)
                    {
                        // Ánh xạ thực thể đã lưu sang DTO phản hồi
                        var responseDto = updatedOrder.MapToOrderViewDetailsDto();

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

        public async Task<IServiceResult> DeleteOrderById(Guid orderId, Guid userId)
        {
            try
            {
                // Chỉ chấp nhận BusinessManager
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
                        "Bạn không phải BusinessManager nên không có quyền xóa đơn hàng."
                    );
                }

                var managerId = manager.ManagerId;

                // Tìm Order theo ID kèm danh sách OrderItems
                var order = await _unitOfWork.OrderRepository.GetByIdAsync(
                    predicate: o =>
                       o.OrderId == orderId &&
                       !o.IsDeleted &&
                       o.DeliveryBatch != null &&
                       o.DeliveryBatch.Contract != null &&
                       o.DeliveryBatch.Contract.SellerId == managerId,
                    include: query => query
                       .Include(o => o.OrderItems),
                    asNoTracking: false
                );

                // Kiểm tra nếu không tồn tại
                if (order == null)
                {
                    return new ServiceResult(
                        Const.WARNING_NO_DATA_CODE,
                        Const.WARNING_NO_DATA_MSG
                    );
                }
                else
                {
                    // Xóa từng OrderItem trước (nếu có)
                    if (order.OrderItems != null &&
                        order.OrderItems.Any())
                    {
                        foreach (var item in order.OrderItems)
                        {
                            // Xóa OrderItem khỏi repository
                            await _unitOfWork.OrderItemRepository
                                .RemoveAsync(item);
                        }
                    }

                    // Xóa Order khỏi repository
                    await _unitOfWork.OrderRepository
                        .RemoveAsync(order);

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

        public async Task<IServiceResult> SoftDeleteOrderById(Guid orderId, Guid userId)
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
                        "Bạn không phải BusinessManager nên không có quyền xóa mềm đơn hàng."
                    );
                }

                var managerId = manager.ManagerId;

                // Tìm order theo ID
                var order = await _unitOfWork.OrderRepository.GetByIdAsync(
                    predicate: o => 
                       o.OrderId == orderId && 
                       !o.IsDeleted &&
                       o.DeliveryBatch != null &&
                       o.DeliveryBatch.Contract != null &&
                       o.DeliveryBatch.Contract.SellerId == managerId,
                    include: query => query
                       .Include(o => o.OrderItems),
                    asNoTracking: false
                );

                // Kiểm tra nếu không tồn tại
                if (order == null)
                {
                    return new ServiceResult(
                        Const.WARNING_NO_DATA_CODE,
                        Const.WARNING_NO_DATA_MSG
                    );
                }
                else
                {
                    // Đánh dấu order là đã xóa
                    order.IsDeleted = true;
                    order.UpdatedAt = DateHelper.NowVietnamTime();

                    // Đánh dấu tất cả OrderItems là đã xóa
                    foreach (var orderItem in order.OrderItems.Where(i => !i.IsDeleted))
                    {
                        orderItem.IsDeleted = true;
                        orderItem.UpdatedAt = DateHelper.NowVietnamTime();

                        // Đảm bảo EF theo dõi thay đổi của orderItem
                        await _unitOfWork.OrderItemRepository
                            .UpdateAsync(orderItem);
                    }

                    // Cập nhật xoá mềm order ở repository
                    await _unitOfWork.OrderRepository
                        .UpdateAsync(order);

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
