using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.ShipmentDTOs.ShipmentDetailDTOs;
using DakLakCoffeeSupplyChain.Common.Helpers;
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
    public class ShipmentDetailService : IShipmentDetailService
    {
        private readonly IUnitOfWork _unitOfWork;

        public ShipmentDetailService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork
                ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task<IServiceResult> Create(ShipmentDetailCreateDto shipmentDetailCreateDto)
        {
            try
            {
                // Kiểm tra OrderItem có tồn tại không
                var orderItem = await _unitOfWork.OrderItemRepository.GetByIdAsync(
                    predicate: oi =>
                        oi.OrderItemId == shipmentDetailCreateDto.OrderItemId &&
                        !oi.IsDeleted,
                    include: query => query
                        .Include(oi => oi.Product)
                        .Include(oi => oi.Order)
                           .ThenInclude(o => o.DeliveryBatch),
                    asNoTracking: true
                );

                if (orderItem == null)
                {
                    return new ServiceResult(
                        Const.WARNING_NO_DATA_CODE,
                        "Không tìm thấy dòng sản phẩm trong đơn hàng."
                    );
                }

                // Kiểm tra Shipment có tồn tại không
                var shipment = await _unitOfWork.ShipmentRepository.GetByIdAsync(
                    predicate: s =>
                        s.ShipmentId == shipmentDetailCreateDto.ShipmentId &&
                        !s.IsDeleted,
                    include: query => query
                       .Include(s => s.Order)
                          .ThenInclude(o => o.DeliveryBatch),
                    asNoTracking: true
                );

                if (shipment == null)
                {
                    return new ServiceResult(
                        Const.WARNING_NO_DATA_CODE,
                        "Không tìm thấy chuyến hàng tương ứng."
                    );
                }

                // Kiểm tra Shipment và OrderItem có thuộc cùng hợp đồng (ContractId) không
                var contractIdOfOrder = orderItem.Order?.DeliveryBatch?.ContractId;
                var contractIdOfShipment = shipment.Order?.DeliveryBatch?.ContractId;

                if (contractIdOfOrder == null || 
                    contractIdOfShipment == null || 
                    contractIdOfOrder != contractIdOfShipment)
                {
                    return new ServiceResult(
                        Const.FAIL_CREATE_CODE,
                        "Chuyến hàng và dòng đơn hàng không thuộc cùng hợp đồng."
                    );
                }

                // Kiểm tra Quantity của ShipmentDetail không vượt quá OrderItem.Quantity còn lại
                double shipmentQuantity = shipmentDetailCreateDto.Quantity ?? 0;

                double totalShipped = await _unitOfWork.ShipmentDetailRepository
                    .SumQuantityByOrderItemAsync(shipmentDetailCreateDto.OrderItemId);

                double maxAvailable = (orderItem.Quantity ?? 0) - totalShipped;

                if (shipmentQuantity > maxAvailable)
                {
                    return new ServiceResult(
                        Const.FAIL_CREATE_CODE,
                        $"Số lượng giao vượt quá phần còn lại trong đơn hàng. Còn lại: {maxAvailable} {orderItem.Product?.Unit ?? "đơn vị"}."
                    );
                }

                // Kiểm tra trùng ShipmentDetail (cùng ShipmentId + OrderItemId)
                bool isDuplicate = await _unitOfWork.ShipmentDetailRepository.AnyAsync(
                    predicate: sd =>
                        sd.ShipmentId == shipmentDetailCreateDto.ShipmentId &&
                        sd.OrderItemId == shipmentDetailCreateDto.OrderItemId &&
                        !sd.IsDeleted
                );

                if (isDuplicate)
                {
                    return new ServiceResult(
                        Const.FAIL_CREATE_CODE,
                        "Dòng sản phẩm này đã được thêm vào chuyến hàng."
                    );
                }

                // Tạo ShipmentDetail mới từ DTO và fallback Product.Unit nếu cần
                var newShipmentDetail = shipmentDetailCreateDto.MapToNewShipmentDetail(orderItem.Product?.Unit);

                // Tạo ShipmentDetail ở repository
                await _unitOfWork.ShipmentDetailRepository.CreateAsync(newShipmentDetail);

                // Lưu thay đổi vào database
                var result = await _unitOfWork.SaveChangesAsync();

                if (result > 0)
                {
                    // Truy xuất lại ShipmentDetail để trả về đầy đủ thông tin (có ProductName)
                    var createdShipmentDetail = await _unitOfWork.ShipmentDetailRepository.GetByIdAsync(
                        predicate: sd => sd.ShipmentDetailId == newShipmentDetail.ShipmentDetailId,
                        include: query => query
                            .Include(sd => sd.OrderItem)
                                .ThenInclude(oi => oi.Product),
                        asNoTracking: true
                    );

                    if (createdShipmentDetail != null)
                    {
                        // Ánh xạ thực thể đã lưu sang DTO phản hồi
                        var responseDto = createdShipmentDetail.MapToShipmentDetailViewDto();

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

        public async Task<IServiceResult> DeleteShipmentDetailById(Guid shipmentDetailId)
        {
            try
            {
                // Tìm ShipmentDetail theo ID
                var shipmentDetail = await _unitOfWork.ShipmentDetailRepository.GetByIdAsync(
                    predicate: sd => sd.ShipmentDetailId == shipmentDetailId,
                    include: query => query
                           .Include(sd => sd.Shipment)
                           .Include(sd => sd.OrderItem),
                    asNoTracking: false
                );

                // Kiểm tra nếu không tồn tại
                if (shipmentDetail == null)
                {
                    return new ServiceResult(
                        Const.WARNING_NO_DATA_CODE,
                        Const.WARNING_NO_DATA_MSG
                    );
                }
                else
                {
                    // Xóa shipmentDetail khỏi repository
                    await _unitOfWork.ShipmentDetailRepository.RemoveAsync(shipmentDetail);

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

        public async Task<IServiceResult> SoftDeleteShipmentDetailById(Guid shipmentDetailId)
        {
            try
            {
                // Tìm ShipmentDetail theo ID
                var shipmentDetail = await _unitOfWork.ShipmentDetailRepository.GetByIdAsync(
                    predicate: sd =>
                       sd.ShipmentDetailId == shipmentDetailId &&
                       !sd.IsDeleted,
                    asNoTracking: false
                );

                // Kiểm tra nếu không tồn tại
                if (shipmentDetail == null)
                {
                    return new ServiceResult(
                        Const.WARNING_NO_DATA_CODE,
                        Const.WARNING_NO_DATA_MSG
                    );
                }
                else
                {
                    // Đánh dấu shipmentDetail là đã xóa
                    shipmentDetail.IsDeleted = true;
                    shipmentDetail.UpdatedAt = DateHelper.NowVietnamTime();

                    // Cập nhật xoá mềm shipmentDetail ở repository
                    await _unitOfWork.ShipmentDetailRepository.UpdateAsync(shipmentDetail);

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
