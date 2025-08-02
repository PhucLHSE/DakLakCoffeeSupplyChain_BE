using DakLakCoffeeSupplyChain.Common.DTOs.ShipmentDTOs.ShipmentDetailDTOs;
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
        Task<IServiceResult> Create(ShipmentDetailCreateDto shipmentDetailCreateDto);

        Task<IServiceResult> Update(ShipmentDetailUpdateDto shipmentDetailUpdateDto);

        Task<IServiceResult> DeleteShipmentDetailById(Guid shipmentDetailId);

        Task<IServiceResult> SoftDeleteShipmentDetailById(Guid shipmentDetailId, Guid userId);
    }
}
