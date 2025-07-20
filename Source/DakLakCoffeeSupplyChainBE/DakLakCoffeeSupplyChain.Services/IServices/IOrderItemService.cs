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

        Task<IServiceResult> Update(OrderItemUpdateDto orderItemUpdateDto);

        Task<IServiceResult> DeleteOrderItemById(Guid orderItemId, Guid userId);

        Task<IServiceResult> SoftDeleteOrderItemById(Guid orderItemId, Guid userId);
    }
}
