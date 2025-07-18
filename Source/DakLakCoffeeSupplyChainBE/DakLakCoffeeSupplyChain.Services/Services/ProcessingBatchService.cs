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
using System.Text;
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
            if (isAdmin || isManager)
            {
                var batches = await _unitOfWork.ProcessingBatchRepository.GetAllAsync(
                    predicate: x => !x.IsDeleted,
                    include: query => query
                        .Include(x => x.Method)
                        .Include(x => x.CropSeason)
                        .Include(x => x.Farmer).ThenInclude(f => f.User)
                        .Include(x => x.ProcessingBatchProgresses),
                    orderBy: q => q.OrderByDescending(x => x.CreatedAt),
                    asNoTracking: true
                );

                if (!batches.Any())
                    return new ServiceResult(Const.WARNING_NO_DATA_CODE, Const.WARNING_NO_DATA_MSG);

                var dtoList = batches.Select(b => b.MapToProcessingBatchViewDto()).ToList();
                return new ServiceResult(Const.SUCCESS_READ_CODE, Const.SUCCESS_READ_MSG, dtoList);
            }
            else
            {
                var farmer = await _unitOfWork.FarmerRepository.GetByIdAsync(f => f.UserId == userId && !f.IsDeleted);
                if (farmer == null)
                    return new ServiceResult(Const.FAIL_READ_CODE, "Không tìm thấy thông tin nông hộ.");

                var batches = await _unitOfWork.ProcessingBatchRepository.GetAllAsync(
                    predicate: x => !x.IsDeleted && x.FarmerId == farmer.FarmerId,
                    include: query => query
                        .Include(x => x.Method)
                        .Include(x => x.CropSeason)
                        .Include(x => x.Farmer).ThenInclude(f => f.User)
                        .Include(x => x.ProcessingBatchProgresses),
                    orderBy: q => q.OrderByDescending(x => x.CreatedAt),
                    asNoTracking: true
                );

                if (!batches.Any())
                    return new ServiceResult(Const.WARNING_NO_DATA_CODE, Const.WARNING_NO_DATA_MSG);

                var dtoList = batches.Select(b => b.MapToProcessingBatchViewDto()).ToList();
                return new ServiceResult(Const.SUCCESS_READ_CODE, Const.SUCCESS_READ_MSG, dtoList);
            }
        }

        public async Task<IServiceResult> CreateAsync(ProcessingBatchCreateDto dto, Guid userId)
        {
            // 1. Kiểm tra quyền và tìm Farmer
            var farmer = await _unitOfWork.FarmerRepository.FindByUserIdAsync(userId);
            if (farmer == null)
                return new ServiceResult(Const.FAIL_CREATE_CODE, "Chỉ nông hộ mới được tạo lô sơ chế.");

            // 2. Kiểm tra mùa vụ
            var cropSeason = await _unitOfWork.CropSeasonRepository.GetByIdAsync(dto.CropSeasonId);
            if (cropSeason == null || cropSeason.IsDeleted)
                return new ServiceResult(Const.FAIL_CREATE_CODE, "Mùa vụ không hợp lệ.");

            // 3. Kiểm tra phương pháp sơ chế
            var method = await _unitOfWork.ProcessingMethodRepository.GetByIdAsync(dto.MethodId);
            if (method == null)
                return new ServiceResult(Const.FAIL_CREATE_CODE, "Phương pháp sơ chế không hợp lệ.");

            // 4. Sinh mã SystemBatchCode từ năm mùa vụ
            int year = cropSeason.StartDate?.Year ?? DateTime.Now.Year;
            string systemBatchCode = await _codeGenerator.GenerateProcessingSystemBatchCodeAsync(year);

            // 5. Tạo entity mới
            var batch = new ProcessingBatch
            {
                BatchId = Guid.NewGuid(),
                BatchCode = dto.BatchCode?.Trim(),
                SystemBatchCode = systemBatchCode,
                CropSeasonId = dto.CropSeasonId,
                CoffeeTypeId = dto.CoffeeTypeId,
                MethodId = dto.MethodId,
                InputQuantity = dto.InputQuantity,
                InputUnit = dto.InputUnit?.Trim(),
                FarmerId = farmer.FarmerId,
                Status = ProcessingStatus.NotStarted.ToString(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            // 6. Ghi DB
            await _unitOfWork.ProcessingBatchRepository.CreateAsync(batch);
            var saveResult = await _unitOfWork.SaveChangesAsync();

            if (saveResult <= 0)
                return new ServiceResult(Const.FAIL_CREATE_CODE, "Tạo lô sơ chế thất bại.");

            // 7. Truy xuất lại bản ghi để trả về DTO
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

        public async Task<IServiceResult> UpdateAsync(ProcessingBatchUpdateDto dto, Guid userId, bool isAdmin)
        {
            var batch = await _unitOfWork.ProcessingBatchRepository.GetByIdAsync(dto.BatchId);
            if (batch == null || batch.IsDeleted)
                return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không tìm thấy lô sơ chế.");

            // Quyền truy cập
            if (!isAdmin && batch.Farmer?.UserId != userId)
                return new ServiceResult(Const.FAIL_UPDATE_CODE, "Bạn không có quyền cập nhật lô sơ chế này.");

            // Cập nhật thông tin
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

        public async Task<IServiceResult> SoftDeleteAsync(Guid id, Guid userId, bool isAdmin)
        {
            var batch = await _unitOfWork.ProcessingBatchRepository.GetByIdAsync(
                predicate: b => b.BatchId == id && !b.IsDeleted,
                include: q => q.Include(b => b.Farmer).ThenInclude(f => f.User)
            );

            if (batch == null)
                return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Lô sơ chế không tồn tại.");

            if (!isAdmin && batch.Farmer?.UserId != userId)
                return new ServiceResult(Const.FAIL_DELETE_CODE, "Bạn không có quyền xoá lô sơ chế này.");

            batch.IsDeleted = true;
            batch.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.ProcessingBatchRepository.UpdateAsync(batch);
            var result = await _unitOfWork.SaveChangesAsync();

            return result > 0
                ? new ServiceResult(Const.SUCCESS_DELETE_CODE, "Xoá mềm thành công.")
                : new ServiceResult(Const.FAIL_DELETE_CODE, "Xoá mềm thất bại.");
        }
        public async Task<IServiceResult> HardDeleteAsync(Guid batchId, Guid userId)
        {
            try
            {
                var farmer = await _unitOfWork.FarmerRepository.FindByUserIdAsync(userId);
                if (farmer == null)
                {
                    return new ServiceResult(Const.FAIL_DELETE_CODE, "Chỉ Farmer mới có quyền xóa mẻ sơ chế.");
                }

                var batch = await _unitOfWork.ProcessingBatchRepository.GetByIdAsync(
                    predicate: x => x.BatchId == batchId,
                    asNoTracking: false
                );

                if (batch == null)
                {
                    return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không tìm thấy mẻ sơ chế.");
                }

                if (batch.FarmerId != farmer.FarmerId)
                {
                    return new ServiceResult(Const.FAIL_DELETE_CODE, "Không được xóa mẻ sơ chế của người khác.");
                }

                await _unitOfWork.ProcessingBatchRepository.RemoveAsync(batch);
                var result = await _unitOfWork.SaveChangesAsync();

                if (result > 0)
                {
                    return new ServiceResult(Const.SUCCESS_DELETE_CODE, "Đã xóa vĩnh viễn mẻ sơ chế.");
                }

                return new ServiceResult(Const.FAIL_DELETE_CODE, Const.FAIL_DELETE_MSG);
            }
            catch (Exception ex)
            {
                return new ServiceResult(Const.ERROR_EXCEPTION, ex.Message);
            }
        }
        public async Task<IServiceResult> GetByIdAsync(Guid id, Guid userId, bool isAdmin)
        {
            var batch = await _unitOfWork.ProcessingBatchRepository.GetByIdAsync(
                x => x.BatchId == id && !x.IsDeleted,
                include: q => q
                    .Include(x => x.CropSeason)
                    .Include(x => x.Method)
                    .Include(x => x.Farmer).ThenInclude(f => f.User),
                asNoTracking: true
            );

            if (batch == null)
                return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không tìm thấy lô sơ chế.");

            // Check quyền
            if (!isAdmin && batch.Farmer?.UserId != userId)
                return new ServiceResult(Const.FAIL_UPDATE_CODE, "Bạn không có quyền truy cập lô sơ chế này.");

            var dto = batch.MapToDetailsDto(); // map chi tiết
            return new ServiceResult(Const.SUCCESS_READ_CODE, Const.SUCCESS_READ_MSG, dto);
        }


    }
}