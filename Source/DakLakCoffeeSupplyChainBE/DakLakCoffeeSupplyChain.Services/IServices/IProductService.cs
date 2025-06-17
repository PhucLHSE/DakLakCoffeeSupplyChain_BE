using DakLakCoffeeSupplyChain.Services.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Services.IServices
{
    public interface IProductService
    {
        Task<IServiceResult> GetAll();

        Task<IServiceResult> GetById(Guid productId);

        Task<IServiceResult> DeleteById(Guid productId);

        Task<IServiceResult> SoftDeleteById(Guid productId);
    }
}
