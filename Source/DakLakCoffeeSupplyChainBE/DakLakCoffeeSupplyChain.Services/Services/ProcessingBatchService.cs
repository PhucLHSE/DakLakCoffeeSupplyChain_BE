using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.ProcessingBatchDTOs;
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
        public async Task<IServiceResult> GetAllByUserId(Guid userId, bool isAdmin = false)
        {
            List<ProcessingBatch> batches;

            if (isAdmin)
            {
                // Admin xem tất cả
                batches = await _unitOfWork.ProcessingBatchRepository
                    .GetQueryable()
                    .Include(pb => pb.CropSeason)
                    .Include(pb => pb.Farmer).ThenInclude(f => f.User)
                    .Include(pb => pb.Method)
                    .Include(pb => pb.ProcessingBatchProgresses)
                    .Where(pb => !pb.IsDeleted)
                    .ToListAsync();
            }
            else
            {
                // Farmer chỉ xem batch của chính họ
                var farmer = await _unitOfWork.FarmerRepository.FindByUserIdAsync(userId);
                if (farmer == null)
                {
                    return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không tìm thấy Farmer tương ứng.", new List<ProcessingBatchViewDto>());
                }

                batches = await _unitOfWork.ProcessingBatchRepository
                    .GetQueryable()
                    .Include(pb => pb.CropSeason)
                    .Include(pb => pb.Farmer).ThenInclude(f => f.User)
                    .Include(pb => pb.Method)
                    .Include(pb => pb.ProcessingBatchProgresses)
                    .Where(pb => pb.FarmerId == farmer.FarmerId && !pb.IsDeleted)
                    .ToListAsync();
            }

            if (!batches.Any())
            {
                return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không có dữ liệu ProcessingBatch.", new List<ProcessingBatchViewDto>());
            }

            var dtoList = batches.Select(b => b.MapToProcessingBatchViewDto()).ToList();
            return new ServiceResult(Const.SUCCESS_READ_CODE, Const.SUCCESS_READ_MSG, dtoList);
        }
        public async Task<IServiceResult> CreateAsync(ProcessingBatchCreateDto dto, Guid userId)
        {
            try
            {
                var farmer = await _unitOfWork.FarmerRepository.FindByUserIdAsync(userId);
                if (farmer == null)
                    return new ServiceResult(Const.FAIL_CREATE_CODE, "Chỉ người dùng có vai trò Nông hộ (Farmer) mới được phép tạo mẻ sơ chế.");

                var cropSeasonExists = await _unitOfWork.CropSeasonRepository.AnyAsync(
                    x => x.CropSeasonId == dto.CropSeasonId && !x.IsDeleted);
                if (!cropSeasonExists)
                    return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Vụ mùa không tồn tại.");

                var coffeeTypeExists = await _unitOfWork.CoffeeTypeRepository.AnyAsync(
                    x => x.CoffeeTypeId == dto.CoffeeTypeId && !x.IsDeleted);
                if (!coffeeTypeExists)
                    return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Loại cà phê không tồn tại.");

                var methodExists = await _unitOfWork.ProcessingMethodRepository.AnyAsync(
                    x => x.MethodId == dto.MethodId);
                if (!methodExists)
                    return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Phương pháp sơ chế không tồn tại.");

                var systemBatchCode = await _codeGenerator.GenerateProcessingSystemBatchCodeAsync(DateTime.UtcNow.Year);

                var batch = new ProcessingBatch
                {
                    BatchId = Guid.NewGuid(),
                    SystemBatchCode = systemBatchCode,
                    BatchCode = dto.BatchCode?.Trim(),
                    CoffeeTypeId = dto.CoffeeTypeId,
                    CropSeasonId = dto.CropSeasonId,
                    FarmerId = farmer.FarmerId,
                    MethodId = dto.MethodId,
                    InputQuantity = dto.InputQuantity,
                    InputUnit = dto.InputUnit?.Trim(),
                    CreatedAt = DateTime.UtcNow,
                    Status = "pending",
                    IsDeleted = false
                };

                // kiểm tra trùng mã batchCode
                var isDuplicate = await _unitOfWork.ProcessingBatchRepository.AnyAsync(
                    x => x.BatchCode == batch.BatchCode && !x.IsDeleted);
                if (isDuplicate)
                {
                    return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Mã lô đã tồn tại.");
                }

                // thiếu dòng này khiến SaveChangesAsync không có gì để lưu
                await _unitOfWork.ProcessingBatchRepository.CreateAsync(batch);

                var result = await _unitOfWork.SaveChangesAsync();
                if (result <= 0)
                    return new ServiceResult(Const.FAIL_CREATE_CODE, Const.FAIL_CREATE_MSG);

                var createdBatch = await _unitOfWork.ProcessingBatchRepository.GetByIdAsync(
                    x => x.BatchId == batch.BatchId && !x.IsDeleted,
                    include: q => q
                        .Include(b => b.CropSeason)
                        .Include(b => b.Farmer).ThenInclude(f => f.User)
                        .Include(b => b.Method),
                    asNoTracking: true
                );

                if (createdBatch != null)
                {
                    var viewDto = createdBatch.MapToProcessingBatchViewDto();
                    return new ServiceResult(Const.SUCCESS_CREATE_CODE, Const.SUCCESS_CREATE_MSG, viewDto);
                }

                return new ServiceResult(Const.FAIL_CREATE_CODE, "Tạo thành công nhưng không truy xuất được bản ghi.");
            }
            catch (Exception ex)
            {
                return new ServiceResult(Const.ERROR_EXCEPTION, ex.Message);
            }
        }
        public async Task<IServiceResult> UpdateAsync(ProcessingBatchUpdateDto dto, Guid userId)
        {
            try
            {
                var farmer = await _unitOfWork.FarmerRepository.FindByUserIdAsync(userId);
                if (farmer == null)
                    return new ServiceResult(Const.FAIL_UPDATE_CODE, "Chỉ Farmer mới được phép cập nhật mẻ sơ chế.");

                var batch = await _unitOfWork.ProcessingBatchRepository.GetByIdAsync(
                    predicate: x => x.BatchId == dto.BatchId && !x.IsDeleted,
                    asNoTracking: false
                );

                if (batch == null)
                    return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không tìm thấy mẻ sơ chế cần cập nhật.");

                if (batch.FarmerId != farmer.FarmerId)
                    return new ServiceResult(Const.FAIL_UPDATE_CODE, "Không được phép cập nhật mẻ sơ chế của người khác.");

                var cropSeasonExists = await _unitOfWork.CropSeasonRepository.AnyAsync(
                    x => x.CropSeasonId == dto.CropSeasonId && !x.IsDeleted);
                if (!cropSeasonExists)
                    return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Vụ mùa không tồn tại.");

                var coffeeTypeExists = await _unitOfWork.CoffeeTypeRepository.AnyAsync(
                    x => x.CoffeeTypeId == dto.CoffeeTypeId && !x.IsDeleted);
                if (!coffeeTypeExists)
                    return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Loại cà phê không tồn tại.");

                var methodExists = await _unitOfWork.ProcessingMethodRepository.AnyAsync(
                    x => x.MethodId == dto.MethodId);
                if (!methodExists)
                    return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Phương pháp sơ chế không tồn tại.");

                // Cập nhật dữ liệu
                batch.BatchCode = dto.BatchCode?.Trim();
                batch.CoffeeTypeId = dto.CoffeeTypeId;
                batch.CropSeasonId = dto.CropSeasonId;
                batch.MethodId = dto.MethodId;
                batch.InputQuantity = dto.InputQuantity;
                batch.InputUnit = dto.InputUnit?.Trim();
                batch.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.ProcessingBatchRepository.PrepareUpdate(batch);
                var result = await _unitOfWork.SaveChangesAsync();

                if (result <= 0)
                    return new ServiceResult(Const.FAIL_UPDATE_CODE, Const.FAIL_UPDATE_MSG);

                var updated = await _unitOfWork.ProcessingBatchRepository.GetByIdAsync(
                    predicate: x => x.BatchId == dto.BatchId,
                    include: q => q
                        .Include(x => x.CropSeason)
                        .Include(x => x.Farmer).ThenInclude(f => f.User)
                        .Include(x => x.Method),
                    asNoTracking: true
                );

                var viewDto = updated?.MapToProcessingBatchViewDto();
                return new ServiceResult(Const.SUCCESS_UPDATE_CODE, Const.SUCCESS_UPDATE_MSG, viewDto);
            }
            catch (Exception ex)
            {
                return new ServiceResult(Const.ERROR_EXCEPTION, ex.Message);
            }
        }
        public async Task<IServiceResult> SoftDeleteAsync(Guid batchId, Guid userId)
        {
            try
            {
                // Lấy Farmer theo userId
                var farmer = await _unitOfWork.FarmerRepository.FindByUserIdAsync(userId);
                if (farmer == null)
                {
                    return new ServiceResult(Const.FAIL_DELETE_CODE, "Chỉ Farmer mới có quyền xóa mẻ sơ chế.");
                }

                // Tìm mẻ sơ chế cần xóa
                var batch = await _unitOfWork.ProcessingBatchRepository.GetByIdAsync(
                    predicate: x => x.BatchId == batchId && !x.IsDeleted,
                    asNoTracking: false
                );

                if (batch == null)
                {
                    return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không tìm thấy mẻ sơ chế.");
                }

                // Chặn xóa nếu không phải của Farmer hiện tại
                if (batch.FarmerId != farmer.FarmerId)
                {
                    return new ServiceResult(Const.FAIL_DELETE_CODE, "Không được xóa mẻ sơ chế của người khác.");
                }

                batch.IsDeleted = true;
                batch.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.ProcessingBatchRepository.PrepareUpdate(batch);

                var result = await _unitOfWork.SaveChangesAsync();

                if (result > 0)
                {
                    return new ServiceResult(Const.SUCCESS_DELETE_CODE, "Xóa mềm mẻ sơ chế thành công.");
                }

                return new ServiceResult(Const.FAIL_DELETE_CODE, Const.FAIL_DELETE_MSG);
            }
            catch (Exception ex)
            {
                return new ServiceResult(Const.ERROR_EXCEPTION, ex.Message);
            }
        }


    }
}