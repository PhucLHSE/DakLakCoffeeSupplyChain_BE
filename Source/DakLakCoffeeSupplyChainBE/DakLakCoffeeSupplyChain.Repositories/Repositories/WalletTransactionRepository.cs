using DakLakCoffeeSupplyChain.Repositories.Base;
using DakLakCoffeeSupplyChain.Repositories.DBContext;
using DakLakCoffeeSupplyChain.Repositories.IRepositories;
using DakLakCoffeeSupplyChain.Repositories.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Repositories.Repositories
{
    public class WalletTransactionRepository : GenericRepository<WalletTransaction>, IWalletTransactionRepository
    {
        public WalletTransactionRepository(DakLakCoffee_SCMContext context) => _context = context;

        public async Task<IEnumerable<WalletTransaction>> GetByWalletIdAsync(Guid walletId)
        {
            return await _context.WalletTransactions
                .Where(t => t.WalletId == walletId && !t.IsDeleted)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        public async Task<double> GetTotalAmountByWalletIdAsync(Guid walletId)
        {
            return await _context.WalletTransactions
                .Where(t => t.WalletId == walletId && !t.IsDeleted)
                .SumAsync(t => t.Amount);
        }

        public async Task<IEnumerable<WalletTransaction>> GetByWalletIdAndTypeAsync(Guid walletId, string transactionType)
        {
            return await _context.WalletTransactions
                .Where(t => t.WalletId == walletId && t.TransactionType == transactionType && !t.IsDeleted)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }
    }
}
