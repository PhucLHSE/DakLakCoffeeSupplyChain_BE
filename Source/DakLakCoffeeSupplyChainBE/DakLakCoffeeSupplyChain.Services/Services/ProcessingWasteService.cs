﻿using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.ProcessingWastesDTOs;
using DakLakCoffeeSupplyChain.Common.Helpers;
using DakLakCoffeeSupplyChain.Repositories.Models;
using DakLakCoffeeSupplyChain.Repositories.UnitOfWork;
using DakLakCoffeeSupplyChain.Services.Base;
using DakLakCoffeeSupplyChain.Services.Generators;
using DakLakCoffeeSupplyChain.Services.IServices;
using DakLakCoffeeSupplyChain.Services.Mappers;

using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Services.Services
{
    public class ProcessingWasteService : IProcessingWasteService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICodeGenerator _codeGenerator;

        public ProcessingWasteService(IUnitOfWork unitOfWork, ICodeGenerator CodeGenerator )
        {
            _unitOfWork = unitOfWork;
            _codeGenerator = CodeGenerator;
        }

        public async Task<IServiceResult> GetAllByUserIdAsync(Guid userId, bool isAdmin)
        {
            // Lấy toàn bộ danh sách người dùng để ánh xạ tên
            var users = await _unitOfWork.UserAccountRepository.GetAllAsync(
                predicate: u => !u.IsDeleted,
                asNoTracking: true
            );

            var userMap = users.ToDictionary(u => u.UserId, u => u.Name);

            // Bắt đầu truy vấn Waste
            var query = _unitOfWork.ProcessingWasteRepository.GetAllQueryable()
                .Where(w => !w.IsDeleted);

            if (!isAdmin)
            {
                var farmer = await _unitOfWork.FarmerRepository.FindByUserIdAsync(userId);
                if (farmer == null)
                {
                    return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không tìm thấy Farmer tương ứng.");
                }

                // Lọc các ProgressId thuộc về farmer
                var progressIds = await _unitOfWork.ProcessingBatchProgressRepository.GetAllQueryable()
                    .Where(p => p.Batch.FarmerId == farmer.FarmerId)
                    .Select(p => p.ProgressId)
                    .ToListAsync();

                query = query.Where(w => progressIds.Contains(w.ProgressId));
            }

            // Thực thi truy vấn
            var wastes = await query.ToListAsync();

            // Map sang DTO
            var dtos = wastes.Select(waste =>
            {
                var recordedByName = waste.RecordedBy.HasValue && userMap.ContainsKey(waste.RecordedBy.Value)
                    ? userMap[waste.RecordedBy.Value]
                    : "N/A";

                return waste.MapToViewAllDto(recordedByName);
            }).ToList();

            return new ServiceResult(Const.SUCCESS_READ_CODE, Const.SUCCESS_READ_MSG, dtos);
        }
        public async Task<IServiceResult> GetByIdAsync(Guid wasteId, Guid userId, bool isAdmin)
        {
            // Get all users to resolve RecordedBy names
            var users = await _unitOfWork.UserAccountRepository.GetAllAsync(
                predicate: u => !u.IsDeleted,
                asNoTracking: true
            );
            var userMap = users.ToDictionary(u => u.UserId, u => u.Name);

            // Fetch the waste entry
            var waste = await _unitOfWork.ProcessingWasteRepository.GetByIdAsync(
                predicate: w => w.WasteId == wasteId && !w.IsDeleted,
                include: q => q.Include(w => w.Progress).ThenInclude(p => p.Batch),
                asNoTracking: true
            );

            if (waste == null)
            {
                return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không tìm thấy dữ liệu chất thải.");
            }

            // If not admin, ensure this waste belongs to the requesting farmer
            if (!isAdmin)
            {
                var farmer = await _unitOfWork.FarmerRepository.FindByUserIdAsync(userId);
                if (farmer == null || waste.Progress?.Batch?.FarmerId != farmer.FarmerId)
                {
                    return new ServiceResult(Const.FAIL_READ_CODE, "Bạn không có quyền truy cập chất thải này.");
                }
            }

            var recordedByName = waste.RecordedBy.HasValue && userMap.ContainsKey(waste.RecordedBy.Value)
                ? userMap[waste.RecordedBy.Value]
                : "N/A";

            var dto = waste.MapToViewAllDto(recordedByName);

            return new ServiceResult(Const.SUCCESS_READ_CODE, Const.SUCCESS_READ_MSG, dto);
        }
        public async Task<IServiceResult> CreateAsync(ProcessingWasteCreateDto dto, Guid userId, bool isAdmin)
        {
            try
            {
                // Retrieve the farmer based on the userId
                var farmer = await _unitOfWork.FarmerRepository.FindByUserIdAsync(userId);
                if (farmer == null && !isAdmin)
                    return new ServiceResult(Const.FAIL_CREATE_CODE, "Chỉ người dùng có vai trò Nông hộ (Farmer) mới được phép tạo xử lý chất thải.");

                // Check if the progress exists
                var progressExists = await _unitOfWork.ProcessingBatchProgressRepository.AnyAsync(
                    x => x.ProgressId == dto.ProgressId && !x.IsDeleted);
                if (!progressExists)
                    return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Tiến trình sơ chế không tồn tại.");

                // Validate if WasteType is provided
                if (string.IsNullOrEmpty(dto.WasteType))
                    return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Loại chất thải không được để trống.");

                // Validate if Quantity is greater than zero
                if (dto.Quantity <= 0)
                    return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Số lượng chất thải phải lớn hơn 0.");

                // Validate if Unit is provided
                if (string.IsNullOrEmpty(dto.Unit))
                    return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Đơn vị không được để trống.");

                // Prepare the ProcessingBatchWaste entity
                var waste = new ProcessingBatchWaste
                {
                    WasteId = Guid.NewGuid(),
                    WasteCode = await _codeGenerator.GenerateProcessingWasteCodeAsync(), // Assuming a WasteCode generator is available
                    ProgressId = dto.ProgressId,
                    WasteType = dto.WasteType,
                    Quantity = dto.Quantity,
                    Unit = dto.Unit.Trim(),
                    Note = dto.Note?.Trim(),
                    RecordedAt = dto.RecordedAt ?? DateTime.UtcNow,
                    RecordedBy = farmer != null ? farmer.FarmerId : userId, // If it's an admin, use the admin userId, else use farmer
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsDeleted = false,
                    IsDisposed = false // Set as not disposed initially
                };

                // Check for duplicate waste record for this progress (if required)
                var isDuplicate = await _unitOfWork.ProcessingWasteRepository.AnyAsync(
                    x => x.ProgressId == waste.ProgressId && x.WasteType == waste.WasteType && !x.IsDeleted);
                if (isDuplicate)
                {
                    return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Loại chất thải đã tồn tại cho tiến trình này.");
                }

                // Save the ProcessingBatchWaste entity
                await _unitOfWork.ProcessingWasteRepository.CreateAsync(waste);

                var result = await _unitOfWork.SaveChangesAsync();
                if (result <= 0)
                    return new ServiceResult(Const.FAIL_CREATE_CODE, "Tạo chất thải không thành công.");

                // Retrieve the created waste record
                var createdWaste = await _unitOfWork.ProcessingWasteRepository.GetByIdAsync(
                    x => x.WasteId == waste.WasteId && !x.IsDeleted,
                    asNoTracking: true
                );

                if (createdWaste != null)
                {
                    // Fetch user details for recordedByName
                    var recordedByUser = isAdmin
                        ? await _unitOfWork.UserAccountRepository.GetByIdAsync(f => f.UserId == createdWaste.RecordedBy)
                        : await _unitOfWork.UserAccountRepository.GetByIdAsync(f => f.UserId == farmer.UserId);

                    var recordedByName = recordedByUser?.Name ?? "N/A";

                    // Map to DTO and return success
                    var viewDto = createdWaste.MapToViewAllDto(recordedByName);
                    return new ServiceResult(Const.SUCCESS_CREATE_CODE, Const.SUCCESS_CREATE_MSG, viewDto);
                }

                return new ServiceResult(Const.FAIL_CREATE_CODE, "Tạo thành công nhưng không truy xuất được bản ghi.");
            }
            catch (Exception ex)
            {
                return new ServiceResult(Const.ERROR_EXCEPTION, ex.Message);
            }
        }

        public async Task<IServiceResult> UpdateAsync(Guid wasteId, ProcessingWasteUpdateDto dto, Guid userId, bool isAdmin)
        {
            try
            {
                // 1. Kiểm tra vai trò Farmer nếu không phải Admin
                var farmer = await _unitOfWork.FarmerRepository.FindByUserIdAsync(userId);
                if (!isAdmin && farmer == null)
                    return new ServiceResult(Const.FAIL_UPDATE_CODE, "Chỉ Farmer hoặc Admin mới được phép cập nhật chất thải.");

                // 2. Lấy bản ghi chất thải cần cập nhật
                var waste = await _unitOfWork.ProcessingWasteRepository.GetByIdAsync(
                    predicate: x => x.WasteId == wasteId && !x.IsDeleted,
                    asNoTracking: false
                );

                if (waste == null)
                    return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không tìm thấy bản ghi chất thải cần cập nhật.");

                // 3. Nếu không phải Admin, chỉ cho phép cập nhật bản ghi do chính Farmer đó tạo
                if (!isAdmin && waste.RecordedBy != farmer.FarmerId)
                    return new ServiceResult(Const.FAIL_UPDATE_CODE, "Không được phép cập nhật bản ghi chất thải của người khác.");

                // 4. Kiểm tra tiến trình tồn tại
                var progressExists = await _unitOfWork.ProcessingBatchProgressRepository.AnyAsync(
                    x => x.ProgressId == dto.ProgressId && !x.IsDeleted);
                if (!progressExists)
                    return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Tiến trình sơ chế không tồn tại.");

                // 5. Cập nhật dữ liệu
                waste.ProgressId = dto.ProgressId;
                waste.WasteType = dto.WasteType?.Trim();
                waste.Quantity = dto.Quantity;
                waste.Unit = dto.Unit?.Trim();
                waste.Note = dto.Note?.Trim();
                waste.RecordedAt = dto.RecordedAt ?? waste.RecordedAt;
                waste.UpdatedAt = DateHelper.NowVietnamTime();

                _unitOfWork.ProcessingWasteRepository.PrepareUpdate(waste);
                var result = await _unitOfWork.SaveChangesAsync();

                if (result <= 0)
                    return new ServiceResult(Const.FAIL_UPDATE_CODE, "Cập nhật bản ghi thất bại.");

                // 6. Truy xuất lại bản ghi để trả về
                var updated = await _unitOfWork.ProcessingWasteRepository.GetByIdAsync(
                    predicate: x => x.WasteId == wasteId && !x.IsDeleted,
                    asNoTracking: true
                );

                if (updated == null)
                    return new ServiceResult(Const.FAIL_UPDATE_CODE, "Cập nhật thành công nhưng không truy xuất được bản ghi.");

                // 7. Truy ngược Farmer → User để lấy recordedByName
                var recordedFarmer = await _unitOfWork.FarmerRepository.GetByIdAsync(f => f.FarmerId == updated.RecordedBy);
                var recordedUser = await _unitOfWork.UserAccountRepository.GetByIdAsync(u => u.UserId == recordedFarmer.UserId);
                var recordedByName = recordedUser?.Name ?? "N/A";

                // 8. Map và trả về DTO
                var viewDto = updated.MapToViewAllDto(recordedByName);
                return new ServiceResult(Const.SUCCESS_UPDATE_CODE, Const.SUCCESS_UPDATE_MSG, viewDto);
            }
            catch (Exception ex)
            {
                return new ServiceResult(Const.ERROR_EXCEPTION, ex.Message);
            }
        }
        public async Task<IServiceResult> SoftDeleteAsync(Guid wasteId, Guid userId, bool isAdmin)
        {
            try
            {
                var waste = await _unitOfWork.ProcessingWasteRepository.GetByIdAsync(
                    x => x.WasteId == wasteId && !x.IsDeleted,
                    include: q => q.Include(w => w.Progress).ThenInclude(p => p.Batch),
                    asNoTracking: false
                );

                if (waste == null)
                    return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không tìm thấy bản ghi chất thải.");

                if (!isAdmin)
                {
                    var farmer = await _unitOfWork.FarmerRepository.FindByUserIdAsync(userId);
                    if (farmer == null || waste.Progress?.Batch?.FarmerId != farmer.FarmerId)
                        return new ServiceResult(Const.FAIL_DELETE_CODE, "Bạn không có quyền xóa bản ghi này.");
                }

                waste.IsDeleted = true;
                waste.UpdatedAt = DateHelper.NowVietnamTime();

                _unitOfWork.ProcessingWasteRepository.PrepareUpdate(waste);
                var result = await _unitOfWork.SaveChangesAsync();

                return result > 0
                    ? new ServiceResult(Const.SUCCESS_DELETE_CODE, "Xóa mềm thành công.")
                    : new ServiceResult(Const.FAIL_DELETE_CODE, "Xóa mềm thất bại.");
            }
            catch (Exception ex)
            {
                return new ServiceResult(Const.ERROR_EXCEPTION, ex.Message);
            }
        }
        public async Task<IServiceResult> HardDeleteAsync(Guid wasteId, Guid userId, bool isAdmin)
        {
            try
            {
                var waste = await _unitOfWork.ProcessingWasteRepository.GetByIdAsync(
                    x => x.WasteId == wasteId,
                    include: q => q.Include(w => w.Progress).ThenInclude(p => p.Batch),
                    asNoTracking: false
                );

                if (waste == null)
                    return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không tìm thấy bản ghi chất thải.");

                if (!isAdmin)
                {
                    var farmer = await _unitOfWork.FarmerRepository.FindByUserIdAsync(userId);
                    if (farmer == null || waste.Progress?.Batch?.FarmerId != farmer.FarmerId)
                        return new ServiceResult(Const.FAIL_DELETE_CODE, "Bạn không có quyền xóa bản ghi này.");
                }

                _unitOfWork.ProcessingWasteRepository.PrepareRemove(waste);
                var result = await _unitOfWork.SaveChangesAsync();

                return result > 0
                    ? new ServiceResult(Const.SUCCESS_DELETE_CODE, "Xóa cứng thành công.")
                    : new ServiceResult(Const.FAIL_DELETE_CODE, "Xóa cứng thất bại.");
            }
            catch (Exception ex)
            {
                return new ServiceResult(Const.ERROR_EXCEPTION, ex.Message);
            }
        }

    }
}
