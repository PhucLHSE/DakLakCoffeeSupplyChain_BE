using DakLakCoffeeSupplyChain.Common.DTOs.OrderDTOs;
using DakLakCoffeeSupplyChain.Services.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Services.IServices
{
    public interface IOrderService
    {
        Task<IServiceResult> GetAll(Guid userId);

        Task<IServiceResult> GetById(Guid orderId, Guid userId);

        Task<IServiceResult> Create(OrderCreateDto orderCreateDto);

        Task<IServiceResult> Update(OrderUpdateDto orderUpdateDto);

        Task<IServiceResult> DeleteOrderById(Guid orderId, Guid userId);

        Task<IServiceResult> SoftDeleteOrderById(Guid orderId, Guid userId);
    }
}
