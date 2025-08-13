using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.CropStageDTOs;
using DakLakCoffeeSupplyChain.Common.Helpers;
using DakLakCoffeeSupplyChain.Repositories.UnitOfWork;
using DakLakCoffeeSupplyChain.Services.Base;
using DakLakCoffeeSupplyChain.Services.IServices;
using DakLakCoffeeSupplyChain.Services.Mappers;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace DakLakCoffeeSupplyChain.Services.Services
{
    public class CropStageService : ICropStageService
    {
        private readonly IUnitOfWork _uow;

        public CropStageService(IUnitOfWork uow)
        {
            _uow = uow ?? throw new ArgumentNullException(nameof(uow));
        }

        private static string Normalize(string s)
        {
            var v = (s ?? string.Empty).Trim().ToLowerInvariant();
            return Regex.Replace(v, "[^a-z0-9-]", "");
        }

        public async Task<IServiceResult> GetAll()
        {
            try
            {
                var entities = await _uow.CropStageRepository.GetAllAsync(
                    predicate: s => !s.IsDeleted,
                    orderBy: q => q.OrderBy(s => s.OrderIndex ?? int.MaxValue).ThenBy(s => s.StageId),
                    asNoTracking: true
                );

                var dtos = entities.Select(x => x.ToViewDto()).ToList();
                if (!dtos.Any())
                    return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không có giai đoạn nào trong hệ thống.", dtos);

                return new ServiceResult(Const.SUCCESS_READ_CODE, Const.SUCCESS_READ_MSG, dtos);
            }
            catch (Exception ex)
            {
                return new ServiceResult(Const.ERROR_EXCEPTION, "Lỗi khi lấy danh sách giai đoạn: " + ex.Message);
            }
        }

        public async Task<IServiceResult> GetById(int id)
        {
            var entity = await _uow.CropStageRepository.GetByIdAsync(id);
            if (entity == null)
                return new ServiceResult(Const.FAIL_READ_CODE, "Giai đoạn không tồn tại.");
            return new ServiceResult(Const.SUCCESS_READ_CODE, Const.SUCCESS_READ_MSG, entity.MapToViewDto());
        }

        public async Task<IServiceResult> Create(CropStageCreateDto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.StageCode) || string.IsNullOrWhiteSpace(dto.StageName))
                    return new ServiceResult(Const.FAIL_CREATE_CODE, "StageCode/StageName là bắt buộc.");

                dto.StageCode = Normalize(dto.StageCode);
                dto.StageName = dto.StageName.Trim();

                if (dto.OrderIndex is <= 0)
                    return new ServiceResult(Const.FAIL_CREATE_CODE, "OrderIndex phải > 0.");

                if (await _uow.CropStageRepository.AnyAsync(x => !x.IsDeleted && x.StageCode == dto.StageCode))
                    return new ServiceResult(Const.FAIL_CREATE_CODE, "Mã giai đoạn đã tồn tại.");
                if (await _uow.CropStageRepository.AnyAsync(x => !x.IsDeleted && x.StageName == dto.StageName))
                    return new ServiceResult(Const.FAIL_CREATE_CODE, "Tên giai đoạn đã tồn tại.");
                if (dto.OrderIndex.HasValue &&
                    await _uow.CropStageRepository.AnyAsync(x => !x.IsDeleted && x.OrderIndex == dto.OrderIndex))
                    return new ServiceResult(Const.FAIL_CREATE_CODE, $"Thứ tự {dto.OrderIndex} đã tồn tại.");

                var entity = dto.MapToNewCropStage();
                entity.CreatedAt = DateHelper.NowVietnamTime();
                entity.UpdatedAt = entity.CreatedAt;

                await _uow.CropStageRepository.CreateAsync(entity);
                var saved = await _uow.SaveChangesAsync();
                if (saved <= 0) return new ServiceResult(Const.FAIL_CREATE_CODE, Const.FAIL_CREATE_MSG);

                var created = await _uow.CropStageRepository.GetByIdAsync(entity.StageId, true);
                return new ServiceResult(Const.SUCCESS_CREATE_CODE, Const.SUCCESS_CREATE_MSG, created!.MapToViewDto());
            }
            catch (Exception ex)
            {
                return new ServiceResult(Const.ERROR_EXCEPTION, ex.ToString());
            }
        }

        public async Task<IServiceResult> Update(CropStageUpdateDto dto)
        {
            try
            {
                var stage = await _uow.CropStageRepository.GetByIdAsync(dto.StageId);
                if (stage == null || stage.IsDeleted)
                    return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không tìm thấy giai đoạn cần cập nhật.");

                if (string.IsNullOrWhiteSpace(dto.StageCode) || string.IsNullOrWhiteSpace(dto.StageName))
                    return new ServiceResult(Const.FAIL_UPDATE_CODE, "StageCode/StageName là bắt buộc.");

                dto.StageCode = Normalize(dto.StageCode);
                dto.StageName = dto.StageName.Trim();

                if (dto.OrderIndex is <= 0)
                    return new ServiceResult(Const.FAIL_UPDATE_CODE, "OrderIndex phải > 0.");

                if (await _uow.CropStageRepository.AnyAsync(x => !x.IsDeleted && x.StageCode == dto.StageCode && x.StageId != dto.StageId))
                    return new ServiceResult(Const.FAIL_UPDATE_CODE, "Mã giai đoạn đã tồn tại.");
                if (await _uow.CropStageRepository.AnyAsync(x => !x.IsDeleted && x.StageName == dto.StageName && x.StageId != dto.StageId))
                    return new ServiceResult(Const.FAIL_UPDATE_CODE, "Tên giai đoạn đã tồn tại.");
                if (dto.OrderIndex.HasValue &&
                    await _uow.CropStageRepository.AnyAsync(x => !x.IsDeleted && x.OrderIndex == dto.OrderIndex && x.StageId != dto.StageId))
                    return new ServiceResult(Const.FAIL_UPDATE_CODE, $"Thứ tự {dto.OrderIndex} đã tồn tại.");

                dto.MapToUpdateCropStage(stage);
                stage.UpdatedAt = DateHelper.NowVietnamTime();

                await _uow.CropStageRepository.UpdateAsync(stage);
                var saved = await _uow.SaveChangesAsync();
                if (saved <= 0) return new ServiceResult(Const.FAIL_UPDATE_CODE, Const.FAIL_UPDATE_MSG);

                var updated = await _uow.CropStageRepository.GetByIdAsync(stage.StageId);
                return new ServiceResult(Const.SUCCESS_UPDATE_CODE, Const.SUCCESS_UPDATE_MSG, updated!.MapToViewDto());
            }
            catch (Exception ex)
            {
                return new ServiceResult(Const.ERROR_EXCEPTION, ex.ToString());
            }
        }

        public async Task<IServiceResult> DeleteById(int stageId)
        {
            try
            {
                var stage = await _uow.CropStageRepository.GetByIdAsync(stageId);
                if (stage == null) return new ServiceResult(Const.WARNING_NO_DATA_CODE, Const.WARNING_NO_DATA_MSG);

                // block hard delete if in use by any progress
                var inUse = await _uow.CropProgressRepository.AnyAsync(p => p.StageId == stageId && !p.IsDeleted);
                if (inUse)
                    return new ServiceResult(Const.FAIL_DELETE_CODE, "Giai đoạn đang được sử dụng. Vui lòng xoá mềm.");

                await _uow.CropStageRepository.RemoveAsync(stage);
                var saved = await _uow.SaveChangesAsync();

                return saved > 0
                    ? new ServiceResult(Const.SUCCESS_DELETE_CODE, Const.SUCCESS_DELETE_MSG)
                    : new ServiceResult(Const.FAIL_DELETE_CODE, Const.FAIL_DELETE_MSG);
            }
            catch (Exception ex)
            {
                return new ServiceResult(Const.ERROR_EXCEPTION, $"Lỗi khi xóa giai đoạn: {ex.Message}");
            }
        }

        public async Task<IServiceResult> SoftDeleteById(int stageId)
        {
            try
            {
                var stage = await _uow.CropStageRepository.GetByIdAsync(stageId);
                if (stage == null || stage.IsDeleted)
                    return new ServiceResult(Const.WARNING_NO_DATA_CODE, Const.WARNING_NO_DATA_MSG);

                stage.IsDeleted = true;
                stage.UpdatedAt = DateHelper.NowVietnamTime();
                await _uow.CropStageRepository.UpdateAsync(stage);

                var saved = await _uow.SaveChangesAsync();
                return saved > 0
                    ? new ServiceResult(Const.SUCCESS_DELETE_CODE, Const.SUCCESS_DELETE_MSG)
                    : new ServiceResult(Const.FAIL_DELETE_CODE, Const.FAIL_DELETE_MSG);
            }
            catch (Exception ex)
            {
                return new ServiceResult(Const.ERROR_EXCEPTION, $"Lỗi khi xóa mềm giai đoạn: {ex.Message}");
            }
        }
    }
}
