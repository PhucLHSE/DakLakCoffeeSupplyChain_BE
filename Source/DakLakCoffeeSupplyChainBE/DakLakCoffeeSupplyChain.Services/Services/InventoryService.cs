﻿using DakLakCoffeeSupplyChain.Common.DTOs.InventoryDTOs;
using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Repositories.UnitOfWork;
using DakLakCoffeeSupplyChain.Services.Base;
using DakLakCoffeeSupplyChain.Services.IServices;
using System;
using System.Linq;
using System.Threading.Tasks;
using DakLakCoffeeSupplyChain.Repositories.Models;
using DakLakCoffeeSupplyChain.Services.Generators;

namespace DakLakCoffeeSupplyChain.Services.Services
{
    public class InventoryService : IInventoryService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICodeGenerator _codeGenerator;

        public InventoryService(IUnitOfWork unitOfWork, ICodeGenerator codeGenerator)
        {
            _unitOfWork = unitOfWork;
            _codeGenerator = codeGenerator;
        }

        public async Task<IServiceResult> GetAllAsync(Guid userId)
        {
            // Lấy ManagerId từ UserId
            var manager = await _unitOfWork.BusinessManagerRepository.FindByUserIdAsync(userId);
            Guid? targetManagerId = null;

            if (manager != null && !manager.IsDeleted)
            {
                targetManagerId = manager.ManagerId;
            }
            else
            {
                var staff = await _unitOfWork.BusinessStaffRepository.FindByUserIdAsync(userId);
                if (staff != null && !staff.IsDeleted)
                {
                    targetManagerId = staff.SupervisorId;
                }
            }

            if (targetManagerId == null)
                return new ServiceResult(Const.FAIL_READ_CODE, "Không xác định được quyền truy cập.");

            var inventories = await _unitOfWork.Inventories.GetAllWithIncludesAsync(i =>
                !i.IsDeleted && i.Warehouse.ManagerId == targetManagerId);

            if (inventories == null || !inventories.Any())
                return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không có dữ liệu tồn kho.", []);

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

        public async Task<IServiceResult> CreateAsync(InventoryCreateDto dto, Guid userId)
        {
            // ✅ Xác định ManagerId của người dùng (manager hoặc staff)
            Guid? targetManagerId = null;

            var manager = await _unitOfWork.BusinessManagerRepository.FindByUserIdAsync(userId);
            if (manager != null && !manager.IsDeleted)
                targetManagerId = manager.ManagerId;
            else
            {
                var staff = await _unitOfWork.BusinessStaffRepository.FindByUserIdAsync(userId);
                if (staff != null && !staff.IsDeleted)
                    targetManagerId = staff.SupervisorId;
            }

            if (targetManagerId == null)
                return new ServiceResult(Const.FAIL_READ_CODE, "Không xác định được người dùng thuộc công ty nào.");

            // ✅ Kiểm tra kho có tồn tại và thuộc quyền quản lý
            var warehouse = await _unitOfWork.Warehouses.GetByIdAsync(dto.WarehouseId);
            if (warehouse == null || warehouse.ManagerId != targetManagerId)
                return new ServiceResult(Const.FAIL_READ_CODE, "Bạn không có quyền tạo tồn kho cho kho này.");

            // (Tạm bỏ kiểm tra batch vì chưa gắn batch theo công ty)

            var existing = await _unitOfWork.Inventories.FindByWarehouseAndBatchAsync(dto.WarehouseId, dto.BatchId);
            if (existing != null)
                return new ServiceResult(Const.FAIL_CREATE_CODE, "Tồn kho đã tồn tại cho kho và batch này.");

            var currentInventories = await _unitOfWork.Inventories
                .GetAllAsync(i => i.WarehouseId == dto.WarehouseId && !i.IsDeleted);
            double totalCurrentQuantity = currentInventories.Sum(i => i.Quantity);
            double available = (warehouse.Capacity ?? 0) - totalCurrentQuantity;

            if (dto.Quantity > available)
            {
                return new ServiceResult(Const.ERROR_VALIDATION_CODE,
                    $"Kho \"{warehouse.Name}\" chỉ còn trống {available:n0}kg, không thể thêm {dto.Quantity}kg.");
            }

            var inventoryCode = await _codeGenerator.GenerateInventoryCodeAsync();

            var newInventory = new Inventory
            {
                InventoryId = Guid.NewGuid(),
                InventoryCode = inventoryCode,
                WarehouseId = dto.WarehouseId,
                BatchId = dto.BatchId,
                Quantity = dto.Quantity,
                Unit = "kg",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            await _unitOfWork.Inventories.CreateAsync(newInventory);
            await _unitOfWork.SaveChangesAsync();

            return new ServiceResult(Const.SUCCESS_CREATE_CODE, "Tạo tồn kho thành công.", newInventory.InventoryId);
        }





        public async Task<IServiceResult> SoftDeleteAsync(Guid id)
        {
            var entity = await _unitOfWork.Inventories.FindByIdAsync(id);
            if (entity == null)
                return new ServiceResult(Const.FAIL_DELETE_CODE, "Không tìm thấy tồn kho để xoá.");

            entity.IsDeleted = true;
            entity.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Inventories.Update(entity);
            await _unitOfWork.SaveChangesAsync();

            return new ServiceResult(Const.SUCCESS_DELETE_CODE, "Xoá mềm tồn kho thành công.");
        }

        public async Task<IServiceResult> HardDeleteAsync(Guid id)
        {
            var entity = await _unitOfWork.Inventories.FindByIdAsync(id);
            if (entity == null)
                return new ServiceResult(Const.FAIL_DELETE_CODE, "Không tìm thấy tồn kho để xoá.");

            await _unitOfWork.Inventories.RemoveAsync(entity);
            await _unitOfWork.SaveChangesAsync();

            return new ServiceResult(Const.SUCCESS_DELETE_CODE, "Xoá thật tồn kho thành công.");
        }
    }
}
