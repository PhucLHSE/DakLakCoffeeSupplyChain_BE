using DakLakCoffeeSupplyChain.Repositories.IRepositories;
using DakLakCoffeeSupplyChain.Repositories.IRepositories.DakLakCoffeeSupplyChain.Repositories.IRepositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Repositories.UnitOfWork
{
    public interface IUnitOfWork : IDisposable
    {
        Task<int> SaveChangesAsync();

        IRoleRepository RoleRepository { get; }

        IUserAccountRepository UserAccountRepository { get; }

        IBusinessManagerRepository BusinessManagerRepository { get; }

        IFarmerRepository FarmerRepository { get; }

        IBusinessBuyerRepository BusinessBuyerRepository { get; }

        IContractRepository ContractRepository { get; }

        IContractItemRepository ContractItemRepository { get; }

        IContractDeliveryBatchRepository ContractDeliveryBatchRepository { get; }

        IContractDeliveryItemRepository ContractDeliveryItemRepository { get; }

        IProcurementPlanRepository ProcurementPlanRepository { get; }

        IProcurementPlanDetailsRepository ProcurementPlanDetailsRepository { get; }

        ICultivationRegistrationRepository CultivationRegistrationRepository { get; }

        ICultivationRegistrationsDetailRepository CultivationRegistrationsDetailRepository { get; }

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
      
        IGeneralFarmerReportRepository GeneralFarmerReportRepository { get; }
      
        IProcessingBatchProgressRepository ProcessingBatchProgressRepository { get; }
      
        IProcessingParameterRepository ProcessingParameterRepository { get; }

        IProcessingBatchRepository ProcessingBatchRepository { get; }

        ICropSeasonDetailRepository CropSeasonDetailRepository { get; }

        IInventoryLogRepository InventoryLogs { get; }

        IExpertAdviceRepository ExpertAdviceRepository { get; }

        IAgriculturalExpertRepository AgriculturalExpertRepository { get; }

        IOrderRepository OrderRepository { get; }

        IOrderItemRepository OrderItemRepository { get; }

        IProcessingBatchWasteRepository ProcessingWasteRepository { get; }

        IProcessingWasteDisposalRepository ProcessingWasteDisposalRepository { get; }

        IFarmingCommitmentsDetailRepository FarmingCommitmentsDetailRepository { get; }

        IShipmentRepository ShipmentRepository { get; }

        IShipmentDetailRepository ShipmentDetailRepository { get; }

        IWalletRepository WalletRepository { get; }


        IWalletTransactionRepository WalletTransactionRepository { get; }



        IMediaFileRepository MediaFileRepository { get; }

        IProcessingBatchEvaluationRepository ProcessingBatchEvaluationRepository { get; }

        IPaymentRepository PaymentRepository { get; }

        IPaymentConfigurationRepository PaymentConfigurationRepository { get; }

        ICropRepository CropRepository { get; }
    }
}
