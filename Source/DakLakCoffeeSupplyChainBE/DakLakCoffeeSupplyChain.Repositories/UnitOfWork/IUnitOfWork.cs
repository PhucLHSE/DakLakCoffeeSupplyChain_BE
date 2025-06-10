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

        ISystemConfigurationRepository SystemConfigurationRepository { get; }
        ICropSeasonRepository CropSeasonRepository { get; }
        IFarmerRepository FarmerRepository { get; }
        ICultivationRegistrationRepository CultivationRegistrationRepository { get; }
        IFarmingCommitmentRepository FarmingCommitmentRepository { get; }
        ICropStageRepository CropStageRepository { get; }

    }
}
