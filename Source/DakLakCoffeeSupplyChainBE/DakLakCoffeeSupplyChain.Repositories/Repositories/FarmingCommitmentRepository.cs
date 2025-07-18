using DakLakCoffeeSupplyChain.Repositories.Base;
using DakLakCoffeeSupplyChain.Repositories.DBContext;
using DakLakCoffeeSupplyChain.Repositories.IRepositories;
using DakLakCoffeeSupplyChain.Repositories.Models;
using Microsoft.EntityFrameworkCore;

namespace DakLakCoffeeSupplyChain.Repositories.Repositories
{
    public class FarmingCommitmentRepository : GenericRepository<FarmingCommitment>, IFarmingCommitmentRepository
    {
        public FarmingCommitmentRepository(DakLakCoffee_SCMContext context) : base(context) { }

        public async Task<FarmingCommitment?> GetByIdAsync(Guid id)
        {
            return await _context.FarmingCommitments
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.CommitmentId == id);
        }

        public async Task<FarmingCommitment?> GetWithRegistrationAsync(Guid commitmentId)
        {
            return await _context.FarmingCommitments
                .Include(c => c.RegistrationDetail)
                    .ThenInclude(rd => rd.Registration)
                .FirstOrDefaultAsync(c => c.CommitmentId == commitmentId && !c.IsDeleted);
        }
        public async Task<FarmingCommitment?> GetByRegistrationDetailIdAsync(Guid registrationDetailId)
        {
            return await _context.FarmingCommitments
                .Include(fc => fc.RegistrationDetail)
                    .ThenInclude(rd => rd.Registration)
                .Where(fc => fc.RegistrationDetailId == registrationDetailId && !fc.IsDeleted)
                .OrderByDescending(fc => fc.CreatedAt)
                .FirstOrDefaultAsync();
        }
        public async Task<int> CountFarmingCommitmentsInYearAsync(int year)
        {
            return await _context.FarmingCommitments
                .CountAsync(p => p.CreatedAt.Year == year);
        }

    }
}
