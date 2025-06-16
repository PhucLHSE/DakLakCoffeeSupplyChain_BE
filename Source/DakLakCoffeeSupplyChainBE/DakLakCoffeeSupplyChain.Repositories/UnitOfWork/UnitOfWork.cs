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
        private IContractRepository? contractRepository;
        private IProductRepository? productRepository;
        private ISystemConfigurationRepository? systemConfigurationRepository;
        private IFarmerRepository? farmerRepository; 
        private ICultivationRegistrationRepository? cultivationRegistrationRepository; 
        private IFarmingCommitmentRepository? farmingCommitmentRepository; 
        private ICropSeasonRepository? cropSeasonRepository; 
        private ICropStageRepository cropStageRepository;
        private IProcurementPlanRepository? procurementPlanRepository;
        private IProcessingMethodRepository? processingMethodRepository;
        private IProcurementPlanDetailsRepository? procurementPlanDetailsRepository;
        private ICropProgressRepository? cropProgressRepository;
        private IWarehouseInboundRequestRepository? warehouseInboundRequestRepository;
        private ISystemNotificationRepository? systemNotificationRepository;
        private ISystemNotificationRecipientRepository? systemNotificationRecipientRepository;
        private IBusinessStaffRepository? businessStaffRepository;


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

        public IContractRepository ContractRepository
        {
            get
            {
                return contractRepository ??= new ContractRepository(context);
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
        
        public ICropSeasonRepository CropSeasonRepository
        {
            get
            {
                return cropSeasonRepository ??= new CropSeasonRepository(context);
            }
        }
        
        public ICropStageRepository CropStageRepository
        {
            get
            {
                return cropStageRepository ??= new CropStageRepository(context);
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
        
        public IProcessingMethodRepository ProcessingMethodRepository
        {
            get
            {
                return processingMethodRepository ??= new ProcessingMethodRepository(context);
            }
        }

        public ICropProgressRepository CropProgressRepository
        {
            get
            {
                return cropProgressRepository ??= new CropProgressRepository(context);
            }
        }
        public IWarehouseInboundRequestRepository WarehouseInboundRequests
        {
            get
            {
                return warehouseInboundRequestRepository ??= new WarehouseInboundRequestRepository(context);
            }
        }
        public ISystemNotificationRepository SystemNotificationRepository
        {
            get
            {
                return systemNotificationRepository ??= new SystemNotificationRepository(context);
            }
        }

        public ISystemNotificationRecipientRepository SystemNotificationRecipientRepository
        {
            get
            {
                return systemNotificationRecipientRepository ??= new SystemNotificationRecipientRepository(context);
            }
        }

        public IBusinessStaffRepository BusinessStaffRepository
        {
            get
            {
                return businessStaffRepository ??= new BusinessStaffRepository(context);
            }
        }
    }
}