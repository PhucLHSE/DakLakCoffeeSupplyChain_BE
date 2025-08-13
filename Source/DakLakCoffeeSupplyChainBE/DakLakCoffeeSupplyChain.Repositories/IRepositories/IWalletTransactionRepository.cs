using DakLakCoffeeSupplyChain.Repositories.Base;
using DakLakCoffeeSupplyChain.Repositories.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Repositories.IRepositories
{
    public interface IWalletTransactionRepository : IGenericRepository<WalletTransaction>
    {
        Task<IEnumerable<WalletTransaction>> GetByWalletIdAsync(Guid walletId);
        Task<double> GetTotalAmountByWalletIdAsync(Guid walletId);
        Task<IEnumerable<WalletTransaction>> GetByWalletIdAndTypeAsync(Guid walletId, string transactionType);
    }
}
