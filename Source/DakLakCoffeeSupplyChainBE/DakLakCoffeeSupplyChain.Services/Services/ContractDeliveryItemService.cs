using DakLakCoffeeSupplyChain.Common.Helpers;
using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Repositories.UnitOfWork;
using DakLakCoffeeSupplyChain.Services.Base;
using DakLakCoffeeSupplyChain.Services.IServices;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DakLakCoffeeSupplyChain.Common.DTOs.ContractDeliveryBatchDTOs.ContractDeliveryItem;
using DakLakCoffeeSupplyChain.Services.Generators;
using DakLakCoffeeSupplyChain.Services.Mappers;

namespace DakLakCoffeeSupplyChain.Services.Services
{
    public class ContractDeliveryItemService : IContractDeliveryItemService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICodeGenerator _codeGenerator;

        public ContractDeliveryItemService(IUnitOfWork unitOfWork, ICodeGenerator codeGenerator)
        {
            _unitOfWork = unitOfWork
                ?? throw new ArgumentNullException(nameof(unitOfWork));

            _codeGenerator = codeGenerator
                ?? throw new ArgumentNullException(nameof(codeGenerator));
        }

        public async Task<IServiceResult> Create(ContractDeliveryItemCreateDto contractDeliveryItemDto)
        {
            try
            {
                // Kiểm tra ContractDeliveryBatch có tồn tại không
                var contractDeliveryBatch = await _unitOfWork.ContractDeliveryBatchRepository.GetByIdAsync(
                    predicate: cdb =>
                       cdb.DeliveryBatchId == contractDeliveryItemDto.DeliveryBatchId &&
                       !cdb.IsDeleted,
                    asNoTracking: true
                );

                if (contractDeliveryBatch == null)
                {
                    return new ServiceResult(
                        Const.WARNING_NO_DATA_CODE,
                        "Không tìm thấy đợt giao hàng tương ứng."
                    );
                }

                // Kiểm tra ContractItem có tồn tại không và thuộc cùng hợp đồng với DeliveryBatch
                var contractItem = await _unitOfWork.ContractItemRepository.GetByIdAsync(
                    predicate: ci => 
                       ci.ContractItemId == contractDeliveryItemDto.ContractItemId && 
                       !ci.IsDeleted,
                    include: query => query
                       .Include(x => x.Contract),
                    asNoTracking: true
                );

                if (contractItem == null || contractItem.ContractId != contractDeliveryBatch.ContractId)
                {
                    return new ServiceResult(
                        Const.FAIL_CREATE_CODE, 
                        "ContractItem không hợp lệ hoặc không thuộc hợp đồng này."
                    );
                }

                // Tổng FulfilledQuantity + PlannedQuantity <= Quantity trong ContractItem
                double totalPlanned = await _unitOfWork.ContractDeliveryItemRepository.SumPlannedQuantityAsync(contractItem.ContractItemId);

                if ((totalPlanned + (contractDeliveryItemDto.PlannedQuantity ?? 0)) > contractItem.Quantity)
                {
                    return new ServiceResult(
                        Const.FAIL_CREATE_CODE, 
                        $"Tổng số lượng vượt quá giới hạn trong hợp đồng ({contractItem.Quantity} kg)."
                    );
                }

                // Kiểm tra trùng trong cùng đợt giao (same DeliveryBatchId + ContractItemId)
                bool isDuplicate = await _unitOfWork.ContractDeliveryItemRepository.AnyAsync(
                    predicate: dli => 
                       dli.DeliveryBatchId == contractDeliveryItemDto.DeliveryBatchId &&
                       dli.ContractItemId == contractDeliveryItemDto.ContractItemId &&
                       !dli.IsDeleted
                );

                if (isDuplicate)
                {
                    return new ServiceResult(
                        Const.FAIL_CREATE_CODE, 
                        "Loại sản phẩm này đã có trong đợt giao hàng."
                    );
                }

                // Sinh mã định danh cho ContractDeliveryItem
                string deliveryItemCode = await _codeGenerator.GenerateContractDeliveryItemCodeAsync(contractDeliveryItemDto.DeliveryBatchId);

                // Map DTO to Entity
                var newContractDeliveryItem = contractDeliveryItemDto.MapToNewContractDeliveryItem(deliveryItemCode);

                // Tạo ContractDeliveryItem ở repository
                await _unitOfWork.ContractDeliveryItemRepository.CreateAsync(newContractDeliveryItem);

                // Lưu thay đổi vào database
                var result = await _unitOfWork.SaveChangesAsync();

                if (result > 0)
                {
                    var createdDeliveryItem = await _unitOfWork.ContractDeliveryItemRepository.GetByIdAsync(
                        predicate: ci => ci.DeliveryItemId == newContractDeliveryItem.DeliveryItemId,
                        include: query => query
                           .Include(x => x.ContractItem)
                              .ThenInclude(ci => ci.CoffeeType)
                           .Include(x => x.DeliveryBatch),
                        asNoTracking: true
                    );

                    if (createdDeliveryItem != null)
                    {
                        var responseDto = createdDeliveryItem.MapToContractDeliveryItemViewDto();

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
                return new ServiceResult(
                    Const.ERROR_EXCEPTION,
                    ex.ToString()
                );
            }
        }

        public async Task<IServiceResult> SoftDeleteContractDeliveryItemById(Guid deliveryItemId)
        {
            try
            {
                // Tìm contractDeliveryItem theo ID
                var contractDeliveryItem = await _unitOfWork.ContractDeliveryItemRepository.GetByIdAsync(
                    predicate: cdi => cdi.DeliveryItemId == deliveryItemId,
                    asNoTracking: false
                );

                // Kiểm tra nếu không tồn tại
                if (contractDeliveryItem == null)
                {
                    return new ServiceResult(
                        Const.WARNING_NO_DATA_CODE,
                        Const.WARNING_NO_DATA_MSG
                    );
                }
                else
                {
                    // Đánh dấu ContractDeliveryItem là đã xóa
                    contractDeliveryItem.IsDeleted = true;
                    contractDeliveryItem.UpdatedAt = DateHelper.NowVietnamTime();

                    // Cập nhật xoá mềm contractDeliveryBatch ở repository
                    await _unitOfWork.ContractDeliveryItemRepository.UpdateAsync(contractDeliveryItem);

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
