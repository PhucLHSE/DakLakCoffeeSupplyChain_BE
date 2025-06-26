using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.CropProgressDTOs;
using DakLakCoffeeSupplyChain.Common.Helpers;
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

            return new ServiceResult(Const.SUCCESS_READ_CODE, Const.SUCCESS_READ_MSG, progress.MapToCropProgressViewDetailsDto());
        }

        public async Task<IServiceResult> Create(CropProgressCreateDto dto)
        {
            try
            {
                var stage = await _unitOfWork.CropStageRepository.GetByIdAsync(dto.StageId);
                if (stage == null)
                    return new ServiceResult(Const.FAIL_CREATE_CODE, "Giai đoạn không tồn tại.");

                var detail = await _unitOfWork.CultivationRegistrationRepository.GetCropSeasonDetailByIdAsync(dto.CropSeasonDetailId);
                if (detail == null)
                    return new ServiceResult(Const.FAIL_CREATE_CODE, "Chi tiết mùa vụ không tồn tại.");

                var farmer = await _unitOfWork.FarmerRepository.GetByIdAsync(dto.UpdatedBy);
                if (farmer == null)
                    return new ServiceResult(Const.FAIL_CREATE_CODE, "Nông dân không tồn tại.");

                if (dto.ProgressDate.HasValue &&
                    dto.ProgressDate.Value.ToDateTime(new TimeOnly(0, 0)) > DateTime.UtcNow)
                {
                    return new ServiceResult(Const.FAIL_CREATE_CODE, "Ngày ghi nhận không được lớn hơn hôm nay.");
                }

                var duplicate = await _unitOfWork.CropProgressRepository.GetAllAsync(p =>
                    !p.IsDeleted &&
                    p.CropSeasonDetailId == dto.CropSeasonDetailId &&
                    p.StageId == dto.StageId &&
                    p.ProgressDate == dto.ProgressDate
                );
                if (duplicate.Any())
                {
                    return new ServiceResult(Const.FAIL_CREATE_CODE, "Tiến trình đã tồn tại với ngày và giai đoạn này.");
                }

                var entity = dto.MapToCropProgressCreateDto();

                await _unitOfWork.CropProgressRepository.CreateAsync(entity);
                var result = await _unitOfWork.SaveChangesAsync();

                if (result > 0)
                    return new ServiceResult(Const.SUCCESS_CREATE_CODE, Const.SUCCESS_CREATE_MSG, entity.MapToCropProgressViewDetailsDto());

                return new ServiceResult(Const.FAIL_CREATE_CODE, Const.FAIL_CREATE_MSG);
            }
            catch (Exception ex)
            {
                return new ServiceResult(Const.ERROR_EXCEPTION, ex.ToString());
            }
        }
        public async Task<IServiceResult> Update(CropProgressUpdateDto dto)
        {
            try
            {
                var entity = await _unitOfWork.CropProgressRepository.GetByIdAsync(dto.ProgressId);
                if (entity == null || entity.IsDeleted)
                {
                    return new ServiceResult(
                        Const.WARNING_NO_DATA_CODE,
                        $"Không tìm thấy tiến trình với ID = {dto.ProgressId} hoặc bản ghi đã bị xóa."
                    );
                }

                var seasonDetail = await _unitOfWork.CultivationRegistrationRepository
                    .GetCropSeasonDetailByIdAsync(dto.CropSeasonDetailId);
                if (seasonDetail == null)
                {
                    return new ServiceResult(
                        Const.FAIL_UPDATE_CODE,
                        $"Chi tiết mùa vụ với ID = {dto.CropSeasonDetailId} không tồn tại."
                    );
                }

                var stage = await _unitOfWork.CropStageRepository.GetByIdAsync(dto.StageId);
                if (stage == null)
                {
                    return new ServiceResult(
                        Const.FAIL_UPDATE_CODE,
                        $"Giai đoạn với ID = {dto.StageId} không tồn tại."
                    );
                }

                var farmer = await _unitOfWork.FarmerRepository.GetByIdAsync(dto.UpdatedBy);
                if (farmer == null)
                {
                    return new ServiceResult(
                        Const.FAIL_UPDATE_CODE,
                        $"Người cập nhật (UpdatedBy) với ID = {dto.UpdatedBy} không tồn tại."
                    );
                }

                var linkedReports = await _unitOfWork.GeneralFarmerReportRepository.GetAllAsync(
                    r => r.ProcessingProgressId == dto.ProgressId
                );
                if (linkedReports.Any() && dto.CropSeasonDetailId != entity.CropSeasonDetailId)
                {
                    return new ServiceResult(
                        Const.FAIL_UPDATE_CODE,
                        $"Không được thay đổi CropSeasonDetailId vì tiến trình này đã liên kết với báo cáo nông hộ."
                    );
                }

                var duplicates = await _unitOfWork.CropProgressRepository.GetAllAsync(
                    p => p.CropSeasonDetailId == dto.CropSeasonDetailId
                      && p.StageId == dto.StageId
                      && p.ProgressDate == dto.ProgressDate
                      && p.ProgressId != dto.ProgressId
                      && !p.IsDeleted
                );
                if (duplicates.Any())
                {
                    return new ServiceResult(
                        Const.FAIL_UPDATE_CODE,
                        $"Đã tồn tại tiến trình khác có cùng ngày {dto.ProgressDate} và giai đoạn {stage.StageName} trong mùa vụ."
                    );
                }

                dto.MapToUpdateCropProgress(entity);

                await _unitOfWork.CropProgressRepository.UpdateAsync(entity);
                var saveResult = await _unitOfWork.SaveChangesAsync();

                if (saveResult > 0)
                {
                    var fullEntity = await _unitOfWork.CropProgressRepository.GetByIdWithIncludesAsync(dto.ProgressId);
                    var responseDto = fullEntity?.MapToCropProgressViewDetailsDto();

                    return new ServiceResult(Const.SUCCESS_UPDATE_CODE, Const.SUCCESS_UPDATE_MSG, responseDto);
                }

                return new ServiceResult(Const.FAIL_UPDATE_CODE, "Cập nhật thất bại. Không có thay đổi nào được ghi nhận.");
            }
            catch (Exception ex)
            {
                return new ServiceResult(Const.ERROR_EXCEPTION, "Lỗi hệ thống khi cập nhật tiến trình: " + ex.Message);
            }
        }

        public async Task<IServiceResult> DeleteById(Guid progressId)
        {
            try
            {
                // Tìm tiến trình mùa vụ theo ID từ repository
                var progress = await _unitOfWork.CropProgressRepository.GetByIdAsync(progressId);

                // Nếu không tìm thấy tiến trình, trả về cảnh báo không có dữ liệu
                if (progress == null)
                {
                    return new ServiceResult(
                        Const.WARNING_NO_DATA_CODE,
                        Const.WARNING_NO_DATA_MSG
                    );
                }
                else
                {
                    // Xóa tiến trình ra khỏi repository
                    await _unitOfWork.CropProgressRepository.RemoveAsync(progress);

                    // Lưu thay đổi vào database
                    var result = await _unitOfWork.SaveChangesAsync();

                    // Kiểm tra xem việc lưu có thành công không
                    if (result > 0)
                    {
                        return new ServiceResult(
                            Const.SUCCESS_DELETE_CODE,
                            Const.SUCCESS_DELETE_MSG
                        );
                    }
                    else
                    {
                        return new ServiceResult(
                            Const.FAIL_DELETE_CODE,
                            Const.FAIL_DELETE_MSG
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                // Xử lý ngoại lệ nếu có lỗi xảy ra trong quá trình xóa
                return new ServiceResult(
                    Const.ERROR_EXCEPTION,
                    ex.ToString()
                );
            }
        }

        public async Task<IServiceResult> SoftDeleteById(Guid progressId)
        {
            try
            {
                // Tìm tiến trình mùa vụ theo ID từ repository
                var progress = await _unitOfWork.CropProgressRepository.GetByIdAsync(progressId);

                // Nếu không tìm thấy tiến trình, trả về cảnh báo không có dữ liệu
                if (progress == null)
                {
                    return new ServiceResult(
                        Const.WARNING_NO_DATA_CODE,
                        Const.WARNING_NO_DATA_MSG
                    );
                }
                else
                {
                    // Đánh dấu xoá mềm bằng IsDeleted
                    progress.IsDeleted = true;
                    progress.UpdatedAt = DateHelper.NowVietnamTime();

                    // Cập nhật lại tiến trình đã đánh dấu xóa
                    await _unitOfWork.CropProgressRepository.UpdateAsync(progress);

                    // Lưu thay đổi vào database
                    var result = await _unitOfWork.SaveChangesAsync();

                    // Kiểm tra xem việc lưu có thành công không
                    if (result > 0)
                    {
                        return new ServiceResult(
                            Const.SUCCESS_DELETE_CODE,
                            Const.SUCCESS_DELETE_MSG
                        );
                    }
                    else
                    {
                        return new ServiceResult(
                            Const.FAIL_DELETE_CODE,
                            Const.FAIL_DELETE_MSG
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                // Xử lý ngoại lệ nếu có lỗi xảy ra trong quá trình xóa
                return new ServiceResult(
                    Const.ERROR_EXCEPTION,
                    ex.ToString()
                );
            }
        }



    }
}
