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
        Task<IServiceResult> Create(ShipmentDetailCreateDto shipmentDetailCreateDto, Guid userId);

        Task<IServiceResult> Update(ShipmentDetailUpdateDto shipmentDetailUpdateDto, Guid userId);

        Task<IServiceResult> DeleteShipmentDetailById(Guid shipmentDetailId, Guid userId);

        Task<IServiceResult> SoftDeleteShipmentDetailById(Guid shipmentDetailId, Guid userId);
    }
}
