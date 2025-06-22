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

            var dto = stage.ToDetailDto();

            return new ServiceResult(
                Const.SUCCESS_READ_CODE,
                Const.SUCCESS_READ_MSG,
                dto
            );
        }
    }
}
