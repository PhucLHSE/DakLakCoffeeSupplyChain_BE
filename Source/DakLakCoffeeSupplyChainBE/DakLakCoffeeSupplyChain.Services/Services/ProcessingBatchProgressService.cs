using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.ProcessingBatchsProgressDTOs;
using DakLakCoffeeSupplyChain.Repositories.Models;
using DakLakCoffeeSupplyChain.Repositories.UnitOfWork;
using DakLakCoffeeSupplyChain.Services.Base;
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
    public class ProcessingBatchProgressService : IProcessingBatchProgressService
    {
        private readonly IUnitOfWork _unitOfWork;

        public ProcessingBatchProgressService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }


        public async Task<IServiceResult> GetAllAsync()
        {
            var progresses = await _unitOfWork.ProcessingBatchProgressRepository.GetAllWithIncludesAsync();

            if (progresses == null || !progresses.Any())
            {
                return new ServiceResult(
                    Const.WARNING_NO_DATA_CODE,
                    Const.WARNING_NO_DATA_MSG,
                    new List<ProcessingBatchProgressViewAllDto>()
                );
            }

            var dtoList = progresses.Select(p => p.MapToProcessingBatchProgressViewAllDto()).ToList();

            return new ServiceResult(
                Const.SUCCESS_READ_CODE,
                Const.SUCCESS_READ_MSG,
                dtoList
            );
        }
        public async Task<IServiceResult> GetByIdAsync(Guid progressId)
        {
            var entity = await _unitOfWork.ProcessingBatchProgressRepository
                .GetByIdAsync(progressId); 

            if (entity == null)
                return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không tìm thấy tiến trình xử lý", null);

            var dto = entity.MapToProcessingBatchProgressDetailDto();

            return new ServiceResult(Const.SUCCESS_READ_CODE, "Thành công", dto);
        }
        public async Task<IServiceResult> CreateAsync(ProcessingBatchProgressCreateDto input)
        {
            try
            {
                
                var batch = await _unitOfWork.ProcessingBatchRepository.GetByIdAsync(input.BatchId);
                if (batch == null || batch.IsDeleted)
                {
                    return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Batch không tồn tại.");
                }

                // 2. Kiểm tra StepIndex trùng trong cùng Batch
                var existingStep = await _unitOfWork.ProcessingBatchProgressRepository.GetByIdAsync(
                    predicate: p => p.BatchId == input.BatchId && p.StepIndex == input.StepIndex && !p.IsDeleted,
                    asNoTracking: true
                );
                if (existingStep != null)
                {
                    return new ServiceResult(Const.FAIL_CREATE_CODE, $"StepIndex {input.StepIndex} đã tồn tại trong Batch.");
                }

                // 3. Lấy danh sách Parameters theo Stage
                var parameters = await _unitOfWork.ProcessingParameterRepository.GetAllAsync(
                    predicate: p => p.Progress.StageId == input.StageId && !p.IsDeleted,
                    include: q => q.Include(p => p.Progress));

                // 4. Tạo mới progress
                var progress = new ProcessingBatchProgress
                {
                    ProgressId = Guid.NewGuid(),
                    BatchId = input.BatchId,
                    StepIndex = input.StepIndex,
                    StageId = input.StageId,
                    StageDescription = "", 
                    ProgressDate = input.ProgressDate,
                    OutputQuantity = input.OutputQuantity,
                    OutputUnit = string.IsNullOrWhiteSpace(input.OutputUnit) ? "kg" : input.OutputUnit,
                    PhotoUrl = input.PhotoUrl,
                    VideoUrl = input.VideoUrl,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    UpdatedBy = batch.FarmerId, 
                    IsDeleted = false,
                    ProcessingParameters = parameters?.Select(p => new ProcessingParameter
                    {
                        ParameterId = Guid.NewGuid(),
                        ParameterName = p.ParameterName,
                        Unit = p.Unit,
                        ParameterValue = null,
                        RecordedAt = null,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        IsDeleted = false
                    }).ToList()
                };

                await _unitOfWork.ProcessingBatchProgressRepository.CreateAsync(progress);
                var result = await _unitOfWork.SaveChangesAsync();

                return result > 0
                    ? new ServiceResult(Const.SUCCESS_CREATE_CODE, Const.SUCCESS_CREATE_MSG)
                    : new ServiceResult(Const.FAIL_CREATE_CODE, Const.FAIL_CREATE_MSG);
            }
            catch (Exception ex)
            {
                return new ServiceResult(Const.ERROR_EXCEPTION, ex.Message);
            }
        }
        public async Task<IServiceResult> UpdateAsync(Guid progressId, ProcessingBatchProgressUpdateDto dto)
        {
            // [Step 1] Lấy entity từ DB
            var entity = await _unitOfWork.ProcessingBatchProgressRepository.GetByIdAsync(
                p => p.ProgressId == progressId && !p.IsDeleted
            );

            if (entity == null)
                return new ServiceResult(Const.WARNING_NO_DATA_CODE, $"[Step 1] Không tìm thấy tiến độ với ID = {progressId}");

            // [Step 2] Kiểm tra StepIndex trùng (nếu thay đổi)
            if (dto.StepIndex != entity.StepIndex)
            {
                var isDuplicated = await _unitOfWork.ProcessingBatchProgressRepository.AnyAsync(
                    p => p.BatchId == entity.BatchId &&
                         p.StepIndex == dto.StepIndex &&
                         p.ProgressId != progressId &&
                         !p.IsDeleted
                );

                if (isDuplicated)
                    return new ServiceResult(Const.FAIL_UPDATE_CODE, $"[Step 2] StepIndex {dto.StepIndex} đã tồn tại trong Batch.");
            }

            // [Step 3] So sánh và cập nhật nếu có thay đổi
            bool isModified = false;

            if (entity.StepIndex != dto.StepIndex)
            {
                entity.StepIndex = dto.StepIndex;
                isModified = true;
            }

            if (entity.OutputQuantity != dto.OutputQuantity)
            {
                entity.OutputQuantity = dto.OutputQuantity;
                isModified = true;
            }

            if (!string.Equals(entity.OutputUnit, dto.OutputUnit, StringComparison.OrdinalIgnoreCase))
            {
                entity.OutputUnit = dto.OutputUnit;
                isModified = true;
            }

            if (entity.PhotoUrl != dto.PhotoUrl)
            {
                entity.PhotoUrl = dto.PhotoUrl;
                isModified = true;
            }

            if (entity.VideoUrl != dto.VideoUrl)
            {
                entity.VideoUrl = dto.VideoUrl;
                isModified = true;
            }

            var dtoDateOnly = DateOnly.FromDateTime(dto.ProgressDate);
            if (entity.ProgressDate != dtoDateOnly)
            {
                entity.ProgressDate = dtoDateOnly;
                isModified = true;
            }

            if (!isModified)
            {
                return new ServiceResult(Const.FAIL_UPDATE_CODE, "[Step 4] Dữ liệu truyền vào không có gì khác biệt.");
            }

            entity.UpdatedAt = DateTime.UtcNow;

            // [Step 4] Gọi UpdateAsync và kiểm tra trả về bool
            var updated = await _unitOfWork.ProcessingBatchProgressRepository.UpdateAsync(entity);
            if (!updated)
                return new ServiceResult(Const.FAIL_UPDATE_CODE, "[Step 5] UpdateAsync() trả về false.");
            var result = await _unitOfWork.SaveChangesAsync();

            if (result > 0)
            {
                var resultDto = entity.MapToProcessingBatchProgressDetailDto();

                return new ServiceResult(Const.SUCCESS_UPDATE_CODE, "[Step 6] Cập nhật thành công.", dto);
            }
            else
            {
                return new ServiceResult(Const.FAIL_UPDATE_CODE, "[Step 6] Không có thay đổi nào được lưu.");
            }
        }
    }
}
