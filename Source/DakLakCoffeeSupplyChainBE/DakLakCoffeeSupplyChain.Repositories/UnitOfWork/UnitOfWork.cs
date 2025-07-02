using DakLakCoffeeSupplyChain.Repositories;
using DakLakCoffeeSupplyChain.Repositories.DBContext;
using DakLakCoffeeSupplyChain.Repositories.IRepositories;
using DakLakCoffeeSupplyChain.Repositories.IRepositories.DakLakCoffeeSupplyChain.Repositories.IRepositories;
using DakLakCoffeeSupplyChain.Repositories.Models;
using DakLakCoffeeSupplyChain.Repositories.Repositories;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Repositories.UnitOfWork
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly DakLakCoffee_SCMContext context;

        private IRoleRepository? roleRepository;
        private IUserAccountRepository? userAccountRepository;
        private IBusinessManagerRepository? businessManagerRepository;
        private IBusinessBuyerRepository? businessBuyerRepository;
        private IContractRepository? contractRepository;
        private IContractItemRepository? contractItemRepository;
        private IContractDeliveryBatchRepository? contractDeliveryBatchRepository;
        private IContractDeliveryItemRepository? contractDeliveryItemRepository;
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
        private IWarehouseReceiptRepository? warehouseReceiptRepository;
        private IInventoryRepository? inventoryRepository;
        private IWarehouseRepository? warehouseRepository;
        private IWarehouseOutboundRequestRepository? warehouseOutboundRequestRepository;
        private IWarehouseOutboundReceiptRepository? warehouseOutboundReceiptRepository;
        private ICoffeeTypeRepository? coffeeTypeRepository;
        private IProcessingStageRepository? processingStageRepository;
        private IGeneralFarmerReportRepository generalFarmerReportRepository;
        private IProcessingBatchProgressRepository? processingBatchProgressRepository;
        private IProcessingParameterRepository? processingParameterRepository;
        private IProcessingBatchRepository? processingBatchRepository;
        private CropSeasonDetailRepository? cropSeasonDetailRepository;
        private IInventoryLogRepository? inventoryLogRepository;
        private IExpertAdviceRepository? expertAdviceRepository;
        private IAgriculturalExpertRepository? agriculturalExpertRepository;
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

        public IBusinessManagerRepository BusinessManagerRepository
        {
            get
            {
                return businessManagerRepository ??= new BusinessManagerRepository(context);
            }
        }

        public IBusinessBuyerRepository BusinessBuyerRepository
        {
            get
            {
                return businessBuyerRepository ??= new BusinessBuyerRepository(context);
            }
        }

        public IContractRepository ContractRepository
        {
            get
            {
                return contractRepository ??= new ContractRepository(context);
            }
        }

        public IContractItemRepository ContractItemRepository
        {
            get
            {
                return contractItemRepository ??= new ContractItemRepository(context);
            }
        }

        public IContractDeliveryBatchRepository ContractDeliveryBatchRepository
        {
            get
            {
                return contractDeliveryBatchRepository ??= new ContractDeliveryBatchRepository(context);
            }
        }

        public IContractDeliveryItemRepository ContractDeliveryItemRepository
        {
            get
            {
                return contractDeliveryItemRepository ??= new ContractDeliveryItemRepository(context);
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

        public IWarehouseReceiptRepository WarehouseReceipts
        {
            get
            {
                return warehouseReceiptRepository ??= new WarehouseReceiptRepository(context);
            }
        }

        public IInventoryRepository Inventories
        {
            get
            {
                return inventoryRepository ??= new InventoryRepository(context);
            }
        }

        public IWarehouseRepository Warehouses
        {
            get
            {
                return warehouseRepository ??= new WarehouseRepository(context);
            }
        }

        public IWarehouseOutboundRequestRepository WarehouseOutboundRequests
        {
            get
            {
                return warehouseOutboundRequestRepository ??= new WarehouseOutboundRequestRepository(context);
            }
        }

        public IWarehouseOutboundReceiptRepository WarehouseOutboundReceipts
        {
            get
            {
                return warehouseOutboundReceiptRepository ??= new WarehouseOutboundReceiptRepository(context);
            }
        }

        public ICoffeeTypeRepository CoffeeTypeRepository
        {
            get
            {
                return coffeeTypeRepository ??= new CoffeeTypeRepository(context);
            }
        }

        public IProcessingStageRepository ProcessingStageRepository
        {
            get
            {
                return processingStageRepository ??= new ProcessingStageRepository(context);
            }
        }

        public IGeneralFarmerReportRepository GeneralFarmerReportRepository
        {
            get
            {
                return generalFarmerReportRepository ??= new GeneralFarmerReportRepository(context);
            }
        }
        public IProcessingBatchProgressRepository ProcessingBatchProgressRepository
        {
            get
            {
                return processingBatchProgressRepository ??= new ProcessingBatchProgressRepository(context);
            }
        }
        public IProcessingParameterRepository ProcessingParameterRepository
        {
            get
            {
                return processingParameterRepository ??= new ProcessingParameterRepository(context);
            }
        }
        public IProcessingBatchRepository ProcessingBatchRepository
        {
            get
            {
                return processingBatchRepository ??= new ProcessingBatchRepository(context);
            }
        }

        public ICropSeasonDetailRepository CropSeasonDetailRepository
        {
            get
            {
                return cropSeasonDetailRepository ??= new CropSeasonDetailRepository(context);
            }
        }
        public IInventoryLogRepository InventoryLogs
        {
            get
            {
                return inventoryLogRepository ??= new InventoryLogRepository(context);
            }
        }
        public IExpertAdviceRepository ExpertAdviceRepository
        {
             get
            {
                return expertAdviceRepository ??= new ExpertAdviceRepository(context);
            }
        }
        
        public IAgriculturalExpertRepository AgriculturalExpertRepository
        {
            get
            {
                return agriculturalExpertRepository ??= new AgriculturalExpertRepository(context);
            }
        }
    }
}