using DakLakCoffeeSupplyChain.Repositories.Base;
using DakLakCoffeeSupplyChain.Repositories.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Repositories.IRepositories
{
    public interface IContractRepository : IGenericRepository<Contract>
    {
        // Đếm số Contract đã tạo trong một năm
        Task<int> CountContractsInYearAsync(int year);
    }
}
