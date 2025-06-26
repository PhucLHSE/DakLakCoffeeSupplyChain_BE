using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.CropSeasonDTOs;
using DakLakCoffeeSupplyChain.Common.Enum.CropSeasonEnums;
using DakLakCoffeeSupplyChain.Common.Enum.CropSeasonEnums;
using DakLakCoffeeSupplyChain.Common.Helpers;
using DakLakCoffeeSupplyChain.Repositories.Models;
using DakLakCoffeeSupplyChain.Repositories.UnitOfWork;
using DakLakCoffeeSupplyChain.Services.Base;
using DakLakCoffeeSupplyChain.Services.Generators;
using DakLakCoffeeSupplyChain.Services.IServices;
using DakLakCoffeeSupplyChain.Services.Mappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Services.Services
{
    public class CropSeasonService : ICropSeasonService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICodeGenerator _codeCropSeasonGenerator;

        public CropSeasonService(IUnitOfWork unitOfWork, ICodeGenerator cropSeasonCodeGenerator)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _codeCropSeasonGenerator = cropSeasonCodeGenerator ?? throw new ArgumentNullException(nameof(cropSeasonCodeGenerator));
        }

        public async Task<IServiceResult> GetAll()
        {
            var cropSeasons = await _unitOfWork.CropSeasonRepository.GetAllCropSeasonsAsync();

            if (cropSeasons == null || !cropSeasons.Any())
            {
                return new ServiceResult(Const.WARNING_NO_DATA_CODE, Const.WARNING_NO_DATA_MSG, new List<CropSeasonViewAllDto>());
            }

            var dtoList = cropSeasons.Select(cs => cs.MapToCropSeasonViewAllDto()).ToList();

            return new ServiceResult(Const.SUCCESS_READ_CODE, Const.SUCCESS_READ_MSG, dtoList);
        }

        public async Task<IServiceResult> GetById(Guid cropSeasonId)
        {
            var cropSeason = await _unitOfWork.CropSeasonRepository.GetWithDetailsByIdAsync(cropSeasonId);

            if (cropSeason == null)
                return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không tìm thấy mùa vụ.");

            var dto = cropSeason.MapToCropSeasonViewDetailsDto();
            return new ServiceResult(Const.SUCCESS_READ_CODE, Const.SUCCESS_READ_MSG, dto);
        }

        public async Task<IServiceResult> Create(CropSeasonCreateDto dto)
        {
            try
            {
                // Kiểm tra điều kiện nhập liệu
                var validationResult = await ValidateCropSeasonCreate(dto);
                if (validationResult != null)
                    return validationResult;

                var farmer = await _unitOfWork.FarmerRepository.GetByIdAsync(dto.FarmerId);
                var registration = await _unitOfWork.CultivationRegistrationRepository.GetByIdAsync(dto.RegistrationId);

                // Tạo mã mùa vụ
                string code = await _codeCropSeasonGenerator.GenerateCropSeasonCodeAsync(dto.StartDate.Year);
                var entity = dto.MapToCropSeasonCreateDto(code);
                double totalArea = dto.Details.Sum(d => d.AreaAllocated ?? 0);
                entity.Area = totalArea;

                // Ghi database
                await _unitOfWork.CropSeasonRepository.CreateAsync(entity);
                int result = await _unitOfWork.SaveChangesAsync();

                if (result > 0)
                {
                    var responseDto = entity.MapToCropSeasonViewDetailsDto();
                    responseDto.FarmerName = farmer.User?.Name ?? "UnKnown";
                    return new ServiceResult(Const.SUCCESS_CREATE_CODE, Const.SUCCESS_CREATE_MSG, responseDto);
                }

                return new ServiceResult(Const.FAIL_CREATE_CODE, Const.FAIL_CREATE_MSG);
            }
            catch (Exception ex)
            {
                return new ServiceResult(Const.ERROR_EXCEPTION, "Đã xảy ra lỗi nội bộ."); // Có thể log ex nếu có _logger
            }
        }

        private async Task<IServiceResult?> ValidateCropSeasonCreate(CropSeasonCreateDto dto)
        {
            if (dto.Details == null || !dto.Details.Any())
                return new ServiceResult(Const.FAIL_CREATE_CODE, "Phải có ít nhất 1 dòng cà phê.");

            if (dto.StartDate >= dto.EndDate)
                return new ServiceResult(Const.FAIL_CREATE_CODE, "Ngày bắt đầu phải trước ngày kết thúc.");

            foreach (var detail in dto.Details)
            {
                if (detail.ExpectedHarvestStart >= detail.ExpectedHarvestEnd)
                    return new ServiceResult(Const.FAIL_CREATE_CODE, "Ngày thu hoạch không hợp lệ.");
            }

            var farmer = await _unitOfWork.FarmerRepository.GetByIdAsync(dto.FarmerId);
            if (farmer == null)
                return new ServiceResult(Const.FAIL_CREATE_CODE, "Nông hộ không tồn tại.");

            var registration = await _unitOfWork.CultivationRegistrationRepository.GetByIdAsync(dto.RegistrationId);
            if (registration == null)
                return new ServiceResult(Const.FAIL_CREATE_CODE, "Đăng ký canh tác không tồn tại.");

            var commitment = await _unitOfWork.FarmingCommitmentRepository.GetByIdAsync(dto.CommitmentId);
            if (commitment == null)
                return new ServiceResult(Const.FAIL_CREATE_CODE, "Cam kết canh tác không tồn tại.");

            bool isDuplicate = await _unitOfWork.CropSeasonRepository.ExistsAsync(
                x => x.RegistrationId == dto.RegistrationId &&
                     x.StartDate.HasValue &&
                     x.StartDate.Value.Year == dto.StartDate.Year
            );
            if (isDuplicate)
                return new ServiceResult(Const.FAIL_CREATE_CODE,
                    $"Đăng ký {registration.RegistrationCode} đã có mùa vụ trong năm {dto.StartDate.Year}.");

            return null;
        }


        public async Task<IServiceResult> Update(CropSeasonUpdateDto dto)
        {
            var cropSeason = await _unitOfWork.CropSeasonRepository.GetWithDetailsByIdAsync(dto.CropSeasonId);
            if (cropSeason == null)
                return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không tìm thấy mùa vụ.");

            var farmer = await _unitOfWork.FarmerRepository.GetByIdAsync(dto.FarmerId);
            if (farmer == null)
                return new ServiceResult(Const.FAIL_UPDATE_CODE, "Nông hộ không tồn tại.");

            var registration = await _unitOfWork.CultivationRegistrationRepository.GetByIdAsync(dto.RegistrationId);
            if (registration == null)
                return new ServiceResult(Const.FAIL_UPDATE_CODE, "Đăng ký canh tác không tồn tại.");

            var commitment = await _unitOfWork.FarmingCommitmentRepository.GetByIdAsync(dto.CommitmentId);
            if (commitment == null)
                return new ServiceResult(Const.FAIL_UPDATE_CODE, "Cam kết canh tác không tồn tại.");

            if (dto.StartDate >= dto.EndDate)
                return new ServiceResult(Const.FAIL_UPDATE_CODE, "Ngày bắt đầu phải trước ngày kết thúc.");

            // Kiểm tra trùng mùa vụ theo năm và đăng ký, trừ chính bản ghi đang cập nhật
            bool isDuplicate = await _unitOfWork.CropSeasonRepository.ExistsAsync(
           x => x.RegistrationId == dto.RegistrationId &&
                x.StartDate.HasValue &&
                x.StartDate.Value.Year == dto.StartDate.Year &&
                x.CropSeasonId != dto.CropSeasonId
       );

            if (isDuplicate)
                return new ServiceResult(Const.FAIL_UPDATE_CODE, "Đã tồn tại mùa vụ khác cho đăng ký canh tác trong năm này.");

            dto.MapToExistingEntity(cropSeason);
            cropSeason.CropSeasonDetails.Clear();
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
                Status = CropSeasonStatus.Active.ToString()
            }).ToList();

            await _unitOfWork.SaveChangesAsync();
            return new ServiceResult(Const.SUCCESS_UPDATE_CODE, "Cập nhật thành công");
        }

        public async Task<IServiceResult> DeleteById(Guid cropSeasonId)
        {
            var existing = await _unitOfWork.CropSeasonRepository.GetByIdAsync(cropSeasonId);
            if (existing == null)
                return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không tìm thấy mùa vụ.");

            await _unitOfWork.CropSeasonRepository.DeleteCropSeasonDetailsBySeasonIdAsync(cropSeasonId);
            _unitOfWork.CropSeasonRepository.PrepareRemove(existing);
            await _unitOfWork.SaveChangesAsync();

            return new ServiceResult(Const.SUCCESS_DELETE_CODE, "Xóa mùa vụ thành công.");
        }

        public async Task<IServiceResult> SoftDeleteAsync(Guid cropSeasonId)
        {
            try
            {
                var existing = await _unitOfWork.CropSeasonRepository.GetByIdAsync(cropSeasonId);

                if (existing == null || existing.IsDeleted)
                {
                    return new ServiceResult(
                        Const.WARNING_NO_DATA_CODE,
                        "Không tìm thấy hoặc mùa vụ đã bị xoá."
                    );
                }

                // Đánh dấu xoá mềm
                existing.IsDeleted = true;
                existing.UpdatedAt = DateHelper.NowVietnamTime();

                await _unitOfWork.CropSeasonRepository.UpdateAsync(existing);

                var result = await _unitOfWork.SaveChangesAsync();

                if (result > 0)
                {
                    return new ServiceResult(
                        Const.SUCCESS_DELETE_CODE,
                        "Xoá mềm mùa vụ thành công."
                    );
                }
                else
                {
                    return new ServiceResult(
                        Const.FAIL_DELETE_CODE,
                        "Xoá mềm mùa vụ thất bại."
                    );
                }
            }
            catch (Exception ex)
            {
                return new ServiceResult(
                    Const.ERROR_EXCEPTION,
                    $"Lỗi hệ thống khi xoá mềm mùa vụ: {ex.Message}"
                );
            }
        }


    }
}
