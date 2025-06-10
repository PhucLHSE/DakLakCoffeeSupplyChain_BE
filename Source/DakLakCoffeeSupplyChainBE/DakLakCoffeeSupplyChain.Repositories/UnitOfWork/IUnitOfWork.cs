using DakLakCoffeeSupplyChain.Repositories.IRepositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Repositories.UnitOfWork
{
    public interface IUnitOfWork
    {
        Task<int> SaveChangesAsync();

        IRoleRepository RoleRepository { get; }

        IUserAccountRepository UserAccountRepository { get; }

        IProductRepository ProductRepository { get; }

        ISystemConfigurationRepository SystemConfigurationRepository { get; }

        IProcurementPlanRepository ProcurementPlanRepository { get; }

        IProcessingMethodRepository ProcessingMethodRepository { get; }

    }
}
