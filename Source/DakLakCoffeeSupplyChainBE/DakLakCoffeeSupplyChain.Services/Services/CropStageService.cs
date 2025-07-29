using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.CropStageDTOs;
using DakLakCoffeeSupplyChain.Common.Helpers;
using DakLakCoffeeSupplyChain.Repositories.Models;
using DakLakCoffeeSupplyChain.Repositories.UnitOfWork;
using DakLakCoffeeSupplyChain.Services.Base;
using DakLakCoffeeSupplyChain.Services.IServices;
using DakLakCoffeeSupplyChain.Services.Mappers;

namespace DakLakCoffeeSupplyChain.Services.Services
{
    public class CropStageService : ICropStageService
    {
        private readonly IUnitOfWork _unitOfWork;

        public CropStageService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task<IServiceResult> GetAll()
        {
            try
            {
                var entities = await _unitOfWork.CropStageRepository.GetAllOrderedAsync();

                if (entities == null || !entities.Any())
                {
                    return new ServiceResult(
                        Const.WARNING_NO_DATA_CODE,
                        "Không có giai đoạn nào trong hệ thống.",
                        new List<CropStageViewAllDto>()
                    );
                }

                var dtos = entities.Select(x => x.ToViewDto()).ToList();

                return new ServiceResult(
                    Const.SUCCESS_READ_CODE,
                    Const.SUCCESS_READ_MSG,
                    dtos
                );
            }
            catch (Exception ex)
            {
                return new ServiceResult(Const.ERROR_EXCEPTION, "Lỗi khi lấy danh sách giai đoạn: " + ex.Message);
            }
        }
        public async Task<IServiceResult> GetById(int id)
        {
            var entity = await _unitOfWork.CropStageRepository.GetByIdAsync(id);

            if (entity == null)
            {
                return new ServiceResult(
                    Const.FAIL_READ_CODE,
                    "Giai đoạn không tồn tại.",
                    null
                );
            }

            var dto = entity.MapToViewDto();

            return new ServiceResult(
                Const.SUCCESS_READ_CODE,
                Const.SUCCESS_READ_MSG,
                dto
            );
        }

        public async Task<IServiceResult> Create(CropStageCreateDto dto)
        {
            try
            {
                // Kiểm tra trùng StageCode
                var duplicateCode = await _unitOfWork.CropStageRepository
                    .GetAllAsync(x => x.StageCode == dto.StageCode && x.IsDeleted == false);

                if (duplicateCode.Any())
                {
                    return new ServiceResult(Const.FAIL_CREATE_CODE, "Mã giai đoạn đã tồn tại.");
                }

                // Kiểm tra trùng StageName
                var duplicateName = await _unitOfWork.CropStageRepository
                    .GetAllAsync(x => x.StageName == dto.StageName && x.IsDeleted == false);

                if (duplicateName.Any())
                {
                    return new ServiceResult(Const.FAIL_CREATE_CODE, "Tên giai đoạn đã tồn tại.");
                }

                // Kiểm tra trùng OrderIndex
                if (dto.OrderIndex.HasValue)
                {
                    var duplicateOrder = await _unitOfWork.CropStageRepository
                        .GetAllAsync(x => x.OrderIndex == dto.OrderIndex && x.IsDeleted == false);

                    if (duplicateOrder.Any())
                    {
                        return new ServiceResult(Const.FAIL_CREATE_CODE, $"Thứ tự giai đoạn {dto.OrderIndex} đã tồn tại.");
                    }
                }

                var newStage = dto.MapToNewCropStage();
                await _unitOfWork.CropStageRepository.CreateAsync(newStage);

                var result = await _unitOfWork.SaveChangesAsync();

                if (result > 0)
                {
                    return new ServiceResult(Const.SUCCESS_CREATE_CODE, Const.SUCCESS_CREATE_MSG, newStage);
                }

                return new ServiceResult(Const.FAIL_CREATE_CODE, Const.FAIL_CREATE_MSG);
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
                var stage = await _unitOfWork.CropStageRepository.GetByIdAsync(dto.StageId);

                if (stage == null || stage.IsDeleted)
                    return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không tìm thấy giai đoạn cần cập nhật.");

                var duplicateCode = await _unitOfWork.CropStageRepository
                    .GetAllAsync(x => x.StageCode == dto.StageCode && x.StageId != dto.StageId && x.IsDeleted == false);

                if (duplicateCode.Any())
                    return new ServiceResult(Const.FAIL_UPDATE_CODE, "Mã giai đoạn đã tồn tại.");

                var duplicateName = await _unitOfWork.CropStageRepository
                    .GetAllAsync(x => x.StageName == dto.StageName && x.StageId != dto.StageId && x.IsDeleted == false);

                if (duplicateName.Any())
                    return new ServiceResult(Const.FAIL_UPDATE_CODE, "Tên giai đoạn đã tồn tại.");

                if (dto.OrderIndex.HasValue)
                {
                    var duplicateOrder = await _unitOfWork.CropStageRepository
                        .GetAllAsync(x => x.OrderIndex == dto.OrderIndex && x.StageId != dto.StageId && x.IsDeleted == false);

                    if (duplicateOrder.Any())
                        return new ServiceResult(Const.FAIL_UPDATE_CODE, $"Thứ tự giai đoạn {dto.OrderIndex} đã tồn tại.");
                }

                dto.MapToUpdateCropStage(stage);
                await _unitOfWork.CropStageRepository.UpdateAsync(stage);

                var result = await _unitOfWork.SaveChangesAsync();
                if (result > 0)
                    return new ServiceResult(Const.SUCCESS_UPDATE_CODE, Const.SUCCESS_UPDATE_MSG, stage);

                return new ServiceResult(Const.FAIL_UPDATE_CODE, Const.FAIL_UPDATE_MSG);
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
                // Tìm giai đoạn theo ID
                var stage = await _unitOfWork.CropStageRepository.GetByIdAsync(stageId);

                // Không tìm thấy
                if (stage == null)
                {
                    return new ServiceResult(
                        Const.WARNING_NO_DATA_CODE,
                        Const.WARNING_NO_DATA_MSG
                    );
                }

                // Xóa giai đoạn
                await _unitOfWork.CropStageRepository.RemoveAsync(stage);

                // Lưu thay đổi
                var result = await _unitOfWork.SaveChangesAsync();

                if (result > 0)
                {
                    return new ServiceResult(
                        Const.SUCCESS_DELETE_CODE,
                        Const.SUCCESS_DELETE_MSG
                    );
                }

                return new ServiceResult(
                    Const.FAIL_DELETE_CODE,
                    Const.FAIL_DELETE_MSG
                );
            }
            catch (Exception ex)
            {
                return new ServiceResult(
                    Const.ERROR_EXCEPTION,
                    $"Lỗi khi xóa giai đoạn: {ex.Message}"
                );
            }
        }
        public async Task<IServiceResult> SoftDeleteById(int stageId)
        {
            try
            {
                // Tìm giai đoạn theo ID
                var stage = await _unitOfWork.CropStageRepository.GetByIdAsync(stageId);

                // Không tìm thấy hoặc đã xóa mềm
                if (stage == null || stage.IsDeleted)
                {
                    return new ServiceResult(
                        Const.WARNING_NO_DATA_CODE,
                        Const.WARNING_NO_DATA_MSG
                    );
                }

                // Gán trạng thái xoá mềm
                stage.IsDeleted = true;
                stage.UpdatedAt = DateHelper.NowVietnamTime();

                await _unitOfWork.CropStageRepository.UpdateAsync(stage);

                // Lưu thay đổi
                var result = await _unitOfWork.SaveChangesAsync();

                if (result > 0)
                {
                    return new ServiceResult(
                        Const.SUCCESS_DELETE_CODE,
                        Const.SUCCESS_DELETE_MSG
                    );
                }

                return new ServiceResult(
                    Const.FAIL_DELETE_CODE,
                    Const.FAIL_DELETE_MSG
                );
            }
            catch (Exception ex)
            {
                return new ServiceResult(
                    Const.ERROR_EXCEPTION,
                    $"Lỗi khi xóa mềm giai đoạn: {ex.Message}"
                );
            }
        }


    }
}