using DakLakCoffeeSupplyChain.Repositories.Base;
using DakLakCoffeeSupplyChain.Repositories.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Repositories.IRepositories
{
    public interface IBusinessStaffRepository : IGenericRepository<BusinessStaff>
    {
        Task<List<BusinessStaff>> GetAllWithUserAsync();
        Task<BusinessStaff?> FindByUserIdAsync(Guid userId);
        Task<int> CountStaffCreatedInYearAsync(int year);
        Task<BusinessStaff?> GetByIdWithUserAsync(Guid staffId);
        Task<List<BusinessStaff>> GetBySupervisorIdAsync(Guid supervisorId);
       

    }
}
