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
using DakLakCoffeeSupplyChain.Services.Mappers;
using DakLakCoffeeSupplyChain.Common.DTOs.OrderDTOs;
using DakLakCoffeeSupplyChain.Common.Helpers;

namespace DakLakCoffeeSupplyChain.Services.Services
{
    public class OrderService : IOrderService
    {
        private readonly IUnitOfWork _unitOfWork;

        public OrderService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork
                ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task<IServiceResult> GetAll(Guid userId)
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

        public async Task<IServiceResult> SoftDeleteOrderById(Guid orderId)
        {
            try
            {
                // Tìm order theo ID
                var order = await _unitOfWork.OrderRepository.GetByIdAsync(
                    predicate: o => 
                       o.OrderId == orderId && 
                       !o.IsDeleted,
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
                    foreach (var orderItem in order.OrderItems)
                    {
                        orderItem.IsDeleted = true;
                        orderItem.UpdatedAt = DateHelper.NowVietnamTime();

                        // Đảm bảo EF theo dõi thay đổi của orderItem
                        await _unitOfWork.OrderItemRepository.UpdateAsync(orderItem);
                    }

                    // Cập nhật xoá mềm order ở repository
                    await _unitOfWork.OrderRepository.UpdateAsync(order);

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
