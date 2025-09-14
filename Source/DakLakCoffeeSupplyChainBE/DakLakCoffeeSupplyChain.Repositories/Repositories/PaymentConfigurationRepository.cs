using DakLakCoffeeSupplyChain.Repositories.Base;
using DakLakCoffeeSupplyChain.Repositories.DBContext;
using DakLakCoffeeSupplyChain.Repositories.IRepositories;
using DakLakCoffeeSupplyChain.Repositories.Models;

namespace DakLakCoffeeSupplyChain.Repositories.Repositories
{
    public class PaymentConfigurationRepository : GenericRepository<PaymentConfiguration>, IPaymentConfigurationRepository
    {
        public PaymentConfigurationRepository(DakLakCoffee_SCMContext context) : base(context)
        {
        }
    }
}





