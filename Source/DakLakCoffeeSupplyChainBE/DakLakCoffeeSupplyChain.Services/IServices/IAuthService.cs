using DakLakCoffeeSupplyChain.Common.DTOs.AuthDTOs;
using DakLakCoffeeSupplyChain.Services.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Services.IServices
{
    public interface IAuthService
    {
        Task<IServiceResult> LoginAsync(LoginRequestDto request);
    }
}
