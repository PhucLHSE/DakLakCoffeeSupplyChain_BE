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
        IFarmerRepository FarmerRepository { get; }
        ICultivationRegistrationRepository CultivationRegistrationRepository { get; }
        IFarmingCommitmentRepository FarmingCommitmentRepository { get; }
        ICropSeasonRepository CropSeasonRepository { get; }
        ICropStageRepository CropStageRepository { get; }
        IProcurementPlanRepository ProcurementPlanRepository { get; }
        IProcessingMethodRepository ProcessingMethodRepository { get; }
        IProcurementPlanDetailsRepository ProcurementPlanDetailsRepository { get; }
        ICropProgressRepository CropProgressRepository { get; }
        IWarehouseInboundRequestRepository WarehouseInboundRequests { get; }
        ISystemNotificationRepository SystemNotificationRepository { get; }
        ISystemNotificationRecipientRepository SystemNotificationRecipientRepository { get; }
        IBusinessStaffRepository BusinessStaffRepository { get; }

    }
}
