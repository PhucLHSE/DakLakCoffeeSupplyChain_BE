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

        IBusinessManagerRepository BusinessManagerRepository { get; }

        IFarmerRepository FarmerRepository { get; }

        IBusinessBuyerRepository BusinessBuyerRepository { get; }

        IContractRepository ContractRepository { get; }

        IContractItemRepository ContractItemRepository { get; }

        IProcurementPlanRepository ProcurementPlanRepository { get; }

        IProcurementPlanDetailsRepository ProcurementPlanDetailsRepository { get; }

        ICultivationRegistrationRepository CultivationRegistrationRepository { get; }

        IFarmingCommitmentRepository FarmingCommitmentRepository { get; }

        ICropSeasonRepository CropSeasonRepository { get; }

        ICropStageRepository CropStageRepository { get; }

        ICropProgressRepository CropProgressRepository { get; }

        IProcessingMethodRepository ProcessingMethodRepository { get; }

        IWarehouseInboundRequestRepository WarehouseInboundRequests { get; }

        IBusinessStaffRepository BusinessStaffRepository { get; }

        IProductRepository ProductRepository { get; }

        ISystemConfigurationRepository SystemConfigurationRepository { get; }

        ISystemNotificationRepository SystemNotificationRepository { get; }

        ISystemNotificationRecipientRepository SystemNotificationRecipientRepository { get; }
        IWarehouseReceiptRepository WarehouseReceipts { get; }
        IInventoryRepository Inventories { get; }
        IWarehouseRepository Warehouses { get; }
        IWarehouseOutboundRequestRepository WarehouseOutboundRequests { get; }
        IWarehouseOutboundReceiptRepository WarehouseOutboundReceipts { get; }
        ICoffeeTypeRepository CoffeeTypeRepository { get; }
        IProcessingStageRepository ProcessingStageRepository { get; }

    }
}
