using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Repositories.UnitOfWork;
using DakLakCoffeeSupplyChain.Services.Base;
using DakLakCoffeeSupplyChain.Services.IServices;
using DakLakCoffeeSupplyChain.Services.Mappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Services.Services
{
    public class InventoryLogService : IInventoryLogService
    {
        private readonly IUnitOfWork _unitOfWork;

        public InventoryLogService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IServiceResult> GetByInventoryIdAsync(Guid inventoryId)
        {
            var logs = await _unitOfWork.InventoryLogs.GetByInventoryIdAsync(inventoryId);
            return new ServiceResult(Const.SUCCESS_READ_CODE, "Lấy log tồn kho thành công", logs);
        }
        public async Task<IServiceResult> GetAllAsync(Guid userId)
        {
            // Xác định công ty của user hiện tại
            var manager = await _unitOfWork.BusinessManagerRepository.FindByUserIdAsync(userId);
            Guid? targetManagerId = null;

            if (manager != null && !manager.IsDeleted)
                targetManagerId = manager.ManagerId;
            else
            {
                var staff = await _unitOfWork.BusinessStaffRepository.FindByUserIdAsync(userId);
                if (staff != null && !staff.IsDeleted)
                    targetManagerId = staff.SupervisorId;
            }

            if (targetManagerId == null)
                return new ServiceResult(Const.FAIL_READ_CODE, "Không xác định được quyền công ty");

            // Lấy tất cả inventory log liên quan đến các inventory thuộc kho do công ty này quản lý
            var logs = await _unitOfWork.InventoryLogs.GetAllAsync();

            var filteredLogs = logs
                .Where(log => log.Inventory?.Warehouse?.ManagerId == targetManagerId)
                .ToList();

            if (!filteredLogs.Any())
                return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không có log tồn kho nào thuộc công ty của bạn", null);

            var result = filteredLogs.Select(log =>
            {
                var updatedByName = log.UpdatedBy.HasValue
                    ? _unitOfWork.UserAccountRepository.GetById(log.UpdatedBy.Value)?.Name ?? "Không rõ"
                    : "Hệ thống";

                return log.ToListItemDto(updatedByName);
            });

            return new ServiceResult(Const.SUCCESS_READ_CODE, "Lấy danh sách log tồn kho thành công", result);
        }

        public async Task<IServiceResult> GetLogsByInventoryIdAsync(Guid inventoryId, Guid userId)
        {
            // Lấy inventory cần truy xuất
            var inventory = await _unitOfWork.Inventories.GetByIdWithWarehouseAsync(inventoryId);
            if (inventory == null || inventory.IsDeleted)
                return new ServiceResult(Const.FAIL_READ_CODE, "Không tìm thấy tồn kho");

            // Xác định công ty của người dùng
            var manager = await _unitOfWork.BusinessManagerRepository.FindByUserIdAsync(userId);
            Guid? ownerManagerId = null;

            if (manager != null && !manager.IsDeleted)
                ownerManagerId = manager.ManagerId;
            else
            {
                var staff = await _unitOfWork.BusinessStaffRepository.FindByUserIdAsync(userId);
                if (staff != null && !staff.IsDeleted)
                    ownerManagerId = staff.SupervisorId;
            }

            if (ownerManagerId == null)
                return new ServiceResult(Const.FAIL_READ_CODE, "Không xác định được công ty của người dùng");

            // Kiểm tra quyền sở hữu tồn kho
            if (inventory.Warehouse?.ManagerId != ownerManagerId)
                return new ServiceResult(Const.FAIL_READ_CODE, "Bạn không có quyền truy cập tồn kho này");

            // Truy xuất logs
            var logs = await _unitOfWork.InventoryLogs.GetByInventoryIdAsync(inventoryId);
            if (logs == null || !logs.Any())
                return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không có log tồn kho nào", null);

            var result = logs.Select(log =>
            {
                var updatedByName = log.UpdatedBy.HasValue
                    ? _unitOfWork.UserAccountRepository.GetById(log.UpdatedBy.Value)?.Name ?? "Không rõ"
                    : "Hệ thống";

                return log.ToByInventoryDto(updatedByName);
            });

            return new ServiceResult(Const.SUCCESS_READ_CODE, "Lấy log tồn kho thành công", result);
        }
        public async Task<IServiceResult> SoftDeleteAsync(Guid logId)
        {
            var log = await _unitOfWork.InventoryLogs.GetByIdAsync(logId);
            if (log == null || log.IsDeleted)
                return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không tìm thấy log hoặc đã bị xoá");

            log.IsDeleted = true;
            

            await _unitOfWork.InventoryLogs.UpdateAsync(log);
            var result = await _unitOfWork.SaveChangesAsync();

            return result > 0
                ? new ServiceResult(Const.SUCCESS_DELETE_CODE, "Xoá mềm thành công")
                : new ServiceResult(Const.FAIL_DELETE_CODE, "Xoá mềm thất bại");
        }

        public async Task<IServiceResult> HardDeleteAsync(Guid logId)
        {
            var log = await _unitOfWork.InventoryLogs.GetByIdAsync(logId);
            if (log == null)
                return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không tìm thấy log");

            await _unitOfWork.InventoryLogs.RemoveAsync(log);
            var result = await _unitOfWork.SaveChangesAsync();

            return result > 0
                ? new ServiceResult(Const.SUCCESS_DELETE_CODE, "Xoá vĩnh viễn thành công")
                : new ServiceResult(Const.FAIL_DELETE_CODE, "Xoá vĩnh viễn thất bại");
        }

    }
}
