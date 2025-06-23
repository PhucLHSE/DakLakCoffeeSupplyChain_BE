using DakLakCoffeeSupplyChain.Repositories.Base;
using DakLakCoffeeSupplyChain.Repositories.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Repositories.IRepositories
{
    public interface IContractItemRepository : IGenericRepository<ContractItem>
    {
        // Đếm số item trong hợp đồng
        Task<int> CountByContractIdAsync(Guid contractId);
    }
}
