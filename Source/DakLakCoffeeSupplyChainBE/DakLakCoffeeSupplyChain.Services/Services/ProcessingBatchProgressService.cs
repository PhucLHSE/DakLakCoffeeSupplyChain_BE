using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.ProcessingBatchsProgressDTOs;
using DakLakCoffeeSupplyChain.Repositories.Models;
using DakLakCoffeeSupplyChain.Repositories.UnitOfWork;
using DakLakCoffeeSupplyChain.Services.Base;
using DakLakCoffeeSupplyChain.Services.IServices;
using DakLakCoffeeSupplyChain.Services.Mappers;
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
    }
}
