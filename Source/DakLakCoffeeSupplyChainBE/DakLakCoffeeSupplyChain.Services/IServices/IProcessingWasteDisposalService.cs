using DakLakCoffeeSupplyChain.Services.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Services.IServices
{
    public interface IProcessingWasteDisposalService
    {
        Task<IServiceResult> GetAllAsync(Guid userId, bool isAdmin);
    }
}
