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
using DakLakCoffeeSupplyChain.Common.DTOs.ContractDTOs;
using DakLakCoffeeSupplyChain.Services.Mappers;

namespace DakLakCoffeeSupplyChain.Services.Services
{
    public class ContractService : IContractService
    {
        private readonly IUnitOfWork _unitOfWork;

        public ContractService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork 
                ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task<IServiceResult> GetAll()
        {
            // Truy vấn tất cả hợp đồng từ repository
            var contracts = await _unitOfWork.ContractRepository.GetAllAsync(
                include: query => query
                   .Include(c => c.Buyer)
                   .Include(c => c.Seller)
                      .ThenInclude(s => s.User),
                orderBy: u => u.OrderBy(u => u.ContractCode),
                asNoTracking: true
            );

            // Kiểm tra nếu không có dữ liệu
            if (contracts == null || !contracts.Any())
            {
                return new ServiceResult(
                    Const.WARNING_NO_DATA_CODE,
                    Const.WARNING_NO_DATA_MSG,
                    new List<ContractViewAllDto>()   // Trả về danh sách rỗng
                );
            }
            else
            {
                // Map danh sách entity sang DTO
                var contractDtos = contracts
                    .Select(contracts => contracts.MapToContractViewAllDto())
                    .ToList();

                return new ServiceResult(
                    Const.SUCCESS_READ_CODE,
                    Const.SUCCESS_READ_MSG,
                    contractDtos
                );
            }
        }

        public async Task<IServiceResult> GetById(Guid contractId)
        {
            // Tìm contract theo ID
            var contract = await _unitOfWork.ContractRepository.GetByIdAsync(
                predicate: c => c.ContractId == contractId,
                include: query => query
                   .Include(c => c.Buyer)
                   .Include(c => c.Seller)
                      .ThenInclude(s => s.User)
                   .Include(c => c.ContractItems)
                      .ThenInclude(ci => ci.CoffeeType),
                asNoTracking: true
            );

            // Kiểm tra nếu không tìm thấy contract
            if (contract == null)
            {
                return new ServiceResult(
                    Const.WARNING_NO_DATA_CODE,
                    Const.WARNING_NO_DATA_MSG,
                    new ContractViewDetailDto()  // Trả về DTO rỗng
                );
            }
            else
            {
                // Map sang DTO chi tiết để trả về
                var contractDto = contract.MapToContractViewDetailDto();

                return new ServiceResult(
                    Const.SUCCESS_READ_CODE,
                    Const.SUCCESS_READ_MSG,
                    contractDto
                );
            }
        }
    }
}
