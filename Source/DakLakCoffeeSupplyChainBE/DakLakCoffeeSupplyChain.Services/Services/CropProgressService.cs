using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.CropProgressDTOs;
using DakLakCoffeeSupplyChain.Repositories.Models;
using DakLakCoffeeSupplyChain.Repositories.UnitOfWork;
using DakLakCoffeeSupplyChain.Services.Base;
using DakLakCoffeeSupplyChain.Services.IServices;
using DakLakCoffeeSupplyChain.Services.Mappers;
using Microsoft.EntityFrameworkCore;

namespace DakLakCoffeeSupplyChain.Services.Services
{
    public class CropProgressService : ICropProgressService
    {
        private readonly IUnitOfWork _unitOfWork;

        public CropProgressService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IServiceResult> GetAll()
        {
            var progresses = await _unitOfWork.CropProgressRepository.GetAllWithIncludesAsync();

            if (progresses == null || !progresses.Any())
                return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không có tiến trình nào.", new List<CropProgressViewAllDto>());

            var dtoList = progresses.Select(p => p.ToViewAllDto()).ToList();
            return new ServiceResult(Const.SUCCESS_READ_CODE, Const.SUCCESS_READ_MSG, dtoList);
        }

        public async Task<IServiceResult> GetById(Guid id)
        {
            var progress = await _unitOfWork.CropProgressRepository.GetByIdWithIncludesAsync(id);

            if (progress == null)
                return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không tìm thấy tiến trình.");

            return new ServiceResult(Const.SUCCESS_READ_CODE, Const.SUCCESS_READ_MSG, progress.ToViewDetailsDto());
        }
    }
}