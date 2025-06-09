using DakLakCoffeeSupplyChain.Repositories.DBContext;
using DakLakCoffeeSupplyChain.Repositories.IRepositories;
using DakLakCoffeeSupplyChain.Repositories.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DakLakCoffeeSupplyChain.Repositories.UnitOfWork
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly DakLakCoffee_SCMContext context;

        private IRoleRepository? roleRepository;
        private IUserAccountRepository? userAccountRepository;
        private IProductRepository? productRepository;
        private ISystemConfigurationRepository? systemConfigurationRepository;
        private IWarehouseInboundRequestRepository? warehouseInboundRequestRepository;
        private IFarmerRepository? farmerRepository;
        private IBusinessStaffRepository? businessStaffRepository;
        private IWarehouseReceiptRepository? warehouseReceiptRepository; // ✅ Thêm biến tạm

        public UnitOfWork()
            => context ??= new DakLakCoffee_SCMContext();

        public async Task<int> SaveChangesAsync()
        {
            return await context.SaveChangesAsync();
        }

        public IRoleRepository RoleRepository
        {
            this.context = context;
        }

        public IRoleRepository RoleRepository => roleRepository ??= new RoleRepository(context);
        public IUserAccountRepository UserAccountRepository => userAccountRepository ??= new UserAccountRepository(context);
        public ISystemConfigurationRepository SystemConfigurationRepository => systemConfigurationRepository ??= new SystemConfigurationRepository(context);
        public IWarehouseInboundRequestRepository WarehouseInboundRequests => warehouseInboundRequestRepository ??= new WarehouseInboundRequestRepository(context);
        public IFarmerRepository Farmers => farmerRepository ??= new FarmerRepository(context);
        public IBusinessStaffRepository BusinessStaffs => businessStaffRepository ??= new BusinessStaffRepository(context);

        public IWarehouseReceiptRepository WarehouseReceiptRepository => warehouseReceiptRepository ??= new WarehouseReceiptRepository(context); // ✅ Gán đúng cách
        private IInventoryRepository? inventoryRepository;
        public IInventoryRepository InventoryRepository => inventoryRepository ??= new InventoryRepository(context);

        public async Task<int> CompleteAsync()
        {
            return await context.SaveChangesAsync();
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
    }
}
