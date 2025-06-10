using DakLakCoffeeSupplyChain.Repositories.DBContext;
using DakLakCoffeeSupplyChain.Repositories;
using DakLakCoffeeSupplyChain.Repositories.IRepositories;
using DakLakCoffeeSupplyChain.Repositories.Repositories;
using System.Threading.Tasks;
using DakLakCoffeeSupplyChain.Repositories.Models;

namespace DakLakCoffeeSupplyChain.Repositories.UnitOfWork
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly DakLakCoffee_SCMContext context;

        private IRoleRepository? roleRepository;
        private IUserAccountRepository? userAccountRepository;
        private ISystemConfigurationRepository? systemConfigurationRepository;
        private ICropSeasonRepository? cropSeasonRepository; 
        private IFarmerRepository? farmerRepository; 
        private ICultivationRegistrationRepository? cultivationRegistrationRepository; 
        private IFarmingCommitmentRepository? farmingCommitmentRepository; 
        private ICropStageRepository cropStageRepository;


        public UnitOfWork()
            => context ??= new DakLakCoffee_SCMContext();

        public async Task<int> SaveChangesAsync()
        {
            return await context.SaveChangesAsync();
        }

        public IRoleRepository RoleRepository
        {
            get
            {
                return roleRepository ??= new RoleRepository(context);
            }
        }

        public IUserAccountRepository UserAccountRepository
        {
            get
            {
                return userAccountRepository ??= new UserAccountRepository(context);
            }
        }

        public ISystemConfigurationRepository SystemConfigurationRepository
        {
            get
            {
                return systemConfigurationRepository ??= new SystemConfigurationRepository(context);
            }
        }

        public ICropSeasonRepository CropSeasonRepository
        {
            get
            {
                return cropSeasonRepository ??= new CropSeasonRepository(context);
            }
        }
        public IFarmerRepository FarmerRepository
        {
            get
            {
                return farmerRepository ??= new FarmerRepository(context);
            }
        }
        public ICultivationRegistrationRepository CultivationRegistrationRepository
        {
            get
            {
                return cultivationRegistrationRepository ??= new CultivationRegistrationRepository(context);
            }
        }
        public IFarmingCommitmentRepository FarmingCommitmentRepository
        {
            get
            {
                return farmingCommitmentRepository ??= new FarmingCommitmentRepository(context);
            }
        }
        public ICropStageRepository CropStageRepository
        {
            get
            {
                return cropStageRepository ??= new CropStageRepository(context);
            }
        }
    }
}