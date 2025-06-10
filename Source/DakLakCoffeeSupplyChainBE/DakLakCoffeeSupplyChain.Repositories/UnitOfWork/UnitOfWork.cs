using DakLakCoffeeSupplyChain.Repositories.DBContext;
using DakLakCoffeeSupplyChain.Repositories.IRepositories;
using DakLakCoffeeSupplyChain.Repositories.Repositories;

namespace DakLakCoffeeSupplyChain.Repositories.UnitOfWork
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly DakLakCoffee_SCMContext context;

        private IRoleRepository? roleRepository;
        private IUserAccountRepository? userAccountRepository;
        private IProductRepository? productRepository;
        private ISystemConfigurationRepository? systemConfigurationRepository;
        private IProcurementPlanRepository? procurementPlanRepository;
        private IProcurementPlanDetailsRepository? procurementPlanDetailsRepository;

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

        public IProductRepository ProductRepository
        {
            get
            {
                return productRepository ??= new ProductRepository(context);
            }
        }

        public ISystemConfigurationRepository SystemConfigurationRepository
        {
            get
            {
                return systemConfigurationRepository ??= new SystemConfigurationRepository(context);
            }
        }
        public IProcurementPlanRepository ProcurementPlanRepository
        {
            get
            {
                return procurementPlanRepository ??= new ProcurementPlanRepository(context);
            }
        }
        public IProcurementPlanDetailsRepository ProcurementPlanDetailsRepository
        {
            get
            {
                return procurementPlanDetailsRepository ??= new ProcurementPlanDetailsRepository(context);
            }
        }
    }
}
