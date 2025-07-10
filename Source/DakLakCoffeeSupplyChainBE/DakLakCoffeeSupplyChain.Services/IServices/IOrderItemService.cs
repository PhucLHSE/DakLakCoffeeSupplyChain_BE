using DakLakCoffeeSupplyChain.Common.DTOs.OrderDTOs.OrderItemDTOs;
using DakLakCoffeeSupplyChain.Services.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Services.IServices
{
    public interface IOrderItemService
    {
        Task<IServiceResult> Create(OrderItemCreateDto orderItemCreateDto);

        Task<IServiceResult> SoftDeleteOrderItemById(Guid orderItemId);
    }
}
