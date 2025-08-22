using DakLakCoffeeSupplyChain.Common.DTOs.WarehouseReceiptDTOs;
using DakLakCoffeeSupplyChain.Services.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Services.IServices
{
    public interface IWarehouseReceiptService
    {
        Task<IServiceResult> CreateReceiptAsync(Guid staffUserId, WarehouseReceiptCreateDto dto);
        Task<IServiceResult> ConfirmReceiptAsync(Guid receiptId, WarehouseReceiptConfirmDto dto);

        Task<IServiceResult> GetAllAsync(Guid userId);
        Task<IServiceResult> GetByIdAsync(Guid receiptId);
        Task<IServiceResult> SoftDeleteAsync(Guid receiptId, Guid userId);
        Task<IServiceResult> HardDeleteAsync(Guid receiptId);

       
        Task<IServiceResult> GetInboundRequestSummaryAsync(Guid inboundRequestId);

       
        Task<IServiceResult> CancelReceiptAsync(Guid receiptId, Guid userId);
    }
}
