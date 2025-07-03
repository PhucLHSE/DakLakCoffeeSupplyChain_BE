using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.CropSeasonDTOs;
using DakLakCoffeeSupplyChain.Common.Helpers;
using DakLakCoffeeSupplyChain.Repositories.Models;
using DakLakCoffeeSupplyChain.Repositories.UnitOfWork;
using DakLakCoffeeSupplyChain.Services.Base;
using DakLakCoffeeSupplyChain.Services.Generators;
using DakLakCoffeeSupplyChain.Services.IServices;
using DakLakCoffeeSupplyChain.Services.Mappers;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Services.Services
{
    public class CropSeasonService : ICropSeasonService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICodeGenerator _codeGenerator;

        public CropSeasonService(IUnitOfWork unitOfWork, ICodeGenerator codeGenerator)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _codeGenerator = codeGenerator ?? throw new ArgumentNullException(nameof(codeGenerator));
        }

        public async Task<IServiceResult> GetAllByUserId(Guid userId, bool isAdmin, bool isManager)
        {
            if (isAdmin || isManager)
            {
                // Truy vấn tất cả mùa vụ
                var cropSeasons = await _unitOfWork.CropSeasonRepository.GetAllAsync(
                    predicate: cs => !cs.IsDeleted,
                    include: query => query.Include(cs => cs.Farmer).ThenInclude(f => f.User),
                    orderBy: query => query.OrderByDescending(cs => cs.StartDate),
                    asNoTracking: true
                );

                if (!cropSeasons.Any())
                    return new ServiceResult(Const.WARNING_NO_DATA_CODE, Const.WARNING_NO_DATA_MSG);

                var dtoList = cropSeasons.Select(cs => cs.MapToCropSeasonViewAllDto()).ToList();
                return new ServiceResult(Const.SUCCESS_READ_CODE, Const.SUCCESS_READ_MSG, dtoList);
            }
            else
            {
                // Là Farmer → truy vấn mùa vụ của chính mình
                var farmer = await _unitOfWork.FarmerRepository.GetByIdAsync(f => f.UserId == userId && !f.IsDeleted);
                if (farmer == null)
                    return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không tìm thấy nông hộ tương ứng.");

                var cropSeasons = await _unitOfWork.CropSeasonRepository.GetAllAsync(
                    predicate: cs => cs.FarmerId == farmer.FarmerId && !cs.IsDeleted,
                    include: query => query.Include(cs => cs.Farmer).ThenInclude(f => f.User),
                    orderBy: query => query.OrderByDescending(cs => cs.StartDate),
                    asNoTracking: true
                );

                if (!cropSeasons.Any())
                    return new ServiceResult(Const.WARNING_NO_DATA_CODE, Const.WARNING_NO_DATA_MSG);

                var dtoList = cropSeasons.Select(cs => cs.MapToCropSeasonViewAllDto()).ToList();
                return new ServiceResult(Const.SUCCESS_READ_CODE, Const.SUCCESS_READ_MSG, dtoList);
            }
        }


        public async Task<IServiceResult> GetById(Guid cropSeasonId, Guid userId, bool isAdmin = false)
        {
            var cropSeason = await _unitOfWork.CropSeasonRepository.GetWithDetailsByIdAsync(cropSeasonId);
            if (cropSeason == null)
                return new ServiceResult(Const.WARNING_NO_DATA_CODE, Const.WARNING_NO_DATA_MSG);

            if (!isAdmin && cropSeason.Farmer?.UserId != userId)
                return new ServiceResult(Const.FAIL_UPDATE_CODE, "Bạn không có quyền truy cập mùa vụ này.");

            var dto = cropSeason.MapToCropSeasonViewDetailsDto();
            return new ServiceResult(Const.SUCCESS_READ_CODE, Const.SUCCESS_READ_MSG, dto);
        }

        public async Task<IServiceResult> Create(CropSeasonCreateDto dto, Guid userId)
        {
            var farmer = await _unitOfWork.FarmerRepository.GetByIdAsync(f => f.UserId == userId && !f.IsDeleted);
            if (farmer == null)
                return new ServiceResult(Const.FAIL_CREATE_CODE, "Không tìm thấy nông hộ tương ứng.");

            var validationResult = await ValidateCropSeasonCreate(dto);
            if (validationResult != null)
                return validationResult;

            string code = await _codeGenerator.GenerateCropSeasonCodeAsync(dto.StartDate.Year);
            var entity = dto.MapToCropSeasonCreateDto(code, farmer.FarmerId);
            entity.Area = dto.Area;

            await _unitOfWork.CropSeasonRepository.CreateAsync(entity);
            var result = await _unitOfWork.SaveChangesAsync();

            if (result > 0)
            {
                var responseDto = entity.MapToCropSeasonViewDetailsDto();
                responseDto.FarmerName = farmer.User?.Name ?? "Unknown";
                return new ServiceResult(Const.SUCCESS_CREATE_CODE, Const.SUCCESS_CREATE_MSG, responseDto);
            }

            return new ServiceResult(Const.FAIL_CREATE_CODE, Const.FAIL_CREATE_MSG);
        }

        private async Task<IServiceResult?> ValidateCropSeasonCreate(CropSeasonCreateDto dto)
        {
            if (dto.StartDate >= dto.EndDate)
                return new ServiceResult(Const.FAIL_CREATE_CODE, "Ngày bắt đầu phải trước ngày kết thúc.");

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

        public async Task<IServiceResult> Update(CropSeasonUpdateDto dto, Guid userId, bool isAdmin = false)
        {
            var cropSeason = await _unitOfWork.CropSeasonRepository.GetWithDetailsByIdAsync(dto.CropSeasonId);
            if (cropSeason == null)
                return new ServiceResult(Const.FAIL_UPDATE_CODE, Const.WARNING_NO_DATA_MSG);

            if (!isAdmin && cropSeason.Farmer?.UserId != userId)
                return new ServiceResult(Const.FAIL_UPDATE_CODE, "Bạn không có quyền cập nhật mùa vụ này.");

            if (dto.StartDate >= dto.EndDate)
                return new ServiceResult(Const.FAIL_UPDATE_CODE, "Ngày bắt đầu phải trước ngày kết thúc.");

            bool isDuplicate = await _unitOfWork.CropSeasonRepository.ExistsAsync(
                x => x.RegistrationId == dto.RegistrationId &&
                     x.StartDate.HasValue &&
                     x.StartDate.Value.Year == dto.StartDate.Year &&
                     x.CropSeasonId != dto.CropSeasonId
            );

            if (isDuplicate)
                return new ServiceResult(Const.FAIL_UPDATE_CODE, "Đã tồn tại mùa vụ khác cho đăng ký canh tác trong năm này.");

            dto.MapToExistingEntity(cropSeason);
            cropSeason.UpdatedAt = DateHelper.NowVietnamTime();

            await _unitOfWork.CropSeasonRepository.UpdateAsync(cropSeason);
            var result = await _unitOfWork.SaveChangesAsync();

            return result > 0
                ? new ServiceResult(Const.SUCCESS_UPDATE_CODE, Const.SUCCESS_UPDATE_MSG)
                : new ServiceResult(Const.FAIL_UPDATE_CODE, Const.FAIL_UPDATE_MSG);
        }

        public async Task<IServiceResult> DeleteById(Guid cropSeasonId, Guid userId, bool isAdmin)
        {
            var cropSeason = await _unitOfWork.CropSeasonRepository.GetWithDetailsByIdAsync(cropSeasonId);
            if (cropSeason == null)
                return new ServiceResult(Const.WARNING_NO_DATA_CODE, Const.WARNING_NO_DATA_MSG);

            if (!isAdmin && cropSeason.Farmer?.UserId != userId)
                return new ServiceResult(Const.FAIL_DELETE_CODE, "Bạn không có quyền xoá mùa vụ này.");

            await _unitOfWork.CropSeasonRepository.DeleteCropSeasonDetailsBySeasonIdAsync(cropSeasonId);
            _unitOfWork.CropSeasonRepository.PrepareRemove(cropSeason);

            var result = await _unitOfWork.SaveChangesAsync();

            return result > 0
                ? new ServiceResult(Const.SUCCESS_DELETE_CODE, Const.SUCCESS_DELETE_MSG)
                : new ServiceResult(Const.FAIL_DELETE_CODE, Const.FAIL_DELETE_MSG);
        }

        public async Task<IServiceResult> SoftDeleteAsync(Guid cropSeasonId, Guid userId, bool isAdmin)
        {
            var cropSeason = await _unitOfWork.CropSeasonRepository.GetByIdAsync(cropSeasonId);
            if (cropSeason == null || cropSeason.IsDeleted)
                return new ServiceResult(Const.WARNING_NO_DATA_CODE, Const.WARNING_NO_DATA_MSG);

            if (!isAdmin && cropSeason.Farmer?.UserId != userId)
                return new ServiceResult(Const.FAIL_DELETE_CODE, "Bạn không có quyền xoá mùa vụ này.");

            cropSeason.IsDeleted = true;
            cropSeason.UpdatedAt = DateHelper.NowVietnamTime();

            await _unitOfWork.CropSeasonRepository.UpdateAsync(cropSeason);
            var result = await _unitOfWork.SaveChangesAsync();

            return result > 0
                ? new ServiceResult(Const.SUCCESS_DELETE_CODE, "Xoá mềm mùa vụ thành công.")
                : new ServiceResult(Const.FAIL_DELETE_CODE, "Xoá mềm mùa vụ thất bại.");
        }
    }
}
