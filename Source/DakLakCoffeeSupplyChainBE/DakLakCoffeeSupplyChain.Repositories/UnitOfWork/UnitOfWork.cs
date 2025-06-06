using DakLakCoffeeSupplyChain.Repositories.DBContext;
using DakLakCoffeeSupplyChain.Repositories.IRepositories;
using DakLakCoffeeSupplyChain.Repositories.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Repositories.UnitOfWork
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly DakLakCoffee_SCMContext context;

        private IRoleRepository? roleRepository;
        private IUserAccountRepository? userAccountRepository;
        private ISystemConfigurationRepository? systemConfigurationRepository;

        public UnitOfWork()
            => context ??= new DakLakCoffee_SCMContext();

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
    }
}
