using DakLakCoffeeSupplyChain.Common.DTOs.InventoryDTOs;
using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Repositories.UnitOfWork;
using DakLakCoffeeSupplyChain.Services.Base;
using DakLakCoffeeSupplyChain.Services.IServices;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Services.Services
{
    public class InventoryService : IInventoryService
    {
        private readonly IUnitOfWork _unitOfWork;

        public InventoryService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IServiceResult> GetAllAsync()
        {
            var inventories = await _unitOfWork.Inventories.GetAllWithIncludesAsync();
            if (inventories == null || !inventories.Any())
            {
                return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không có dữ liệu tồn kho.", []);
            }

            var result = inventories.Select(inv => new InventoryListItemDto
            {
                InventoryId = inv.InventoryId,
                InventoryCode = inv.InventoryCode,
                WarehouseName = inv.Warehouse?.Name ?? "N/A",
                BatchCode = inv.Batch?.BatchCode ?? "N/A",
                ProductName = inv.Batch?.Products?.FirstOrDefault()?.ProductName ?? "N/A",
                CoffeeTypeName = inv.Batch?.CoffeeType?.TypeName ?? "N/A",
                Quantity = inv.Quantity,
                Unit = inv.Unit
            }).ToList();

            return new ServiceResult(Const.SUCCESS_READ_CODE, "Lấy danh sách tồn kho thành công.", result);
        }

        public async Task<IServiceResult> GetByIdAsync(Guid id)
        {
            var inv = await _unitOfWork.Inventories.GetDetailByIdAsync(id);
            if (inv == null)
            {
                return new ServiceResult(Const.FAIL_READ_CODE, "Không tìm thấy thông tin tồn kho.");
            }

            var dto = new InventoryDetailDto
            {
                InventoryId = inv.InventoryId,
                InventoryCode = inv.InventoryCode,
                WarehouseId = inv.WarehouseId,
                WarehouseName = inv.Warehouse?.Name ?? "N/A",
                BatchId = inv.BatchId,
                BatchCode = inv.Batch?.BatchCode ?? "N/A",
                ProductName = inv.Batch?.Products?.FirstOrDefault()?.ProductName ?? "N/A",
                CoffeeTypeName = inv.Batch?.CoffeeType?.TypeName ?? "N/A",
                Quantity = inv.Quantity,
                Unit = inv.Unit,
                CreatedAt = inv.CreatedAt,
                UpdatedAt = inv.UpdatedAt
            };

            return new ServiceResult(Const.SUCCESS_READ_CODE, "Lấy chi tiết tồn kho thành công.", dto);
        }
    }
}
