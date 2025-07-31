using DakLakCoffeeSupplyChain.Services.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Services.IServices
{
    public interface IShipmentDetailService
    {
        Task<IServiceResult> SoftDeleteShipmentDetailById(Guid shipmentDetailId);
    }
}
