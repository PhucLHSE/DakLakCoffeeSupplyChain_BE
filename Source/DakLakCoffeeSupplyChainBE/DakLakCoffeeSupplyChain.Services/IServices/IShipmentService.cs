using DakLakCoffeeSupplyChain.Common.DTOs.ShipmentDTOs;
using DakLakCoffeeSupplyChain.Services.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Services.IServices
{
    public interface IShipmentService
    {
        Task<IServiceResult> GetAll(Guid userId);

        Task<IServiceResult> GetById(Guid shipmentId, Guid userId);

        Task<IServiceResult> Create(ShipmentCreateDto shipmentCreateDto);

        Task<IServiceResult> Update(ShipmentUpdateDto shipmentUpdateDto, Guid userId);

        Task<IServiceResult> DeleteShipmentById(Guid shipmentId, Guid userId);

        Task<IServiceResult> SoftDeleteShipmentById(Guid shipmentId, Guid userId);
    }
}
