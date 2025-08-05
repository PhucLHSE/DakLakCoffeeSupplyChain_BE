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

        public async Task<IServiceResult> Create(ShipmentDetailCreateDto shipmentDetailCreateDto, Guid userId)
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

                // Truy xuất OrderItem và quan hệ liên kết với Contract
                var orderItem = await _unitOfWork.OrderItemRepository.GetByIdAsync(
                    predicate: oi =>
                        oi.OrderItemId == shipmentDetailCreateDto.OrderItemId &&
                        !oi.IsDeleted,
                    include: query => query
                        .Include(oi => oi.Product)
                        .Include(oi => oi.Order)
                            .ThenInclude(o => o.DeliveryBatch)
                        .Include(oi => oi.ContractDeliveryItem)
                            .ThenInclude(cdi => cdi.ContractItem)
                                .ThenInclude(ci => ci.Contract),
                    asNoTracking: true
                );

                if (orderItem == null)
                {
                    return new ServiceResult(
                        Const.WARNING_NO_DATA_CODE,
                        "Không tìm thấy dòng sản phẩm trong đơn hàng."
                    );
                }

                // Kiểm tra quyền truy cập hợp đồng qua SellerId
                var sellerId = orderItem.ContractDeliveryItem?.ContractItem?.Contract?.SellerId;

                if (sellerId == null || 
                    sellerId != managerId)
                {
                    return new ServiceResult(
                        Const.FAIL_CREATE_CODE,
                        "Bạn không có quyền thao tác với hợp đồng chứa dòng sản phẩm này."
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
                var newShipmentDetail = shipmentDetailCreateDto
                    .MapToNewShipmentDetail(orderItem.Product?.Unit);

                // Tạo ShipmentDetail ở repository
                await _unitOfWork.ShipmentDetailRepository
                    .CreateAsync(newShipmentDetail);

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

        public async Task<IServiceResult> Update(ShipmentDetailUpdateDto shipmentDetailUpdateDto)
        {
            try
            {
                // Tìm ShipmentDetail theo ID
                var shipmentDetail = await _unitOfWork.ShipmentDetailRepository.GetByIdAsync(
                    predicate: sd =>
                       sd.ShipmentDetailId == shipmentDetailUpdateDto.ShipmentDetailId &&
                       !sd.IsDeleted,
                    include: query => query
                           .Include(sd => sd.OrderItem)
                              .ThenInclude(oi => oi.Product)
                              .Include(sd => sd.Shipment),
                    asNoTracking: false
                );

                // Nếu không tìm thấy
                if (shipmentDetail == null ||
                    shipmentDetail.IsDeleted)
                {
                    return new ServiceResult(
                        Const.FAIL_UPDATE_CODE,
                        "Không tìm thấy thông tin chi tiết lô hàng cần cập nhật."
                    );
                }

                // Kiểm tra OrderItem có tồn tại không
                var orderItem = await _unitOfWork.OrderItemRepository.GetByIdAsync(
                    predicate: oi =>
                       oi.OrderItemId == shipmentDetailUpdateDto.OrderItemId &&
                       !oi.IsDeleted,
                    include: query => query
                       .Include(oi => oi.Product),
                    asNoTracking: true
                );

                if (orderItem == null)
                {
                    return new ServiceResult(
                        Const.WARNING_NO_DATA_CODE,
                        "Không tìm thấy mục đơn hàng tương ứng."
                    );
                }

                // Kiểm tra Shipment có tồn tại không
                var shipment = await _unitOfWork.ShipmentRepository.GetByIdAsync(
                    predicate: s =>
                        s.ShipmentId == shipmentDetailUpdateDto.ShipmentId &&
                        !s.IsDeleted,
                    asNoTracking: true
                );

                if (shipment == null)
                {
                    return new ServiceResult(
                        Const.FAIL_UPDATE_CODE,
                        "Không tìm thấy thông tin lô hàng tương ứng."
                    );
                }

                // Kiểm tra trùng ShipmentDetail (OrderItem trong cùng Shipment, khác ID hiện tại)
                bool isDuplicate = await _unitOfWork.ShipmentDetailRepository.AnyAsync(
                    predicate: sd =>
                        sd.ShipmentId == shipmentDetailUpdateDto.ShipmentId &&
                        sd.OrderItemId == shipmentDetailUpdateDto.OrderItemId &&
                        sd.ShipmentDetailId != shipmentDetail.ShipmentDetailId &&
                        !sd.IsDeleted
                );

                if (isDuplicate)
                {
                    return new ServiceResult(
                        Const.FAIL_UPDATE_CODE,
                        "Mục đơn hàng đã tồn tại trong lô hàng này."
                    );
                }

                // Kiểm tra quantity hợp lệ (có thể bỏ nếu không giới hạn)
                double newQuantity = shipmentDetailUpdateDto.Quantity ?? 0;

                if (newQuantity <= 0)
                {
                    return new ServiceResult(
                        Const.FAIL_UPDATE_CODE,
                        "Số lượng phải lớn hơn 0."
                    );
                }

                // Cập nhật ShipmentDetail từ DTO
                shipmentDetailUpdateDto.MapToUpdateShipmentDetail(shipmentDetail);

                // Cập nhật ShipmentDetail ở repository
                await _unitOfWork.ShipmentDetailRepository.UpdateAsync(shipmentDetail);

                // Lưu thay đổi vào database
                var result = await _unitOfWork.SaveChangesAsync();

                if (result > 0)
                {
                    // Lấy lại dữ liệu vừa cập nhật
                    var updatedShipmentDetail = await _unitOfWork.ShipmentDetailRepository.GetByIdAsync(
                        predicate: sd => sd.ShipmentDetailId == shipmentDetail.ShipmentDetailId,
                        include: query => query
                           .Include(sd => sd.OrderItem)
                              .ThenInclude(oi => oi.Product),
                        asNoTracking: true
                    );

                    if (updatedShipmentDetail != null)
                    {
                        // Ánh xạ thực thể đã lưu sang DTO phản hồi
                        var responseDto = updatedShipmentDetail.MapToShipmentDetailViewDto();

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

        public async Task<IServiceResult> DeleteShipmentDetailById(Guid shipmentDetailId, Guid userId)
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

                // Tìm ShipmentDetail theo ID và kiểm duyệt quyền
                var shipmentDetail = await _unitOfWork.ShipmentDetailRepository.GetByIdAsync(
                    predicate: sd => 
                       sd.ShipmentDetailId == shipmentDetailId &&
                       sd.Shipment.Order.DeliveryBatch.Contract.SellerId == manager.ManagerId,
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

        public async Task<IServiceResult> SoftDeleteShipmentDetailById(Guid shipmentDetailId, Guid userId)
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

                // Tìm ShipmentDetail theo ID
                var shipmentDetail = await _unitOfWork.ShipmentDetailRepository.GetByIdAsync(
                    predicate: sd =>
                       sd.ShipmentDetailId == shipmentDetailId &&
                       !sd.IsDeleted &&
                       !sd.Shipment.IsDeleted &&
                       sd.Shipment.Order.DeliveryBatch.Contract.SellerId == manager.ManagerId,
                    include: query => query
                       .Include(sd => sd.Shipment),
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
