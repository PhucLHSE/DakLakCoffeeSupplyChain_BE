﻿using DakLakCoffeeSupplyChain.Common;
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
using DakLakCoffeeSupplyChain.Common.Enum.CropSeasonEnums;

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

            // 3. Kiểm tra quyền sở hữu cam kết
            if (commitment.FarmerId != farmer.FarmerId)
                return new ServiceResult(Const.FAIL_CREATE_CODE, "Cam kết không thuộc về bạn.");

            // 4. Kiểm tra trạng thái duyệt
            if (commitment.Status != FarmingCommitmentStatus.Active.ToString())
                return new ServiceResult(Const.FAIL_CREATE_CODE, "Cam kết chưa được duyệt hoặc không hợp lệ.");

            // 5. Kiểm tra nếu cam kết đã được dùng
            bool hasUsed = await _unitOfWork.CropSeasonRepository.ExistsAsync(
                x => x.CommitmentId == dto.CommitmentId && !x.IsDeleted);
            if (hasUsed)
                return new ServiceResult(Const.FAIL_CREATE_CODE, "Cam kết này đã được dùng để tạo một mùa vụ khác.");

            // 6. Validate ngày
            if (dto.StartDate >= dto.EndDate)
                return new ServiceResult(Const.FAIL_CREATE_CODE, "Ngày bắt đầu phải trước ngày kết thúc.");

            // ❌ 7. BỎ kiểm tra duplicate mùa vụ theo đăng ký/năm – CHO PHÉP 1-N

            // 8. Tạo mã mùa vụ
            string code = await _codeGenerator.GenerateCropSeasonCodeAsync(dto.StartDate.Year);

            // 9. Map entity
            var cropSeason = dto.MapToCropSeasonCreateDto(code, farmer.FarmerId, registration.RegistrationId);
            cropSeason.Area = dto.Area ?? 0;
            cropSeason.CommitmentId = commitment.CommitmentId;

            await _unitOfWork.CropSeasonRepository.CreateAsync(cropSeason);

            // 10. Tạo vùng trồng từ RegistrationDetail
            var registrationDetails = await _unitOfWork.CultivationRegistrationsDetailRepository
                .GetByRegistrationIdAsync(registration.RegistrationId);

            var cropSeasonDetails = new List<CropSeasonDetail>();

            foreach (var detail in registrationDetails)
            {
                var planDetail = await _unitOfWork.ProcurementPlanDetailsRepository.GetByIdAsync(detail.PlanDetailId);
                if (planDetail == null)
                    continue;

                cropSeasonDetails.Add(new CropSeasonDetail
                {
                    DetailId = Guid.NewGuid(),
                    CropSeasonId = cropSeason.CropSeasonId,
                    CoffeeTypeId = planDetail.CoffeeTypeId,
                    ExpectedHarvestStart = detail.ExpectedHarvestStart,
                    ExpectedHarvestEnd = detail.ExpectedHarvestEnd,
                    EstimatedYield = detail.EstimatedYield,
                    AreaAllocated = cropSeason.Area / registrationDetails.Count,
                    PlannedQuality = null,
                    QualityGrade = null,
                    Status = CropDetailStatus.Planned.ToString(),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsDeleted = false
                });
            }

            foreach (var d in cropSeasonDetails)
            {
                await _unitOfWork.CropSeasonDetailRepository.CreateAsync(d);
            }

            var result = await _unitOfWork.SaveChangesAsync();
            if (result > 0)
            {
                var fullEntity = await _unitOfWork.CropSeasonRepository.GetWithDetailsByIdAsync(cropSeason.CropSeasonId);
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

            // ❌ BỎ kiểm tra trùng mùa vụ cùng năm theo Registration – CHO PHÉP 1-N

            // Cập nhật entity
            dto.MapToExistingEntity(cropSeason);
            cropSeason.UpdatedAt = DateHelper.NowVietnamTime();

            // FIX EF navigation trùng
            foreach (var detail in cropSeason.CropSeasonDetails)
            {
                detail.CoffeeType = null;
            }

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
