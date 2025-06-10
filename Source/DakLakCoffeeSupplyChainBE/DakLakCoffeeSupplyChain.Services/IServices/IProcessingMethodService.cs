using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DakLakCoffeeSupplyChain.Services.Base;

namespace DakLakCoffeeSupplyChain.Services.IServices
{
    public interface IProcessingMethodService
    {
        Task<IServiceResult> GetAll();
        Task<IServiceResult> GetById(int methodId);
        Task<IServiceResult> DeleteById(int methodId);
    


    }
}
