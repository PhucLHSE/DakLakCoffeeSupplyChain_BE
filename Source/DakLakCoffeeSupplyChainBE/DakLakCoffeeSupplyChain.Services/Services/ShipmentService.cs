using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.ShipmentDTOs;
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

        public async Task<IServiceResult> GetAll()
        {
            // Truy vấn các Shipment có Order liên quan đến hợp đồng của Manager hiện tại
            var shipments = await _unitOfWork.ShipmentRepository.GetAllAsync(
                predicate: s =>
                    !s.IsDeleted &&
                    s.Order != null &&
                    s.Order.DeliveryBatch != null &&
                    s.Order.DeliveryBatch.Contract != null,
                include: query => query
                    .Include(s => s.Order)
                    .Include(s => s.DeliveryStaff),
                orderBy: query => query.OrderByDescending(s => s.CreatedAt),
                asNoTracking: true
            );

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
    }
}
