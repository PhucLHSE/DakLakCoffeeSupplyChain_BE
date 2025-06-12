using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.CropSeasonDTOs;
using DakLakCoffeeSupplyChain.Repositories.Models;
using DakLakCoffeeSupplyChain.Repositories.UnitOfWork;
using DakLakCoffeeSupplyChain.Services.Base;
using DakLakCoffeeSupplyChain.Services.Generators;
using DakLakCoffeeSupplyChain.Services.IServices;
using DakLakCoffeeSupplyChain.Services.Mappers;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.InteropServices.Marshalling;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Services.Services
{
    public class CropSeasonService : ICropSeasonService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICropSeasonCodeGenerator _codeCropSeasonGenerator;


        public CropSeasonService(IUnitOfWork unitOfWork, ICropSeasonCodeGenerator cropSeasonCodeGenerator)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _codeCropSeasonGenerator = cropSeasonCodeGenerator ?? throw new ArgumentNullException(nameof(cropSeasonCodeGenerator));

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
        public async Task<IServiceResult> Create(CropSeasonCreateDto dto)
        {
            try
            {

                if (dto.Details == null || !dto.Details.Any())
                    return new ServiceResult(Const.FAIL_CREATE_CODE, "Phải có ít nhất 1 dòng cà phê.");

                var farmer = await _unitOfWork.FarmerRepository.GetByIdAsync(dto.FarmerId);
                if (farmer == null)
                    return new ServiceResult(Const.FAIL_CREATE_CODE, "Nông hộ không tồn tại.");

                var registration = await _unitOfWork.CultivationRegistrationRepository.GetByIdAsync(dto.RegistrationId);
                if (registration == null)
                    return new ServiceResult(Const.FAIL_CREATE_CODE, "Đăng ký canh tác không tồn tại.");

                var commitment = await _unitOfWork.FarmingCommitmentRepository.GetByIdAsync(dto.CommitmentId);
                if (commitment == null)
                    return new ServiceResult(Const.FAIL_CREATE_CODE, "Cam kết canh tác không tồn tại.");

                if (dto.StartDate >= dto.EndDate)
                    return new ServiceResult(Const.FAIL_CREATE_CODE, "Ngày bắt đầu phải trước ngày kết thúc.");


                string code = await _codeCropSeasonGenerator.GenerateCropSeasonCodeAsync(dto.StartDate.Year);


                var entity = dto.MapToCropSeasonCreateDto(code);

                await _unitOfWork.CropSeasonRepository.CreateAsync(entity);
                int result = await _unitOfWork.SaveChangesAsync();

                if (result > 0)
                {
                    var responseDto = entity.MapToCropSeasonViewDetailsDto();
                    responseDto.FarmerName = farmer.User?.Name ?? "UnKnown";

                    return new ServiceResult(
                        Const.SUCCESS_CREATE_CODE,
                        Const.SUCCESS_CREATE_MSG,
                        responseDto
                    );
                }

                return new ServiceResult(Const.FAIL_CREATE_CODE, Const.FAIL_CREATE_MSG);
            }
            catch (Exception ex)
            {
                return new ServiceResult(Const.ERROR_EXCEPTION, ex.ToString());
            }
        }
        public async Task<IServiceResult> Update(CropSeasonUpdateDto dto)
        {
            var cropSeason = await _unitOfWork.CropSeasonRepository
                .GetWithDetailsByIdAsync(dto.CropSeasonId);

            if (cropSeason == null)
                return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không tìm thấy mùa vụ.");

            var farmer = await _unitOfWork.FarmerRepository.GetByIdAsync(dto.FarmerId);
            if (farmer == null) return new ServiceResult(Const.FAIL_UPDATE_CODE, "Nông hộ không tồn tại.");

            var registration = await _unitOfWork.CultivationRegistrationRepository.GetByIdAsync(dto.RegistrationId);
            if (registration == null) return new ServiceResult(Const.FAIL_UPDATE_CODE, "Đăng ký canh tác không tồn tại.");

            var commitment = await _unitOfWork.FarmingCommitmentRepository.GetByIdAsync(dto.CommitmentId);
            if (commitment == null) return new ServiceResult(Const.FAIL_UPDATE_CODE, "Cam kết canh tác không tồn tại.");

            if (dto.StartDate >= dto.EndDate)
                return new ServiceResult(Const.FAIL_UPDATE_CODE, "Ngày bắt đầu phải trước ngày kết thúc.");

            // Cập nhật mùa vụ
            dto.MapToExistingEntity(cropSeason);

            // Xoá detail cũ
            cropSeason.CropSeasonDetails.Clear();

            // Thêm lại detail mới
            cropSeason.CropSeasonDetails = dto.Details.Select(detail => new CropSeasonDetail
            {
                DetailId = Guid.NewGuid(),
                CropSeasonId = cropSeason.CropSeasonId,
                CoffeeTypeId = detail.CoffeeTypeId,
                ExpectedHarvestStart = detail.ExpectedHarvestStart,
                ExpectedHarvestEnd = detail.ExpectedHarvestEnd,
                EstimatedYield = detail.EstimatedYield,
                AreaAllocated = detail.AreaAllocated,
                PlannedQuality = detail.PlannedQuality,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                Status = "Planned"
            }).ToList();

            await _unitOfWork.SaveChangesAsync();

            return new ServiceResult(Const.SUCCESS_UPDATE_CODE, "Cập nhật thành công");
        }

        public async Task<IServiceResult> DeleteById(Guid cropSeasonId)
        {
            var existing = await _unitOfWork.CropSeasonRepository.GetByIdAsync(cropSeasonId);
            if (existing == null)
                return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không tìm thấy mùa vụ.");

            // Xóa chi tiết trước
            await _unitOfWork.CropSeasonRepository.DeleteCropSeasonDetailsBySeasonIdAsync(cropSeasonId);

            // Xóa mùa vụ
            _unitOfWork.CropSeasonRepository.PrepareRemove(existing);

            await _unitOfWork.SaveChangesAsync();

            return new ServiceResult(Const.SUCCESS_DELETE_CODE, "Xóa mùa vụ thành công.");
        }

    }
}