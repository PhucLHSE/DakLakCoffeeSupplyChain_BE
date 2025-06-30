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
                // 1. Kiểm tra Batch tồn tại
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
    include: q => q.Include(p => p.Progress)
);

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

    }
}
