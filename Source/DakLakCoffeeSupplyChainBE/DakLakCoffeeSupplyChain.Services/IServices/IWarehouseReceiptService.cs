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
        Task<IServiceResult> ConfirmReceiptAsync(Guid receiptId);
    }
}
