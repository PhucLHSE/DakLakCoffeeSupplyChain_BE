using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.ProcessingMethodStageDTOs;
using DakLakCoffeeSupplyChain.Common.Helpers;
using DakLakCoffeeSupplyChain.Repositories.UnitOfWork;
using DakLakCoffeeSupplyChain.Services.Base;
using DakLakCoffeeSupplyChain.Services.IServices;
using DakLakCoffeeSupplyChain.Services.Mappers;
using Microsoft.EntityFrameworkCore;
namespace DakLakCoffeeSupplyChain.Services.Services
{
    public class ProcessingStageService : IProcessingStageService
    {
        private readonly IUnitOfWork _unitOfWork;

        public ProcessingStageService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        private ServiceResult CreateValidationError(string errorKey, Dictionary<string, object> parameters = null)
        {
            return new ServiceResult(Const.ERROR_VALIDATION_CODE, errorKey, parameters);
        }

        public async Task<IServiceResult> GetAll()
        {
            var stages = await _unitOfWork.ProcessingStageRepository.GetAllStagesAsync();

            if (stages == null || !stages.Any())
            {
                return new ServiceResult(
                    Const.WARNING_NO_DATA_CODE,
                    Const.WARNING_NO_DATA_MSG,
                    new List<ProcessingStageViewAllDto>()
                );
            }

            var result = stages
                .Select(stage => stage.MapToProcessingStageViewAllDto())
                .ToList();

            return new ServiceResult(
                Const.SUCCESS_READ_CODE,
                Const.SUCCESS_READ_MSG,
                result
            );
        }
        public async Task<IServiceResult> GetDetailByIdAsync(int stageId)
        {
            var stage = await _unitOfWork.ProcessingStageRepository
                .GetAllQueryable()
                .Include(x => x.Method)
                .FirstOrDefaultAsync(x => x.StageId == stageId && !x.IsDeleted);

            if (stage == null)
            {
                var parameters = new Dictionary<string, object>
                {
                    ["stageId"] = stageId
                };
                return CreateValidationError("ProcessingStageNotFound", parameters);
            }

            var dto = stage.MapToProcessingStageViewDetailDto();

            return new ServiceResult(
                Const.SUCCESS_READ_CODE,
                Const.SUCCESS_READ_MSG,
                dto
            );
        }

        public async Task<IServiceResult> GetByMethodIdAsync(int methodId)
        {
            Console.WriteLine($"🔍 DEBUG: GetByMethodIdAsync called with methodId: {methodId}");
            
            var stages = await _unitOfWork.ProcessingStageRepository
                .GetAllQueryable()
                .Include(x => x.Method)
                .Where(x => x.MethodId == methodId && !x.IsDeleted)
                .OrderBy(x => x.OrderIndex)
                .ToListAsync();

            Console.WriteLine($"🔍 DEBUG: Found {stages?.Count ?? 0} stages in database");
            if (stages != null && stages.Any())
            {
                Console.WriteLine($"🔍 DEBUG: First stage: {stages.First().StageName}");
            }

            if (stages == null || !stages.Any())
            {
                Console.WriteLine($"🔍 DEBUG: No stages found, returning WARNING_NO_DATA_CODE");
                return new ServiceResult(
                    Const.WARNING_NO_DATA_CODE,
                    Const.WARNING_NO_DATA_MSG,
                    new List<ProcessingStageViewAllDto>()
                );
            }

            var result = stages
                .Select(stage => stage.MapToProcessingStageViewAllDto())
                .ToList();

            Console.WriteLine($"🔍 DEBUG: Mapped {result.Count} stages to DTOs");

            return new ServiceResult(
                Const.SUCCESS_READ_CODE,
                Const.SUCCESS_READ_MSG,
                result
            );
        }
        public async Task<IServiceResult> CreateAsync(CreateProcessingStageDto dto)
        {
            try
            {
                var entity = dto.MapToProcessingStageCreateEntity(); // map DTO -> Entity
                await _unitOfWork.ProcessingStageRepository.CreateAsync(entity);
                var result = await _unitOfWork.SaveChangesAsync();

                if (result > 0)
                {
                    
                    var created = await _unitOfWork.ProcessingStageRepository.GetByIdAsync (entity.StageId);

                    if (created == null)
                        return new ServiceResult(Const.SUCCESS_CREATE_CODE, Const.SUCCESS_CREATE_MSG, null);

                    var viewDto = created.MapToProcessingStageViewDetailDto();
                    return new ServiceResult(Const.SUCCESS_CREATE_CODE, Const.SUCCESS_CREATE_MSG, viewDto);
                }

                return CreateValidationError("CreateProcessingStageFailed");
            }
            catch (Exception ex)
            {
                return new ServiceResult(Const.ERROR_EXCEPTION, ex.Message);
            }
        }
        public async Task<IServiceResult> DeleteAsync(int stageId)
        {
            var deleted = await _unitOfWork.ProcessingStageRepository.SoftDeleteAsync(stageId);
            if (!deleted)
            {
                var parameters = new Dictionary<string, object>
                {
                    ["stageId"] = stageId
                };
                return CreateValidationError("ProcessingStageNotFoundOrDeleted", parameters);
            }

            var result = await _unitOfWork.SaveChangesAsync();
            return result > 0
                ? new ServiceResult(Const.SUCCESS_DELETE_CODE, Const.SUCCESS_DELETE_MSG)
                : CreateValidationError("DeleteProcessingStageFailed");
        }
        public async Task<IServiceResult> UpdateAsync(ProcessingStageUpdateDto dto)
        {
            // Lấy thực thể từ database
            var entity = await _unitOfWork.ProcessingStageRepository.GetByIdAsync(dto.StageId);
            if (entity == null)
            {
                var parameters = new Dictionary<string, object>
                {
                    ["stageId"] = dto.StageId
                };
                return CreateValidationError("ProcessingStageNotFoundForUpdate", parameters);
            }

            // Ánh xạ giá trị mới từ dto sang entity
            ProcessingStageMapper.MapToProcessingStageUpdateEntity(entity, dto);

            // Gọi update và lưu
            var updated = await _unitOfWork.ProcessingStageRepository.UpdateAsync(entity);
            if (!updated)
                return CreateValidationError("UpdateProcessingStageFailed");

            var result = await _unitOfWork.SaveChangesAsync();
            return result > 0
                ? new ServiceResult(Const.SUCCESS_UPDATE_CODE, Const.SUCCESS_UPDATE_MSG)
                : CreateValidationError("SaveProcessingStageUpdateFailed");
        }

    }
}
