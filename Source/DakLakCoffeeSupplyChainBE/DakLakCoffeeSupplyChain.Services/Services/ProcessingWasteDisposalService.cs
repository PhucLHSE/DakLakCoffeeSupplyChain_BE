using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.ProcessingWasteDisposalDTOs;
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
    public class ProcessingWasteDisposalService : IProcessingWasteDisposalService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICodeGenerator _codeGenerator;

        public ProcessingWasteDisposalService(IUnitOfWork unitOfWork, ICodeGenerator codeGenerator)
        {
            _unitOfWork = unitOfWork;
            _codeGenerator = codeGenerator;
        }

        public async Task<IServiceResult> GetAllAsync(Guid userId, bool isAdmin)
        {
            try
            {
                // 1. Lấy danh sách Farmer + User để map FarmerId -> Name
                var farmers = await _unitOfWork.FarmerRepository.GetAllAsync(
                    predicate: f => !f.IsDeleted,
                    include: q => q.Include(f => f.User),
                    asNoTracking: true
                );
                var farmerNameMap = farmers.ToDictionary(f => f.FarmerId, f => f.User?.Name ?? "N/A");

                // 2. Lấy query xử lý chất thải
                var query = _unitOfWork.ProcessingWasteDisposalRepository.GetAllQueryable()
                    .Where(x => !x.IsDeleted);

                List<ProcessingWasteDisposal> disposals;

                if (isAdmin)
                {
                    // Admin xem tất cả
                    disposals = await query.OrderByDescending(x => x.CreatedAt).ToListAsync();
                }
                else
                {
                    // Kiểm tra xem user có phải BusinessManager không
                    var manager = await _unitOfWork.BusinessManagerRepository.GetByUserIdAsync(userId);
                    if (manager != null)
                    {
                        // Lấy danh sách FarmerId do BM quản lý (qua FarmingCommitment.ApprovedBy == manager.ManagerId)
                        var farmingCommitments = await _unitOfWork.FarmingCommitmentRepository.GetAllAsync(
                            fc => !fc.IsDeleted && fc.ApprovedBy == manager.ManagerId,
                            asNoTracking: true
                        );
                        var farmerIdsManaged = farmingCommitments.Select(fc => fc.FarmerId).Distinct().ToList();

                        disposals = await query
                            .Where(x => farmerIdsManaged.Contains(x.HandledBy ?? Guid.Empty))
                            .OrderByDescending(x => x.CreatedAt)
                            .ToListAsync();

                        if (disposals == null || disposals.Count == 0)
                            return new ServiceResult(Const.FAIL_READ_CODE, "Bạn không có quyền truy cập dữ liệu xử lý chất thải.");
                    }
                    else
                    {
                        // Nếu không phải BM, kiểm tra Farmer
                        var farmer = await _unitOfWork.FarmerRepository.FindByUserIdAsync(userId);
                        if (farmer == null)
                            return new ServiceResult(Const.FAIL_READ_CODE, "Không tìm thấy nông hộ.");

                        disposals = await query
                            .Where(x => x.HandledBy == farmer.FarmerId)
                            .OrderByDescending(x => x.CreatedAt)
                            .ToListAsync();

                        if (disposals == null || disposals.Count == 0)
                            return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Bạn chưa tạo xử lý chất thải nào.");
                    }
                }

                // 3. Map thủ công tên người xử lý dựa trên HandledBy
                var dtos = disposals.Select(x =>
                {
                    var name = farmerNameMap.TryGetValue(x.HandledBy ?? Guid.Empty, out var n) ? n : "N/A";
                    return x.MapToDto(name);
                }).ToList();

                return new ServiceResult(Const.SUCCESS_READ_CODE, Const.SUCCESS_READ_MSG, dtos);
            }
            catch (Exception ex)
            {
                return new ServiceResult(Const.ERROR_EXCEPTION, ex.Message);
            }
        }


        public async Task<IServiceResult> GetByIdAsync(Guid disposalId)
        {
            try
            {
                var entity = await _unitOfWork.ProcessingWasteDisposalRepository.GetAllQueryable()
                    .Include(x => x.Waste)
                    .FirstOrDefaultAsync(x => x.DisposalId == disposalId && !x.IsDeleted);

                if (entity == null)
                    return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không tìm thấy bản ghi.");

                // Lấy tên người xử lý
                string handledByName = "N/A";
                if (entity.HandledBy.HasValue)
                {
                    var farmer = await _unitOfWork.FarmerRepository.FindByUserIdAsync(entity.HandledBy.Value);
                    if (farmer != null)
                    {
                        handledByName = farmer.User?.Name ?? "N/A";
                    }
                }

                var dto = new ProcessingWasteDisposalViewByIdDto
                {
                    DisposalId = entity.DisposalId,
                    DisposalCode = entity.DisposalCode,
                    WasteId = entity.WasteId,
                    WasteName = entity.Waste?.WasteType ?? "N/A",
                    DisposalMethod = entity.DisposalMethod,
                    HandledBy = entity.HandledBy ?? Guid.Empty,
                    HandledByName = handledByName,
                    HandledAt = entity.HandledAt,
                    Notes = entity.Notes,
                    IsSold = entity.IsSold ?? false,
                    Revenue = entity.Revenue,
                    CreatedAt = entity.CreatedAt
                };

                return new ServiceResult(Const.SUCCESS_READ_CODE, Const.SUCCESS_READ_MSG, dto);
            }
            catch (Exception ex)
            {
                return new ServiceResult(Const.ERROR_EXCEPTION, ex.Message);
            }
        }

        public async Task<IServiceResult> CreateAsync(ProcessingWasteDisposalCreateDto dto, Guid userId)
        {
            try
            {
                // Không cho BusinessManager tạo
                var manager = await _unitOfWork.BusinessManagerRepository.GetByIdAsync(
                    m => m.UserId == userId && !m.IsDeleted
                );
                if (manager != null)
                    return new ServiceResult(Const.FAIL_CREATE_CODE, "Business Manager không được tạo xử lý chất thải.");

                // Chỉ cho Farmer tạo
                var farmer = await _unitOfWork.FarmerRepository.FindByUserIdAsync(userId);
                if (farmer == null)
                    return new ServiceResult(Const.FAIL_CREATE_CODE, "Chỉ nông hộ mới được tạo xử lý chất thải.");

                // Sinh mã xử lý
                var disposalCode = await _codeGenerator.GenerateProcessingWasteDisposalCodeAsync();
                var isExist = await _unitOfWork.ProcessingWasteDisposalRepository
                    .AnyAsync(x => x.DisposalCode == disposalCode && !x.IsDeleted);

                if (isExist)
                    return new ServiceResult(Const.ERROR_VALIDATION_CODE, "Mã xử lý chất thải đã tồn tại.");

                // Tạo entity
                var disposal = new ProcessingWasteDisposal
                {
                    DisposalId = Guid.NewGuid(),
                    DisposalCode = disposalCode,
                    WasteId = dto.WasteId,
                    DisposalMethod = dto.DisposalMethod,
                    HandledBy = farmer.FarmerId,
                    HandledAt = dto.HandledAt,
                    Notes = dto.Notes,
                    IsSold = dto.IsSold,
                    Revenue = dto.Revenue,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsDeleted = false
                };

                // Lưu DB
                await _unitOfWork.ProcessingWasteDisposalRepository.CreateAsync(disposal);
                var saveResult = await _unitOfWork.SaveChangesAsync();

                if (saveResult <= 0)
                    return new ServiceResult(Const.FAIL_CREATE_CODE, "Tạo xử lý chất thải thất bại.");

                return new ServiceResult(Const.SUCCESS_CREATE_CODE, Const.SUCCESS_CREATE_MSG, disposal.DisposalId);
            }
            catch (Exception ex)
            {
                return new ServiceResult(Const.ERROR_EXCEPTION, ex.Message);
            }
        }
        public async Task<IServiceResult> UpdateAsync(Guid id, ProcessingWasteDisposalUpdateDto dto, Guid userId)
        {
            try
            {
                var disposal = await _unitOfWork.ProcessingWasteDisposalRepository.GetByIdAsync(
                    predicate: x => x.DisposalId == id && !x.IsDeleted
                );

                if (disposal == null)
                    return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không tìm thấy thông tin xử lý chất thải.");

                // Lấy Farmer từ userId
                var farmer = await _unitOfWork.FarmerRepository.GetByIdAsync(
                    f => f.UserId == userId && !f.IsDeleted
                );

                if (farmer == null)
                    return new ServiceResult(Const.FAIL_UPDATE_CODE, "Người dùng hiện tại không phải là nông dân hợp lệ.");

                if (disposal.HandledBy != farmer.FarmerId)
                    return new ServiceResult(Const.FAIL_UPDATE_CODE, "Bạn không có quyền cập nhật thông tin xử lý chất thải này.");

                // Validate dữ liệu
                if (string.IsNullOrWhiteSpace(dto.DisposalMethod))
                    return new ServiceResult(Const.ERROR_VALIDATION_CODE, "Phương pháp xử lý không được để trống.");

                if (dto.WasteId == Guid.Empty)
                    return new ServiceResult(Const.ERROR_VALIDATION_CODE, "Loại chất thải không hợp lệ.");

                var wasteExists = await _unitOfWork.ProcessingWasteRepository.AnyAsync(w => w.WasteId == dto.WasteId && !w.IsDeleted);
                if (!wasteExists)
                    return new ServiceResult(Const.ERROR_VALIDATION_CODE, "Loại chất thải không tồn tại hoặc đã bị xóa.");

                // Cập nhật dữ liệu
                disposal.WasteId = dto.WasteId;
                disposal.DisposalMethod = dto.DisposalMethod;
                disposal.HandledAt = dto.HandledAt;
                disposal.Notes = dto.Notes;
                disposal.IsSold = dto.IsSold;
                disposal.Revenue = dto.Revenue;
                disposal.UpdatedAt = DateTime.UtcNow;

                await _unitOfWork.ProcessingWasteDisposalRepository.UpdateAsync(disposal);
                var result = await _unitOfWork.SaveChangesAsync();

                return result > 0
                    ? new ServiceResult(Const.SUCCESS_UPDATE_CODE, "Cập nhật xử lý chất thải thành công.")
                    : new ServiceResult(Const.FAIL_UPDATE_CODE, "Cập nhật thất bại.");
            }
            catch (Exception ex)
            {
                return new ServiceResult(Const.ERROR_EXCEPTION, ex.Message);
            }
        }

        public async Task<bool> HasPermissionToDeleteAsync(Guid disposalId, Guid userId)
        {
            var disposal = await _unitOfWork.ProcessingWasteDisposalRepository.GetByIdAsync(
                predicate: d => d.DisposalId == disposalId && !d.IsDeleted,
                asNoTracking: true
            );
            if (disposal == null) return false;

            var user = await _unitOfWork.UserAccountRepository.GetByIdAsync(
                predicate: u => u.UserId == userId,
                include: q => q.Include(u => u.Role),
                asNoTracking: true
            );
            if (user == null) return false;

            var roleName = user.Role?.RoleName;

            if (roleName == "Admin")
                return false;

            if (roleName == "Farmer")
                return disposal.HandledBy == userId;

            if (roleName == "BusinessManager")
            {
                var bm = await _unitOfWork.BusinessManagerRepository.GetByIdAsync(
                    predicate: bm => bm.UserId == userId && !bm.IsDeleted,
                    asNoTracking: true
                );

                if (bm == null) return false;

                return await _unitOfWork.FarmingCommitmentRepository.AnyAsync(fc =>
                    fc.FarmerId == disposal.HandledBy &&
                    fc.ApprovedBy == bm.ManagerId &&
                    !fc.IsDeleted
                );
            }

            return false;
        }
        public async Task<IServiceResult> SoftDeleteAsync(Guid disposalId, Guid userId, bool isManager)
        {
            var disposal = await _unitOfWork.ProcessingWasteDisposalRepository.GetByIdAsync(
                predicate: d => d.DisposalId == disposalId && !d.IsDeleted
            );

            if (disposal == null)
                return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không tìm thấy thông tin xử lý chất thải.");

            if (!await HasPermissionToDeleteAsync(disposalId, userId))
                return new ServiceResult(Const.FAIL_DELETE_CODE, "Bạn không có quyền xoá xử lý chất thải này.");

            disposal.IsDeleted = true;
            disposal.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.ProcessingWasteDisposalRepository.UpdateAsync(disposal);
            var result = await _unitOfWork.SaveChangesAsync();

            return result > 0
                ? new ServiceResult(Const.SUCCESS_DELETE_CODE, "Xoá mềm thành công.")
                : new ServiceResult(Const.FAIL_DELETE_CODE, "Xoá mềm thất bại.");
        }
        public async Task<IServiceResult> HardDeleteAsync(Guid disposalId, Guid userId, bool isManager)
        {
            try
            {
                var disposal = await _unitOfWork.ProcessingWasteDisposalRepository.GetByIdAsync(
                    predicate: d => d.DisposalId == disposalId && !d.IsDeleted
                );

                if (disposal == null)
                    return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không tìm thấy thông tin xử lý chất thải.");

                if (!await HasPermissionToDeleteAsync(disposalId, userId))
                    return new ServiceResult(Const.FAIL_DELETE_CODE, "Bạn không có quyền xoá xử lý chất thải này.");

                await _unitOfWork.ProcessingWasteDisposalRepository.RemoveAsync(disposal);
                var result = await _unitOfWork.SaveChangesAsync();

                return result > 0
                    ? new ServiceResult(Const.SUCCESS_DELETE_CODE, "Đã xóa vĩnh viễn thông tin xử lý chất thải.")
                    : new ServiceResult(Const.FAIL_DELETE_CODE, "Xóa vĩnh viễn thất bại.");
            }
            catch (Exception ex)
            {
                return new ServiceResult(Const.ERROR_EXCEPTION, ex.Message);
            }
        }


    }
}