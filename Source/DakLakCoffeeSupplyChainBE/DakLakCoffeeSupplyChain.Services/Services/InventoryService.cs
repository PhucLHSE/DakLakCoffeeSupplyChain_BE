using DakLakCoffeeSupplyChain.Common.DTOs.InventoryDTOs;
using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Repositories.UnitOfWork;
using DakLakCoffeeSupplyChain.Services.Base;
using DakLakCoffeeSupplyChain.Services.IServices;
using System;
using System.Linq;
using System.Threading.Tasks;
using DakLakCoffeeSupplyChain.Repositories.Models;
using DakLakCoffeeSupplyChain.Services.Generators;
using DakLakCoffeeSupplyChain.Services.Mappers; // ✅ Thêm dòng này
using System.Collections.Generic; // ✅ Thêm dòng này

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
                return new ServiceResult(Const.FAIL_READ_CODE, "Không xác định được quyền truy cập.");

            var inventories = await _unitOfWork.Inventories.GetAllWithIncludesAsync(i =>
                !i.IsDeleted && i.Warehouse.ManagerId == targetManagerId);

            if (inventories == null || !inventories.Any())
                return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không có dữ liệu tồn kho.", []);

            var result = inventories.Select(inv => inv.ToListItemDto()).ToList(); // ✅ Dùng mapper

            return new ServiceResult(Const.SUCCESS_READ_CODE, "Lấy danh sách tồn kho thành công.", result);
        }

        public async Task<IServiceResult> GetByIdAsync(Guid id)
        {
            var inv = await _unitOfWork.Inventories.GetDetailByIdAsync(id);
            if (inv == null)
                return new ServiceResult(Const.FAIL_READ_CODE, "Không tìm thấy thông tin tồn kho.");

            var dto = inv.ToDetailDto();
            return new ServiceResult(Const.SUCCESS_READ_CODE, "Lấy chi tiết tồn kho thành công.", dto);
        }

        public async Task<IServiceResult> CreateAsync(InventoryCreateDto dto, Guid userId)
        {
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

            var warehouse = await _unitOfWork.Warehouses.GetByIdAsync(dto.WarehouseId);
            if (warehouse == null || warehouse.ManagerId != targetManagerId)
                return new ServiceResult(Const.FAIL_READ_CODE, "Bạn không có quyền tạo tồn kho cho kho này.");

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
        public async Task<IServiceResult> GetAllByWarehouseIdAsync(Guid warehouseId, Guid userId)
        {
            Guid? targetManagerId = null;

            // Xác định ManagerId hoặc SupervisorId
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

            // Kiểm tra kho có thuộc quyền quản lý của người dùng không
            var warehouse = await _unitOfWork.Warehouses.GetByIdAsync(warehouseId);
            if (warehouse == null || warehouse.ManagerId != targetManagerId)
                return new ServiceResult(Const.FAIL_READ_CODE, "Kho không tồn tại hoặc không thuộc công ty bạn.");

            // Lấy tồn kho trong kho này - CHỈ lấy cà phê sơ chế (có BatchId), KHÔNG lấy cà phê tươi
            var inventories = await _unitOfWork.Inventories.GetAllWithIncludesAsync(i =>
                !i.IsDeleted && i.WarehouseId == warehouseId && i.BatchId != null);

            if (inventories == null || !inventories.Any())
                return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không có tồn kho trong kho này.", []);

            var result = inventories.Select(inv => inv.ToListItemDto()).ToList();
            return new ServiceResult(Const.SUCCESS_READ_CODE, "Lấy tồn kho theo kho thành công.", result);
        }

        // ✅ Thêm method mới để lấy TẤT CẢ tồn kho (cả sơ chế và tươi) cho warehouse detail
        public async Task<IServiceResult> GetAllByWarehouseIdForDetailAsync(Guid warehouseId, Guid userId)
        {
            Guid? targetManagerId = null;

            // Xác định ManagerId hoặc SupervisorId
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

            // Kiểm tra kho có thuộc quyền quản lý của người dùng không
            var warehouse = await _unitOfWork.Warehouses.GetByIdAsync(warehouseId);
            if (warehouse == null || warehouse.ManagerId != targetManagerId)
                return new ServiceResult(Const.FAIL_READ_CODE, "Kho không tồn tại hoặc không thuộc công ty bạn.");

            // Lấy TẤT CẢ tồn kho trong kho này (cả cà phê sơ chế và cà phê tươi) để hiển thị trong warehouse detail
            var inventories = await _unitOfWork.Inventories.GetAllWithIncludesAsync(i =>
                !i.IsDeleted && i.WarehouseId == warehouseId);

            if (inventories == null || !inventories.Any())
                return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không có tồn kho trong kho này.", []);

            var result = inventories.Select(inv => inv.ToListItemDto()).ToList();
            return new ServiceResult(Const.SUCCESS_READ_CODE, "Lấy tồn kho theo kho thành công.", result);
        }

        // ✅ Method mới để lấy inventory với thông tin FIFO chi tiết cho việc tạo yêu cầu xuất kho
        public async Task<IServiceResult> GetInventoriesWithFifoRecommendationAsync(Guid warehouseId, Guid userId, double? requestedQuantity = null)
        {
            Guid? targetManagerId = null;

            // Xác định ManagerId hoặc SupervisorId
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

            // Kiểm tra kho có thuộc quyền quản lý của người dùng không
            var warehouse = await _unitOfWork.Warehouses.GetByIdAsync(warehouseId);
            if (warehouse == null || warehouse.ManagerId != targetManagerId)
                return new ServiceResult(Const.FAIL_READ_CODE, "Kho không tồn tại hoặc không thuộc công ty bạn.");

            // Lấy tồn kho trong kho này - CHỈ lấy cà phê sơ chế (có BatchId), KHÔNG lấy cà phê tươi
            var inventories = await _unitOfWork.Inventories.GetAllWithIncludesAsync(i =>
                !i.IsDeleted && i.WarehouseId == warehouseId && i.BatchId != null);

            if (inventories == null || !inventories.Any())
                return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không có tồn kho trong kho này.", []);

            // Sắp xếp theo FIFO (First In, First Out) - nhập trước xuất trước
            var sortedInventories = inventories
                .OrderBy(i => i.CreatedAt)
                .ToList();

            // Tạo danh sách kết quả với thông tin FIFO chi tiết
            var result = new List<InventoryListItemDto>();
            double remainingQuantity = requestedQuantity ?? 0;
            
            for (int i = 0; i < sortedInventories.Count; i++)
            {
                var inv = sortedInventories[i];
                var fifoPriority = i + 1; // Thứ tự ưu tiên (1 = cao nhất)
                var daysInWarehouse = (DateTime.UtcNow - inv.CreatedAt).Days;
                
                // Xác định inventory được khuyến nghị
                bool isRecommended = false;
                string fifoRecommendation = "";
                
                if (requestedQuantity.HasValue && requestedQuantity > 0)
                {
                    // Nếu có yêu cầu số lượng cụ thể, tính toán khuyến nghị
                    if (remainingQuantity > 0 && inv.Quantity > 0)
                    {
                        isRecommended = true;
                        var recommendedQuantity = Math.Min(remainingQuantity, inv.Quantity);
                        remainingQuantity -= recommendedQuantity;
                        
                        if (daysInWarehouse > 30)
                        {
                            fifoRecommendation = $"Khuyến nghị xuất {recommendedQuantity:n0}kg: Đã nhập kho {daysInWarehouse} ngày trước (FIFO)";
                        }
                        else
                        {
                            fifoRecommendation = $"Khuyến nghị xuất {recommendedQuantity:n0}kg: Nhập kho sớm nhất (FIFO)";
                        }
                    }
                }
                else
                {
                    // Nếu không có yêu cầu số lượng cụ thể, khuyến nghị inventory đầu tiên
                    isRecommended = i == 0;
                    if (isRecommended)
                    {
                        if (daysInWarehouse > 30)
                        {
                            fifoRecommendation = $"Khuyến nghị xuất trước: Đã nhập kho {daysInWarehouse} ngày trước (FIFO)";
                        }
                        else
                        {
                            fifoRecommendation = "Khuyến nghị xuất trước: Nhập kho sớm nhất (FIFO)";
                        }
                    }
                }

                result.Add(inv.ToListItemDto(fifoPriority, isRecommended, fifoRecommendation));
            }

            return new ServiceResult(Const.SUCCESS_READ_CODE, "Lấy tồn kho với khuyến nghị FIFO thành công.", result);
        }

    }
}
