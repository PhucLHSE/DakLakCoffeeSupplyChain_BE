﻿using DakLakCoffeeSupplyChain.Repositories.Base;
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
    }
}
