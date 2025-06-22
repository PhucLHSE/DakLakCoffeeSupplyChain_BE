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
                return new ServiceResult(
                    Const.WARNING_NO_DATA_CODE,
                    $"Processing stage with ID {stageId} not found.",
                    null
                );
            }

            var dto = stage.MapToProcessingStageViewDetailDto();

            return new ServiceResult(
                Const.SUCCESS_READ_CODE,
                Const.SUCCESS_READ_MSG,
                dto
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

                return new ServiceResult(Const.FAIL_CREATE_CODE, Const.FAIL_CREATE_MSG);
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
                return new ServiceResult(Const.WARNING_NO_DATA_CODE, $"StageId {stageId} không tồn tại hoặc đã bị xóa.");
            }

            var result = await _unitOfWork.SaveChangesAsync();
            return result > 0
                ? new ServiceResult(Const.SUCCESS_DELETE_CODE, Const.SUCCESS_DELETE_MSG)
                : new ServiceResult(Const.FAIL_DELETE_CODE, Const.FAIL_DELETE_MSG);
        }
        public async Task<IServiceResult> UpdateAsync(ProcessingStageUpdateDto dto)
        {
            // Lấy thực thể từ database
            var entity = await _unitOfWork.ProcessingStageRepository.GetByIdAsync(dto.StageId);
            if (entity == null)
                return new ServiceResult(Const.WARNING_NO_DATA_CODE, $"Không tìm thấy stage ID {dto.StageId}");

            // Ánh xạ giá trị mới từ dto sang entity
            ProcessingStageMapper.MapToProcessingStageUpdateEntity(entity, dto);

            // Gọi update và lưu
            var updated = await _unitOfWork.ProcessingStageRepository.UpdateAsync(entity);
            if (!updated)
                return new ServiceResult(Const.FAIL_UPDATE_CODE, "Cập nhật thất bại");

            var result = await _unitOfWork.SaveChangesAsync();
            return result > 0
                ? new ServiceResult(Const.SUCCESS_UPDATE_CODE, Const.SUCCESS_UPDATE_MSG)
                : new ServiceResult(Const.FAIL_UPDATE_CODE, Const.FAIL_UPDATE_MSG);
        }

    }
}
