using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.ProcessingBatchDTOs;
using DakLakCoffeeSupplyChain.Common.Enum.ProcessingEnums;
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
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Services.Services
{
    public class ProcessingBatchService : IProcessingBatchService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICodeGenerator _codeGenerator;

        public ProcessingBatchService(IUnitOfWork unitOfWork, ICodeGenerator codeGenerator)
        {
            _unitOfWork = unitOfWork;
            _codeGenerator = codeGenerator;
        }

        private bool HasPermissionToAccess(ProcessingBatch batch, Guid userId, bool isAdmin, bool isManager)
        {
            if (isAdmin || isManager) return true;
            return batch.Farmer?.UserId == userId;
        }

        public async Task<IServiceResult> GetAll()
        {
            var batches = await _unitOfWork.ProcessingBatchRepository.GetAll();

            if (batches == null || !batches.Any())
            {
                return new ServiceResult(
                    Const.WARNING_NO_DATA_CODE,
                    Const.WARNING_NO_DATA_MSG,
                    new List<ProcessingBatchViewDto>()
                );
            }

            var dtoList = batches.Select(b => b.MapToProcessingBatchViewDto()).ToList();

            return new ServiceResult(
                Const.SUCCESS_READ_CODE,
                Const.SUCCESS_READ_MSG,
                dtoList
            );
        }

        public async Task<IServiceResult> GetAllByUserId(Guid userId, bool isAdmin, bool isManager)
        {
            List<ProcessingBatch> batches;

            if (isAdmin)
            {
                batches = await _unitOfWork.ProcessingBatchRepository.GetAllAsync(
                    predicate: x => !x.IsDeleted,
                    include: query => query
                        .Include(x => x.Method)
                        .Include(x => x.CropSeason).ThenInclude(cs => cs.Commitment)
                        .Include(x => x.Farmer).ThenInclude(f => f.User)
                        .Include(x => x.ProcessingBatchProgresses),
                    orderBy: q => q.OrderByDescending(x => x.CreatedAt),
                    asNoTracking: true
                );
            }
            else if (isManager)
            {
                var manager = await _unitOfWork.BusinessManagerRepository
                    .GetByIdAsync(m => m.UserId == userId && !m.IsDeleted);

                if (manager == null)
                    return new ServiceResult(Const.FAIL_READ_CODE, "Không tìm thấy Business Manager tương ứng.");

                var managerId = manager.ManagerId;

                batches = await _unitOfWork.ProcessingBatchRepository.GetAllAsync(
                    predicate: x =>
                        !x.IsDeleted &&
                        x.CropSeason != null &&
                        x.CropSeason.Commitment != null &&
                        x.CropSeason.Commitment.ApprovedBy == managerId,
                    include: query => query
                        .Include(x => x.Method)
                        .Include(x => x.CropSeason).ThenInclude(cs => cs.Commitment)
                        .Include(x => x.Farmer).ThenInclude(f => f.User)
                        .Include(x => x.ProcessingBatchProgresses),
                    orderBy: q => q.OrderByDescending(x => x.CreatedAt),
                    asNoTracking: true
                );

                if (batches == null || !batches.Any())
                {
                    return new ServiceResult(Const.FAIL_READ_CODE, "Bạn không có quyền truy cập bất kỳ lô sơ chế nào.");
                }
            }
            else
            {
                var farmer = await _unitOfWork.FarmerRepository.GetByIdAsync(f => f.UserId == userId && !f.IsDeleted);

                if (farmer == null)
                {
                    return new ServiceResult(Const.FAIL_READ_CODE, "Không tìm thấy thông tin nông hộ.");
                }

                batches = await _unitOfWork.ProcessingBatchRepository.GetAllAsync(
                    predicate: x => !x.IsDeleted && x.FarmerId == farmer.FarmerId,
                    include: query => query
                        .Include(x => x.Method)
                        .Include(x => x.CropSeason).ThenInclude(cs => cs.Commitment)
                        .Include(x => x.Farmer).ThenInclude(f => f.User)
                        .Include(x => x.ProcessingBatchProgresses),
                    orderBy: q => q.OrderByDescending(x => x.CreatedAt),
                    asNoTracking: true
                );

                if (batches == null || !batches.Any())
                {
                    return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Bạn chưa tạo lô sơ chế nào.");
                }
            }

            var dtoList = batches.Select(b => b.MapToProcessingBatchViewDto()).ToList();
            return new ServiceResult(Const.SUCCESS_READ_CODE, Const.SUCCESS_READ_MSG, dtoList);
        }

        public async Task<IServiceResult> CreateAsync(ProcessingBatchCreateDto dto, Guid userId)
        {
            
            var manager = await _unitOfWork.BusinessManagerRepository.GetByIdAsync(m => m.UserId == userId && !m.IsDeleted);
            if (manager != null)
                return new ServiceResult(Const.FAIL_CREATE_CODE, "Business Manager không được tạo lô sơ chế.");

            
            var farmer = await _unitOfWork.FarmerRepository.FindByUserIdAsync(userId);
            if (farmer == null)
                return new ServiceResult(Const.FAIL_CREATE_CODE, "Chỉ nông hộ mới được tạo lô sơ chế.");

            
            var cropSeason = await _unitOfWork.CropSeasonRepository.GetByIdAsync(dto.CropSeasonId);
            if (cropSeason == null || cropSeason.IsDeleted)
                return new ServiceResult(Const.FAIL_CREATE_CODE, "Mùa vụ không hợp lệ.");

            
            var method = await _unitOfWork.ProcessingMethodRepository.GetByIdAsync(dto.MethodId);
            if (method == null)
                return new ServiceResult(Const.FAIL_CREATE_CODE, "Phương pháp sơ chế không hợp lệ.");

            var cropSeasonDetails = await _unitOfWork.CropSeasonDetailRepository
     .GetAllAsync(d => d.CropSeasonId == dto.CropSeasonId && !d.IsDeleted);

            if (cropSeasonDetails == null || !cropSeasonDetails.Any())
                return new ServiceResult(Const.FAIL_CREATE_CODE, "Không tìm thấy chi tiết mùa vụ để tính khối lượng.");

            var totalActualYield = cropSeasonDetails
                .Where(d => d.ActualYield.HasValue)
                .Sum(d => d.ActualYield.Value);

            if (totalActualYield <= 0)
                return new ServiceResult(Const.FAIL_CREATE_CODE, "Khối lượng đầu ra của mùa vụ chưa được cập nhật hoặc bằng 0.");

         
            int year = cropSeason.StartDate?.Year ?? DateTime.Now.Year;
            string systemBatchCode = await _codeGenerator.GenerateProcessingSystemBatchCodeAsync(year);

           
            var batch = new ProcessingBatch
            {
                BatchId = Guid.NewGuid(),
                BatchCode = dto.BatchCode?.Trim(),
                SystemBatchCode = systemBatchCode,
                CropSeasonId = dto.CropSeasonId,
                CoffeeTypeId = dto.CoffeeTypeId,
                MethodId = dto.MethodId,
                InputQuantity = totalActualYield,
                InputUnit = dto.InputUnit?.Trim() ?? "kg",
                FarmerId = farmer.FarmerId,
                Status = ProcessingStatus.NotStarted.ToString(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

           
            await _unitOfWork.ProcessingBatchRepository.CreateAsync(batch);
            var saveResult = await _unitOfWork.SaveChangesAsync();

            if (saveResult <= 0)
                return new ServiceResult(Const.FAIL_CREATE_CODE, "Tạo lô sơ chế thất bại.");

            
            var created = await _unitOfWork.ProcessingBatchRepository.GetByIdAsync(
                x => x.BatchId == batch.BatchId,
                include: q => q
                    .Include(x => x.Method)
                    .Include(x => x.CropSeason)
                    .Include(x => x.Farmer).ThenInclude(f => f.User),
                asNoTracking: true
            );

            var dtoResult = created?.MapToProcessingBatchViewDto();
            return new ServiceResult(Const.SUCCESS_CREATE_CODE, Const.SUCCESS_CREATE_MSG, dtoResult);
        }


        public async Task<IServiceResult> UpdateAsync(ProcessingBatchUpdateDto dto, Guid userId, bool isAdmin, bool isManager)
        {
            var batch = await _unitOfWork.ProcessingBatchRepository.GetByIdAsync(dto.BatchId);
            if (batch == null || batch.IsDeleted)
                return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không tìm thấy lô sơ chế.");

            if (!HasPermissionToAccess(batch, userId, isAdmin, isManager))
                return new ServiceResult(Const.FAIL_UPDATE_CODE, "Bạn không có quyền cập nhật lô sơ chế này.");

            batch.CoffeeTypeId = dto.CoffeeTypeId;
            batch.CropSeasonId = dto.CropSeasonId;
            batch.MethodId = dto.MethodId;
            batch.InputQuantity = dto.InputQuantity;
            batch.InputUnit = dto.InputUnit?.Trim();
            batch.Status = dto.Status.ToString();
            batch.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.ProcessingBatchRepository.UpdateAsync(batch);
            var result = await _unitOfWork.SaveChangesAsync();

            return result > 0
                ? new ServiceResult(Const.SUCCESS_UPDATE_CODE, "Cập nhật thành công.")
                : new ServiceResult(Const.FAIL_UPDATE_CODE, "Cập nhật thất bại.");
        }

        public async Task<IServiceResult> SoftDeleteAsync(Guid id, Guid userId, bool isAdmin, bool isManager)
        {
            var batch = await _unitOfWork.ProcessingBatchRepository.GetByIdAsync(
                predicate: b => b.BatchId == id && !b.IsDeleted,
                include: q => q.Include(b => b.Farmer).ThenInclude(f => f.User)
            );

            if (batch == null)
                return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Lô sơ chế không tồn tại.");

            if (!HasPermissionToAccess(batch, userId, isAdmin, isManager))
                return new ServiceResult(Const.FAIL_DELETE_CODE, "Bạn không có quyền xoá lô sơ chế này.");

            batch.IsDeleted = true;
            batch.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.ProcessingBatchRepository.UpdateAsync(batch);
            var result = await _unitOfWork.SaveChangesAsync();

            return result > 0
                ? new ServiceResult(Const.SUCCESS_DELETE_CODE, "Xoá mềm thành công.")
                : new ServiceResult(Const.FAIL_DELETE_CODE, "Xoá mềm thất bại.");
        }

        public async Task<IServiceResult> HardDeleteAsync(Guid batchId, Guid userId, bool isAdmin, bool isManager)
        {
            try
            {
                var batch = await _unitOfWork.ProcessingBatchRepository.GetByIdAsync(
                    predicate: x => x.BatchId == batchId && !x.IsDeleted,
                    include: q => q.Include(x => x.Farmer).ThenInclude(f => f.User)
                );

                if (batch == null)
                    return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không tìm thấy mẻ sơ chế.");

                if (!HasPermissionToAccess(batch, userId, isAdmin, isManager))
                    return new ServiceResult(Const.FAIL_DELETE_CODE, "Không được xóa mẻ sơ chế của người khác.");

                await _unitOfWork.ProcessingBatchRepository.RemoveAsync(batch);
                var result = await _unitOfWork.SaveChangesAsync();

                return result > 0
                    ? new ServiceResult(Const.SUCCESS_DELETE_CODE, "Đã xóa vĩnh viễn mẻ sơ chế.")
                    : new ServiceResult(Const.FAIL_DELETE_CODE, Const.FAIL_DELETE_MSG);
            }
            catch (Exception ex)
            {
                return new ServiceResult(Const.ERROR_EXCEPTION, ex.Message);
            }
        }

        public async Task<IServiceResult> GetByIdAsync(Guid id, Guid userId, bool isAdmin, bool isManager)
        {
            var batch = await _unitOfWork.ProcessingBatchRepository.GetByIdAsync(
                x => x.BatchId == id && !x.IsDeleted,
                include: q => q
                    .Include(x => x.CropSeason)
                    .Include(x => x.Method)
                    .Include(x => x.Farmer).ThenInclude(f => f.User)
                    .Include(x => x.ProcessingBatchProgresses.Where(p => !p.IsDeleted))
                        .ThenInclude(p => p.Stage)
                    .Include(x => x.ProcessingBatchProgresses.Where(p => !p.IsDeleted))
                        .ThenInclude(p => p.ProcessingParameters),
                asNoTracking: true
            );

            if (batch == null)
                return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không tìm thấy lô sơ chế.");

            if (!HasPermissionToAccess(batch, userId, isAdmin, isManager))
                return new ServiceResult(Const.FAIL_UPDATE_CODE, "Bạn không có quyền truy cập lô sơ chế này.");

            // Lấy tên nông dân từ bảng Farmer → User
            var farmer = await _unitOfWork.FarmerRepository
                .GetAllQueryable()
                .Include(f => f.User)
                .FirstOrDefaultAsync(f => f.FarmerId == batch.FarmerId);

            var farmerName = farmer?.User?.Name ?? "N/A";

            var dto = batch.MapToDetailsDto(farmerName);
            return new ServiceResult(Const.SUCCESS_READ_CODE, Const.SUCCESS_READ_MSG, dto);
        }


    }
}