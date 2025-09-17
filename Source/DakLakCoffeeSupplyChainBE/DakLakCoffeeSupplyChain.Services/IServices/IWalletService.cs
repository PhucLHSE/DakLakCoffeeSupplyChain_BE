using DakLakCoffeeSupplyChain.Common.DTOs.WalletDTOs;
using DakLakCoffeeSupplyChain.Services.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Services.IServices
{
    public interface IWalletService
    {
        Task<IServiceResult> Create(WalletCreateDto walletCreateDto, Guid userId);
        Task<IServiceResult> GetAllAsync(Guid userId);
        Task<IServiceResult> GetByIdAsync(Guid walletId, Guid userId);
        Task<IServiceResult> UpdateAsync(Guid walletId, WalletUpdateDto dto, Guid userId);
        Task<IServiceResult> DeleteAsync(Guid walletId, Guid userId);
        Task<IServiceResult> GetMyWalletAsync(Guid userId);
        Task<IServiceResult> GetWalletBalanceAsync(Guid walletId, Guid userId);
        Task<IServiceResult> CreateTopupPaymentAsync(WalletTopupRequestDto request, Guid userId);
        Task<IServiceResult> ProcessTopupPaymentAsync(string transactionId, double amount, Guid userId);
        Task<IServiceResult> DirectTopupAsync(Guid userId, double amount, string? description);
    }
}
