using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.ShipmentDTOs;
using DakLakCoffeeSupplyChain.Common.Helpers;
using DakLakCoffeeSupplyChain.Repositories.Models;
using DakLakCoffeeSupplyChain.Repositories.UnitOfWork;
using DakLakCoffeeSupplyChain.Services.Base;
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

        public ShipmentService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork
                ?? throw new ArgumentNullException(nameof(unitOfWork));
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

        public async Task<IServiceResult> GetById(Guid shipmentId)
        {
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

        public async Task<IServiceResult> SoftDeleteShipmentById(Guid shipmentId)
        {
            try
            {
                // Tìm shipment theo ID
                var shipment = await _unitOfWork.ShipmentRepository.GetByIdAsync(
                    predicate: s =>
                        s.ShipmentId == shipmentId &&
                        !s.IsDeleted &&
                        s.Order != null &&
                        s.Order.DeliveryBatch != null &&
                        s.Order.DeliveryBatch.Contract != null,
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
