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
        IRoleRepository RoleRepository { get; }

        IUserAccountRepository UserAccountRepository { get; }

        ISystemConfigurationRepository SystemConfigurationRepository { get; }

        IWarehouseInboundRequestRepository WarehouseInboundRequests { get; }
        IFarmerRepository Farmers { get; }
        Task<int> CompleteAsync();
    }
}
