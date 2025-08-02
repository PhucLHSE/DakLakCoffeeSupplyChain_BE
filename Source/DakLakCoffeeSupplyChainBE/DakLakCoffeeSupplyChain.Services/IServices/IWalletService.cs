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
    }
}
