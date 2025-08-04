using DakLakCoffeeSupplyChain.Repositories.Base;
using DakLakCoffeeSupplyChain.Repositories.DBContext;
using DakLakCoffeeSupplyChain.Repositories.IRepositories;
using DakLakCoffeeSupplyChain.Repositories.Models;

namespace DakLakCoffeeSupplyChain.Repositories.Repositories
{
    public class ExpertAdviceRepository : GenericRepository<ExpertAdvice>, IExpertAdviceRepository
    {
        private readonly DakLakCoffee_SCMContext _context;

        public ExpertAdviceRepository(DakLakCoffee_SCMContext context)
            : base(context)
        {
            _context = context;
        }

        public async Task AddAsync(ExpertAdvice entity)
        {
            await _context.ExpertAdvices.AddAsync(entity);
        }
        public void Update(ExpertAdvice entity)
        {
            _context.ExpertAdvices.Update(entity);
        }
        public void Delete(ExpertAdvice entity)
        {
            _context.ExpertAdvices.Remove(entity);
        }

    }
}
