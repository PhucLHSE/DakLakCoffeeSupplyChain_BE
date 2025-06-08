using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.CropSeasonDTOs;
using DakLakCoffeeSupplyChain.Repositories.UnitOfWork;
using DakLakCoffeeSupplyChain.Services.Base;
using DakLakCoffeeSupplyChain.Services.IServices;
using DakLakCoffeeSupplyChain.Services.Mappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Services.Services
{
    public class CropSeasonService : ICropSeasonService
    {
        private readonly IUnitOfWork _unitOfWork;

        public CropSeasonService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task<IServiceResult> GetAll()
        {
            var cropSeasons = await _unitOfWork.CropSeasonRepository.GetAllCropSeasonsAsync();

            if (cropSeasons == null || !cropSeasons.Any())
            {
                return new ServiceResult(
                    Const.WARNING_NO_DATA_CODE,
                    Const.WARNING_NO_DATA_MSG,
                    new List<CropSeasonViewAllDto>()
                );
            }

            var dtoList = cropSeasons
                .Select(cs => cs.MapToCropSeasonViewAllDto())
                .ToList();

            return new ServiceResult(
                Const.SUCCESS_READ_CODE,
                Const.SUCCESS_READ_MSG,
                dtoList
            );
        }

        public async Task<IServiceResult> GetById(Guid cropSeasonId)
        {
            var cropSeason = await _unitOfWork.CropSeasonRepository.GetCropSeasonByIdAsync(cropSeasonId);

            if (cropSeason == null)
            {
                return new ServiceResult(
                    Const.WARNING_NO_DATA_CODE,
                    Const.WARNING_NO_DATA_MSG,
                    new CropSeasonViewDetailsDto()
                );
            }

            var dto = cropSeason.MapToCropSeasonViewDetailsDto();

            return new ServiceResult(
                Const.SUCCESS_READ_CODE,
                Const.SUCCESS_READ_MSG,
                dto
            );
        }
    }
}
