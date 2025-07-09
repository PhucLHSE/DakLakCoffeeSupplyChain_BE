using DakLakCoffeeSupplyChain.Common.DTOs.WarehouseOutboundReceiptDTOs;
using DakLakCoffeeSupplyChain.Services.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Services.IServices
{
    public interface IWarehouseOutboundReceiptService
    {
        Task<IServiceResult> CreateAsync(Guid staffUserId, WarehouseOutboundReceiptCreateDto dto);
        Task<IServiceResult> ConfirmReceiptAsync(Guid receiptId, WarehouseOutboundReceiptConfirmDto dto);
        Task<IServiceResult> GetAllAsync(Guid userId);
        Task<IServiceResult> GetByIdAsync(Guid receiptId, Guid userId);
    }
}
