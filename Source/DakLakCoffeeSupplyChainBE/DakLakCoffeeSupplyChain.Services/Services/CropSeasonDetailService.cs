using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.CropSeasonDetailDTOs;
using DakLakCoffeeSupplyChain.Common.Helpers;
using DakLakCoffeeSupplyChain.Repositories.UnitOfWork;
using DakLakCoffeeSupplyChain.Services.Base;
using DakLakCoffeeSupplyChain.Services.IServices;
using DakLakCoffeeSupplyChain.Services.Mappers;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Services.Services
{
    public class CropSeasonDetailService : ICropSeasonDetailService
    {
        private readonly IUnitOfWork _unitOfWork;

        public CropSeasonDetailService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task<IServiceResult> GetAll(Guid userId, bool isAdmin = false)
        {
            try
            {
                var details = await _unitOfWork.CropSeasonDetailRepository.GetAllAsync(
                    predicate: d =>
                        !d.IsDeleted &&
                        (isAdmin || d.CropSeason.Farmer.UserId == userId),
                    include: query => query
                        .Include(d => d.CommitmentDetail)
                            .ThenInclude(d => d.PlanDetail)
                        .Include(d => d.CropSeason)
                            .ThenInclude(cs => cs.Farmer)
                                .ThenInclude(f => f.User),
                    asNoTracking: true
                );

                if (details == null || !details.Any())
                {
                    return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không có dòng mùa vụ nào.");
                }

                var dtos = details.Select(d => d.MapToCropSeasonDetailViewDto()).ToList();
                return new ServiceResult(Const.SUCCESS_READ_CODE, Const.SUCCESS_READ_MSG, dtos);
            }
            catch (Exception ex)
            {
                return new ServiceResult(Const.ERROR_EXCEPTION, ex.ToString());
            }
        }


        public async Task<IServiceResult> GetById(Guid detailId, Guid userId, bool isAdmin = false)
        {
            try
            {
                var detail = await _unitOfWork.CropSeasonDetailRepository.GetByIdAsync(
                    predicate: d => d.DetailId == detailId && !d.IsDeleted,
                    include: query => query
                        .Include(d => d.CommitmentDetail)
                            .ThenInclude(d => d.PlanDetail)
                        .Include(d => d.CropSeason)
                            .ThenInclude(cs => cs.Farmer)
                                .ThenInclude(f => f.User), // 👈 Quan trọng: cần include cả User để lấy tên nông hộ
                    asNoTracking: true
                );

                if (detail == null)
                    return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không tìm thấy chi tiết mùa vụ.");

                // ✅ Phân quyền: chỉ admin hoặc chính chủ nông hộ mới được xem
                if (!isAdmin && detail.CropSeason?.Farmer?.UserId != userId)
                    return new ServiceResult(Const.FAIL_READ_CODE, "Bạn không có quyền xem chi tiết mùa vụ này.");

                var dto = detail.MapToCropSeasonDetailViewDto();
                return new ServiceResult(Const.SUCCESS_READ_CODE, Const.SUCCESS_READ_MSG, dto);
            }
            catch (Exception ex)
            {
                return new ServiceResult(Const.ERROR_EXCEPTION, ex.ToString());
            }
        }



        public async Task<IServiceResult> Create(CropSeasonDetailCreateDto dto)
        {
            try
            {
                var season = await _unitOfWork.CropSeasonRepository.GetByIdAsync(
                    s => s.CropSeasonId == dto.CropSeasonId && !s.IsDeleted,
                    asNoTracking: true
                );
                if (season == null)
                    return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không tìm thấy mùa vụ tương ứng.");

                var commitmentDetail = await _unitOfWork.FarmingCommitmentsDetailRepository.GetByIdAsync(
                    cd => cd.CommitmentDetailId == dto.CommitmentDetailId && !cd.IsDeleted,
                    include: q => q.Include(cd => cd.PlanDetail).ThenInclude(p => p.CoffeeType),
                    asNoTracking: true
                );
                if (commitmentDetail == null)
                    return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không tìm thấy dòng cam kết tương ứng.");


                var entity = dto.MapToNewCropSeasonDetail();
                await _unitOfWork.CropSeasonDetailRepository.CreateAsync(entity);

                var result = await _unitOfWork.SaveChangesAsync();
                if (result > 0)
                {
                    var created = await _unitOfWork.CropSeasonDetailRepository.GetByIdAsync(
                        d => d.DetailId == entity.DetailId,
                        include: q => q
                            .Include(d => d.CommitmentDetail).ThenInclude(cd => cd.PlanDetail).ThenInclude(p => p.CoffeeType)
                            .Include(d => d.CropSeason).ThenInclude(cs => cs.Farmer).ThenInclude(f => f.User),
                        asNoTracking: true
                    );

                    var view = created?.MapToCropSeasonDetailViewDto();
                    return new ServiceResult(Const.SUCCESS_CREATE_CODE, Const.SUCCESS_CREATE_MSG, view);
                }

                return new ServiceResult(Const.FAIL_CREATE_CODE, "Tạo vùng trồng thất bại.");
            }
            catch (Exception ex)
            {
                return new ServiceResult(Const.ERROR_EXCEPTION, ex.Message);
            }
        }


        public async Task<IServiceResult> Update(CropSeasonDetailUpdateDto dto)
        {
            try
            {
                var existing = await _unitOfWork.CropSeasonDetailRepository.GetByIdAsync(
                    predicate: d => d.DetailId == dto.DetailId && !d.IsDeleted,
                    include: query => query
                        .Include(d => d.CommitmentDetail)
                            .ThenInclude(cd => cd.PlanDetail), // Để lấy CoffeeTypeId
                    asNoTracking: false
                );

                if (existing == null)
                {
                    return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không tìm thấy dòng mùa vụ.");
                }

                // 🔍 1. Lấy các vùng trồng khác trong cùng mùa vụ (trừ chính nó)
                var otherDetails = await _unitOfWork.CropSeasonDetailRepository.GetAllAsync(
                    predicate: d => d.CropSeasonId == existing.CropSeasonId
                                 && d.DetailId != dto.DetailId
                                 && !d.IsDeleted == false,
                    asNoTracking: true
                );

                double otherAllocated = otherDetails.Sum(d => d.AreaAllocated ?? 0);
                double newTotalAllocated = otherAllocated + (dto.AreaAllocated ?? 0);

                // 📦 2. Lấy mùa vụ để biết diện tích tối đa
                var cropSeason = await _unitOfWork.CropSeasonRepository.GetByIdAsync(
                    predicate: cs => cs.CropSeasonId == existing.CropSeasonId && !cs.IsDeleted,
                    asNoTracking: true
                );

                double maxArea = cropSeason?.Area ?? 0;

                // ❗ 3. Kiểm tra vượt tổng diện tích
                if (newTotalAllocated > maxArea)
                {
                    return new ServiceResult(
                        Const.FAIL_UPDATE_CODE,
                        $"Tổng diện tích phân bổ ({newTotalAllocated} ha) vượt quá diện tích đăng ký ({maxArea} ha). Vui lòng giảm diện tích."
                    );
                }

                // ✅ 4. Mapping dữ liệu mới vào entity hiện tại
                dto.MapToExistingEntity(existing);

                // 📈 5. Tính EstimatedYield = AreaAllocated * DefaultYieldPerHectare
                var coffeeType = await _unitOfWork.CoffeeTypeRepository.GetByIdAsync(
                    predicate: ct => ct.CoffeeTypeId == existing.CommitmentDetail.PlanDetail.CoffeeTypeId && !ct.IsDeleted,
                    asNoTracking: true
                );

                double defaultYieldPerHa = coffeeType?.DefaultYieldPerHectare ?? 0;
                existing.EstimatedYield = (existing.AreaAllocated ?? 0) * defaultYieldPerHa;

                // 💾 6. Lưu DB
                await _unitOfWork.CropSeasonDetailRepository.UpdateAsync(existing);
                var result = await _unitOfWork.SaveChangesAsync();

                if (result > 0)
                {
                    var updated = await _unitOfWork.CropSeasonDetailRepository.GetDetailWithIncludesAsync(dto.DetailId);
                    var view = updated?.MapToCropSeasonDetailViewDto();

                    return new ServiceResult(Const.SUCCESS_UPDATE_CODE, Const.SUCCESS_UPDATE_MSG, view);
                }

                return new ServiceResult(Const.FAIL_UPDATE_CODE, Const.FAIL_UPDATE_MSG);
            }
            catch (Exception ex)
            {
                return new ServiceResult(Const.ERROR_EXCEPTION, ex.ToString());
            }
        }



        public async Task<IServiceResult> DeleteById(Guid detailId)
        {
            try
            {
                var existing = await _unitOfWork.CropSeasonDetailRepository.GetByIdAsync(
                    predicate: d => d.DetailId == detailId && !d.IsDeleted,
                    asNoTracking: false
                );

                if (existing == null)
                {
                    return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không tìm thấy vùng trồng.");
                }

                // Lấy toàn bộ tiến độ liên quan (có thể rỗng)
                var progresses = await _unitOfWork.CropProgressRepository.FindAsync(
                    p => p.CropSeasonDetailId == detailId
                );

                // Xoá cứng tất cả tiến độ
                foreach (var progress in progresses)
                {
                    await _unitOfWork.CropProgressRepository.RemoveAsync(progress);
                }

                // Xoá cứng vùng trồng
                await _unitOfWork.CropSeasonDetailRepository.RemoveAsync(existing);

                var result = await _unitOfWork.SaveChangesAsync();

                return result > 0
                    ? new ServiceResult(Const.SUCCESS_DELETE_CODE, "Xoá vùng trồng và các tiến độ liên quan thành công.")
                    : new ServiceResult(Const.FAIL_DELETE_CODE, "Xoá thất bại.");
            }
            catch (Exception ex)
            {
                return new ServiceResult(Const.ERROR_EXCEPTION, ex.ToString());
            }
        }


        public async Task<IServiceResult> SoftDeleteById(Guid detailId)
        {
            try
            {
                var existing = await _unitOfWork.CropSeasonDetailRepository.GetByIdAsync(
                    predicate: d => d.DetailId == detailId && !d.IsDeleted,
                    asNoTracking: false
                );

                if (existing == null)
                {
                    return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không tìm thấy vùng trồng.");
                }

                // Lấy tất cả tiến độ liên quan
                var progresses = await _unitOfWork.CropProgressRepository.FindAsync(
                    p => p.CropSeasonDetailId == detailId && !p.IsDeleted
                );

                // Đánh dấu xoá mềm các tiến độ
                foreach (var progress in progresses)
                {
                    progress.IsDeleted = true;
                    progress.UpdatedAt = DateHelper.NowVietnamTime();
                    await _unitOfWork.CropProgressRepository.UpdateAsync(progress);
                }

                // Đánh dấu xoá mềm vùng trồng
                existing.IsDeleted = true;
                existing.UpdatedAt = DateHelper.NowVietnamTime();
                await _unitOfWork.CropSeasonDetailRepository.UpdateAsync(existing);

                var result = await _unitOfWork.SaveChangesAsync();

                return result > 0
                    ? new ServiceResult(Const.SUCCESS_DELETE_CODE, "Xoá mềm vùng trồng và các tiến độ liên quan thành công.")
                    : new ServiceResult(Const.FAIL_DELETE_CODE, "Xoá mềm thất bại.");
            }
            catch (Exception ex)
            {
                return new ServiceResult(Const.ERROR_EXCEPTION, ex.ToString());
            }
        }

    }
}
