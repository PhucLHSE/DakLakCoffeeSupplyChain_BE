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
using DakLakCoffeeSupplyChain.Services.Mappers;
using DakLakCoffeeSupplyChain.Common.DTOs.ContractDeliveryBatchDTOs;
using DakLakCoffeeSupplyChain.Common.DTOs.ContractDTOs;
using DakLakCoffeeSupplyChain.Common.Helpers;

namespace DakLakCoffeeSupplyChain.Services.Services
{
    public class ContractDeliveryBatchService : IContractDeliveryBatchService
    {
        private readonly IUnitOfWork _unitOfWork;

        public ContractDeliveryBatchService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork
                ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task<IServiceResult> GetAll(Guid userId)
        {
            Guid? managerId = null;

            // Ưu tiên kiểm tra BusinessManager
            var manager = await _unitOfWork.BusinessManagerRepository.GetByIdAsync(
                predicate: m => m.UserId == userId,
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
                    predicate: s => s.UserId == userId,
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

            // Truy vấn trực tiếp lọc theo SellerId
            var contractDeliveryBatchs = await _unitOfWork.ContractDeliveryBatchRepository.GetAllAsync(
                predicate: cdb => 
                   !cdb.IsDeleted &&
                   cdb.Contract != null &&
                   cdb.Contract.SellerId == managerId,
                include: query => query
                   .Include(cdb => cdb.Contract),
                orderBy: cdb => cdb.OrderBy(cdb => cdb.DeliveryBatchCode),
                asNoTracking: true
            );

            // Kiểm tra nếu không có dữ liệu
            if (contractDeliveryBatchs == null || !contractDeliveryBatchs.Any())
            {
                return new ServiceResult(
                    Const.WARNING_NO_DATA_CODE,
                    Const.WARNING_NO_DATA_MSG,
                    new List<ContractDeliveryBatchViewAllDto>()   // Trả về danh sách rỗng
                );
            }
            else
            {
                // Map danh sách entity sang DTO
                var contractDeliveryBatchDtos = contractDeliveryBatchs
                    .Select(contractDeliveryBatch => contractDeliveryBatch.MapToContractDeliveryBatchViewAllDto())
                    .ToList();

                return new ServiceResult(
                    Const.SUCCESS_READ_CODE,
                    Const.SUCCESS_READ_MSG,
                    contractDeliveryBatchDtos
                );
            }
        }

        public async Task<IServiceResult> GetById(Guid deliveryBatchId)
        {
            // Tìm contractDeliveryBatch theo ID
            var contractDeliveryBatch = await _unitOfWork.ContractDeliveryBatchRepository.GetByIdAsync(
                predicate: cdb =>
                   cdb.DeliveryBatchId == deliveryBatchId && 
                   !cdb.IsDeleted,
                include: query => query
                   .Include(cdb => cdb.Contract)
                   .Include(cdb => cdb.ContractDeliveryItems.Where(cdi => !cdi.IsDeleted))
                      .ThenInclude(cdi => cdi.ContractItem)
                         .ThenInclude(ci => ci.CoffeeType),
                asNoTracking: true
            );

            // Kiểm tra nếu không tìm thấy contract
            if (contractDeliveryBatch == null)
            {
                return new ServiceResult(
                    Const.WARNING_NO_DATA_CODE,
                    Const.WARNING_NO_DATA_MSG,
                    new ContractDeliveryBatchViewDetailDto()  // Trả về DTO rỗng
                );
            }
            else
            {
                // Sắp xếp danh sách ContractDeliveryItems theo DeliveryItemCode tăng dần
                contractDeliveryBatch.ContractDeliveryItems = contractDeliveryBatch.ContractDeliveryItems
                    .OrderBy(cdi => cdi.DeliveryItemCode)
                    .ToList();

                // Map sang DTO chi tiết để trả về
                var contractDto = contractDeliveryBatch.MapToContractDeliveryBatchViewDetailDto();

                return new ServiceResult(
                    Const.SUCCESS_READ_CODE,
                    Const.SUCCESS_READ_MSG,
                    contractDto
                );
            }
        }

        public async Task<IServiceResult> SoftDeleteContractDeliveryBatchById(Guid deliveryBatchId)
        {
            try
            {
                // Tìm contractDeliveryBatch theo ID
                var contractDeliveryBatch = await _unitOfWork.ContractDeliveryBatchRepository.GetByIdAsync(
                    predicate: cdb => cdb.DeliveryBatchId == deliveryBatchId,
                    include: query => query
                       .Include(cdb => cdb.ContractDeliveryItems),
                    asNoTracking: false
                );

                // Kiểm tra nếu không tồn tại
                if (contractDeliveryBatch == null)
                {
                    return new ServiceResult(
                        Const.WARNING_NO_DATA_CODE,
                        Const.WARNING_NO_DATA_MSG
                    );
                }
                else
                {
                    // Đánh dấu contractDeliveryBatch là đã xóa
                    contractDeliveryBatch.IsDeleted = true;
                    contractDeliveryBatch.UpdatedAt = DateHelper.NowVietnamTime();

                    // Đánh dấu tất cả ContractDeliveryItems là đã xóa
                    foreach (var item in contractDeliveryBatch.ContractDeliveryItems)
                    {
                        item.IsDeleted = true;
                        item.UpdatedAt = DateHelper.NowVietnamTime();

                        // Đảm bảo EF theo dõi thay đổi của item
                        await _unitOfWork.ContractDeliveryItemRepository.UpdateAsync(item);
                    }

                    // Cập nhật xoá mềm contractDeliveryBatch ở repository
                    await _unitOfWork.ContractDeliveryBatchRepository.UpdateAsync(contractDeliveryBatch);

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
