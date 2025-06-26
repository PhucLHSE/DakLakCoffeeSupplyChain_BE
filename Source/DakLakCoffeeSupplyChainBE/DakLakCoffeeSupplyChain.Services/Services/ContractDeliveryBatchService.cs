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
    }
}
