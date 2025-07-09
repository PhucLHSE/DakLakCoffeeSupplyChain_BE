using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.CropSeasonDTOs;
using DakLakCoffeeSupplyChain.Common.Enum.FarmingCommitmentEnums;
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
            try
            {
                var cropSeason = await _unitOfWork.CropSeasonRepository.GetByIdAsync(
                    predicate: cs => cs.CropSeasonId == cropSeasonId && !cs.IsDeleted,
                    include: query => query
                        .Include(cs => cs.Farmer)
                            .ThenInclude(f => f.User)
                        .Include(cs => cs.CropSeasonDetails)
                                .ThenInclude(d => d.CoffeeType) // ✅ THÊM dòng này
                        .Include(cs => cs.Commitment)             // ✅ Bắt buộc thêm
                        .Include(cs => cs.Registration),          // ✅ Bắt buộc thêm
                    asNoTracking: true
                );

                if (cropSeason == null)
                    return new ServiceResult(Const.WARNING_NO_DATA_CODE, Const.WARNING_NO_DATA_MSG);

                if (!isAdmin && cropSeason.Farmer?.UserId != userId)
                    return new ServiceResult(Const.FAIL_UPDATE_CODE, "Bạn không có quyền truy cập mùa vụ này.");

                var dto = cropSeason.MapToCropSeasonViewDetailsDto();
                return new ServiceResult(Const.SUCCESS_READ_CODE, Const.SUCCESS_READ_MSG, dto);
            }
            catch (Exception ex)
            {
                return new ServiceResult(Const.ERROR_EXCEPTION, ex.ToString());
            }
        }



        public async Task<IServiceResult> Create(CropSeasonCreateDto dto, Guid userId)
        {
            // 1. Tìm Farmer theo userId
            var farmer = await _unitOfWork.FarmerRepository.GetByIdAsync(f => f.UserId == userId && !f.IsDeleted);
            if (farmer == null)
                return new ServiceResult(Const.FAIL_CREATE_CODE, "Không tìm thấy nông hộ tương ứng.");

            // 2. Tìm Cam kết + truy Registration
            var commitment = await _unitOfWork.FarmingCommitmentRepository.GetWithRegistrationAsync(dto.CommitmentId);
            if (commitment == null)
                return new ServiceResult(Const.FAIL_CREATE_CODE, "Không tìm thấy cam kết canh tác.");

            var registration = commitment.RegistrationDetail?.Registration;
            if (registration == null)
                return new ServiceResult(Const.FAIL_CREATE_CODE, "Không tìm thấy đơn đăng ký tương ứng với cam kết.");

            // ✅ Kiểm tra FarmerId trong cam kết có trùng với user đang đăng nhập không
            if (commitment.FarmerId != farmer.FarmerId)
                return new ServiceResult(Const.FAIL_CREATE_CODE, "Cam kết không thuộc về bạn.");

            // ✅ Kiểm tra trạng thái duyệt: dùng Status hoặc ApprovedAt
            if (commitment.Status != FarmingCommitmentStatus.Active.ToString())
                return new ServiceResult(Const.FAIL_CREATE_CODE, "Cam kết chưa được duyệt hoặc không hợp lệ.");


            // 3. Validate ngày
            if (dto.StartDate >= dto.EndDate)
                return new ServiceResult(Const.FAIL_CREATE_CODE, "Ngày bắt đầu phải trước ngày kết thúc.");

            // 4. Kiểm tra duplicate mùa vụ trong cùng năm theo Registration
            bool isDuplicate = await _unitOfWork.CropSeasonRepository.ExistsAsync(
                x => x.RegistrationId == registration.RegistrationId &&
                     x.StartDate.HasValue &&
                     x.StartDate.Value.Year == dto.StartDate.Year
            );

            if (isDuplicate)
            {
                return new ServiceResult(Const.FAIL_CREATE_CODE,
                    $"Đăng ký {registration.RegistrationCode} đã có mùa vụ trong năm {dto.StartDate.Year}.");
            }

            // 5. Tạo mã mùa vụ
            string code = await _codeGenerator.GenerateCropSeasonCodeAsync(dto.StartDate.Year);

            // 6. Map sang entity
            var entity = dto.MapToCropSeasonCreateDto(code, farmer.FarmerId, registration.RegistrationId);
            entity.Area = dto.Area ?? 0;

            // 7. Ghi vào DB
            await _unitOfWork.CropSeasonRepository.CreateAsync(entity);
            var result = await _unitOfWork.SaveChangesAsync();

            if (result > 0)
            {
                var fullEntity = await _unitOfWork.CropSeasonRepository.GetWithDetailsByIdAsync(entity.CropSeasonId);
                if (fullEntity == null)
                    return new ServiceResult(Const.FAIL_CREATE_CODE, "Tạo mùa vụ thành công nhưng không lấy được dữ liệu.");

                var responseDto = fullEntity.MapToCropSeasonViewDetailsDto();
                return new ServiceResult(Const.SUCCESS_CREATE_CODE, Const.SUCCESS_CREATE_MSG, responseDto);
            }

            return new ServiceResult(Const.FAIL_CREATE_CODE, Const.FAIL_CREATE_MSG);
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
                     x => x.RegistrationId == cropSeason.RegistrationId &&  // ✅ dùng từ DB
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

            // 🔒 Quyền hạn
            if (!isAdmin && cropSeason.Farmer?.UserId != userId)
                return new ServiceResult(Const.FAIL_DELETE_CODE, "Bạn không có quyền xoá mùa vụ này.");

            // ❗ Chỉ xoá khi status là Cancelled
            if (cropSeason.Status != "Cancelled")
                return new ServiceResult(Const.FAIL_DELETE_CODE, "Chỉ có thể xoá mùa vụ đã huỷ.");

            // Nếu có vùng trồng thì không được xoá
            if (cropSeason.CropSeasonDetails != null && cropSeason.CropSeasonDetails.Any())
                return new ServiceResult(Const.FAIL_DELETE_CODE, "Không thể xoá mùa vụ đã có vùng trồng.");

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

            // ❗ Chỉ cho xoá mềm nếu status là Cancelled
            if (cropSeason.Status != "Cancelled")
                return new ServiceResult(Const.FAIL_DELETE_CODE, "Chỉ có thể xoá mùa vụ đã huỷ.");

            // Kiểm tra có vùng trồng chưa
            var hasDetails = await _unitOfWork.CropSeasonDetailRepository
                .ExistsAsync(d => d.CropSeasonId == cropSeasonId && !d.IsDeleted);

            if (hasDetails)
                return new ServiceResult(Const.FAIL_DELETE_CODE, "Không thể xoá mùa vụ đã có vùng trồng.");

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
