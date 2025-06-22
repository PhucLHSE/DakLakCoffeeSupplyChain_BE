using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Services.Base;
using System;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Services.IServices
{
    public interface IGeneralFarmerReportService
    {
        Task<IServiceResult> GetAll();
        Task<IServiceResult> GetById(Guid reportId);
    }
}
