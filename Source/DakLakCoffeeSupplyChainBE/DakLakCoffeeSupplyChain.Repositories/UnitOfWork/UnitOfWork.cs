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
        private ISystemConfigurationRepository? systemConfigurationRepository;
        private IWarehouseInboundRequestRepository? warehouseInboundRequestRepository; // ✅ THÊM DÒNG NÀY

        public UnitOfWork(DakLakCoffee_SCMContext context) // ✅ dùng DI chuẩn
        {
            this.context = context;
        }

        public IRoleRepository RoleRepository => roleRepository ??= new RoleRepository(context);

        public IUserAccountRepository UserAccountRepository => userAccountRepository ??= new UserAccountRepository(context);

        public ISystemConfigurationRepository SystemConfigurationRepository => systemConfigurationRepository ??= new SystemConfigurationRepository(context);

        public IWarehouseInboundRequestRepository WarehouseInboundRequests => warehouseInboundRequestRepository ??= new WarehouseInboundRequestRepository(context);

        public async Task<int> CompleteAsync()
        {
            return await context.SaveChangesAsync();
        }
        private IFarmerRepository? farmerRepository;

        public IFarmerRepository Farmers => farmerRepository ??= new FarmerRepository(context);
        private IBusinessStaffRepository? businessStaffRepository;
        public IBusinessStaffRepository BusinessStaffs => businessStaffRepository ??= new BusinessStaffRepository(context);
    }
}
