using DakLakCoffeeSupplyChain.Common.DTOs.WarehouseDTOs;
using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Repositories.Models;
using DakLakCoffeeSupplyChain.Repositories.UnitOfWork;
using DakLakCoffeeSupplyChain.Services.Base;
using DakLakCoffeeSupplyChain.Services.IServices;
using DakLakCoffeeSupplyChain.Services.Mappers;
using DakLakCoffeeSupplyChain.Services.Generators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Services.Services
{
    public class WarehouseService : IWarehouseService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICodeGenerator _codeGenerator;

        public WarehouseService(IUnitOfWork unitOfWork, ICodeGenerator codeGenerator)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _codeGenerator = codeGenerator ?? throw new ArgumentNullException(nameof(codeGenerator));
        }

        public async Task<IServiceResult> CreateAsync(WarehouseCreateDto dto, Guid userId)
        {
            if (await _unitOfWork.Warehouses.IsNameExistsAsync(dto.Name))
            {
                return new ServiceResult(Const.FAIL_CREATE_CODE, "Tên kho đã tồn tại.");
            }

            var manager = await _unitOfWork.BusinessManagerRepository.FindByUserIdAsync(userId);
            if (manager == null || manager.IsDeleted)
            {
                return new ServiceResult(Const.FAIL_CREATE_CODE, "Không tìm thấy BusinessManager hợp lệ.");
            }

            var warehouseCode = await _codeGenerator.GenerateWarehouseCodeAsync();

            var warehouse = dto.ToEntityFromCreateDto(manager.ManagerId, warehouseCode);

            await _unitOfWork.Warehouses.CreateAsync(warehouse);
            await _unitOfWork.SaveChangesAsync();

            return new ServiceResult(Const.SUCCESS_CREATE_CODE, "Tạo kho thành công", warehouse.WarehouseId);
        }


        public async Task<IServiceResult> GetAllAsync(Guid userId)
        {
            // Check xem là BusinessManager hay BusinessStaff
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
            {
                return new ServiceResult(Const.FAIL_READ_CODE, "Không xác định được quyền truy cập kho.");
            }

            var warehouses = await _unitOfWork.Warehouses.FindAsync(w => !w.IsDeleted && w.ManagerId == targetManagerId);

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

            // Loại trừ chính bản ghi hiện tại khỏi kiểm tra trùng tên
            if (await _unitOfWork.Warehouses.IsNameExistsAsync(dto.Name, warehouse.WarehouseId))
                return new ServiceResult(Const.FAIL_UPDATE_CODE, "Tên kho đã tồn tại.");

            dto.MapToEntity(warehouse);

            _unitOfWork.Warehouses.Update(warehouse);
            await _unitOfWork.SaveChangesAsync();

            return new ServiceResult(Const.SUCCESS_UPDATE_CODE, "Cập nhật kho thành công.");
        }
        public async Task<IServiceResult> DeleteAsync(Guid warehouseId)
        {
            var warehouse = await _unitOfWork.Warehouses.GetDeletableByIdAsync(warehouseId);
            if (warehouse == null)
                return new ServiceResult(Const.FAIL_READ_CODE, "Không tìm thấy kho.");

            var inventories = await _unitOfWork.Inventories.GetAllAsync(i => i.WarehouseId == warehouseId && !i.IsDeleted);
            if (inventories.Any())
            {
                var inventoryCodes = inventories.Select(i => i.InventoryCode).ToList();
                var message = $"Không thể xoá mềm kho vì đang có tồn kho liên kết: {string.Join(", ", inventoryCodes)}";

                return new ServiceResult(Const.FAIL_DELETE_CODE, message);
            }

            warehouse.IsDeleted = true;
            warehouse.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Warehouses.Update(warehouse);
            await _unitOfWork.SaveChangesAsync();

            return new ServiceResult(Const.SUCCESS_DELETE_CODE, "Xoá mềm kho thành công.");
        }

        public async Task<IServiceResult> GetByIdAsync(Guid id)
        {
            var warehouse = await _unitOfWork.Warehouses.GetByIdWithManagerAsync(id);
            if (warehouse == null)
                return new ServiceResult(Const.FAIL_READ_CODE, "Không tìm thấy kho.");

            var result = warehouse.ToDetailDto();
            return new ServiceResult(Const.SUCCESS_READ_CODE, Const.SUCCESS_READ_MSG, result);
        }

        public async Task<IServiceResult> HardDeleteAsync(Guid warehouseId)
        {
            var warehouse = await _unitOfWork.Warehouses.GetByIdAsync(warehouseId);
            if (warehouse == null)
                return new ServiceResult(Const.FAIL_READ_CODE, "Không tìm thấy kho.");

            var inventories = await _unitOfWork.Inventories.GetAllAsync(i => i.WarehouseId == warehouseId);
            if (inventories.Any())
            {
                var inventoryCodes = inventories.Select(i => i.InventoryCode).ToList();
                var message = $"Không thể xoá vĩnh viễn kho vì đang có tồn kho liên kết: {string.Join(", ", inventoryCodes)}";

                return new ServiceResult(Const.FAIL_DELETE_CODE, message);
            }

            await _unitOfWork.Warehouses.RemoveAsync(warehouse);
            await _unitOfWork.SaveChangesAsync();

            return new ServiceResult(Const.SUCCESS_DELETE_CODE, "Xóa kho vĩnh viễn thành công.");
        }
    }
}
