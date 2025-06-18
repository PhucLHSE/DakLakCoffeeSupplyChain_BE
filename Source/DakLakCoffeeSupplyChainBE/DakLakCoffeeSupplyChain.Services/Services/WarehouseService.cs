using DakLakCoffeeSupplyChain.Common.DTOs.WarehouseDTOs;
using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Repositories.Models;
using DakLakCoffeeSupplyChain.Repositories.UnitOfWork;
using DakLakCoffeeSupplyChain.Services.Base;
using DakLakCoffeeSupplyChain.Services.IServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DakLakCoffeeSupplyChain.Services.Mappers;

namespace DakLakCoffeeSupplyChain.Services.Services
{
    public class WarehouseService : IWarehouseService
    {
        private readonly IUnitOfWork _unitOfWork;

        public WarehouseService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IServiceResult> CreateAsync(WarehouseCreateDto dto)
        {
            if (await _unitOfWork.Warehouses.IsNameExistsAsync(dto.Name))
            {
                return new ServiceResult(Const.FAIL_CREATE_CODE, "Tên kho đã tồn tại.");
            }

            var warehouse = new Warehouse
            {
                WarehouseId = Guid.NewGuid(),
                WarehouseCode = "WH-" + Guid.NewGuid().ToString("N")[..8],
                Name = dto.Name,
                Location = dto.Location,
                ManagerId = dto.ManagerId,
                Capacity = dto.Capacity,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Warehouses.CreateAsync(warehouse);
            await _unitOfWork.SaveChangesAsync();

            return new ServiceResult(Const.SUCCESS_CREATE_CODE, "Tạo kho thành công", warehouse.WarehouseId);
        }
        public async Task<IServiceResult> GetAllAsync()
        {
            var warehouses = await _unitOfWork.Warehouses
                .FindAsync(w => !w.IsDeleted);

            var result = warehouses.Select(w => new WarehouseViewDto
            {
                WarehouseId = w.WarehouseId,
                Name = w.Name,
                Location = w.Location,
                Capacity = w.Capacity
            }).ToList();

            return new ServiceResult(Const.SUCCESS_READ_CODE, Const.SUCCESS_READ_MSG, result);
        }
        public async Task<IServiceResult> UpdateAsync(Guid id, WarehouseUpdateDto dto)
        {
            var warehouse = await _unitOfWork.Warehouses.GetByIdAsync(id);
            if (warehouse == null)
                return new ServiceResult(Const.FAIL_READ_CODE, "Không tìm thấy kho.");

            if (await _unitOfWork.Warehouses.IsNameExistsAsync(dto.Name))
                return new ServiceResult(Const.FAIL_UPDATE_CODE, "Tên kho đã tồn tại.");

            // Áp dụng mapper
            dto.MapToEntity(warehouse);

            _unitOfWork.Warehouses.Update(warehouse);
            await _unitOfWork.SaveChangesAsync();

            return new ServiceResult(Const.SUCCESS_UPDATE_CODE, "Cập nhật kho thành công.");
        }

    }
}
