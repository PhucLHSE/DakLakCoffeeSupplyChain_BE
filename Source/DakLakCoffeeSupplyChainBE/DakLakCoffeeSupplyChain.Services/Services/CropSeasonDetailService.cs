using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.CropSeasonDetailDTOs;
using DakLakCoffeeSupplyChain.Common.Enum.CropSeasonEnums;
using DakLakCoffeeSupplyChain.Common.Helpers;
using DakLakCoffeeSupplyChain.Repositories.UnitOfWork;
using DakLakCoffeeSupplyChain.Services.Base;
using DakLakCoffeeSupplyChain.Services.IServices;
using DakLakCoffeeSupplyChain.Services.Mappers;
using Microsoft.EntityFrameworkCore;

namespace DakLakCoffeeSupplyChain.Services.Services
{
    public class CropSeasonDetailService : ICropSeasonDetailService
    {
        private readonly IUnitOfWork _uow;
        private readonly ICropSeasonService _cropSeasonService;

        public CropSeasonDetailService(IUnitOfWork uow, ICropSeasonService cropSeasonService)
        {
            _uow = uow ?? throw new ArgumentNullException(nameof(uow));
            _cropSeasonService = cropSeasonService ?? throw new ArgumentNullException(nameof(cropSeasonService));
        }

        public async Task<IServiceResult> GetAll(Guid userId, bool isAdmin = false)
        {
            var details = await _uow.CropSeasonDetailRepository.GetAllAsync(
                predicate: d => !d.IsDeleted && (isAdmin || d.CropSeason.Farmer.UserId == userId),
                include: q => q
                    .Include(d => d.CommitmentDetail).ThenInclude(cd => cd.PlanDetail)
                    .Include(d => d.CropSeason).ThenInclude(cs => cs.Farmer).ThenInclude(f => f.User),
                asNoTracking: true
            );

            if (details == null || !details.Any())
                return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không có dòng mùa vụ nào.");

            var dtos = details.Select(d => d.MapToCropSeasonDetailViewDto()).ToList();
            return new ServiceResult(Const.SUCCESS_READ_CODE, Const.SUCCESS_READ_MSG, dtos);
        }

        public async Task<IServiceResult> GetById(Guid detailId, Guid userId, bool isAdmin = false)
        {
            var d = await _uow.CropSeasonDetailRepository.GetByIdAsync(
                predicate: x => x.DetailId == detailId && !x.IsDeleted,
                include: q => q
                    .Include(x => x.CommitmentDetail).ThenInclude(cd => cd.PlanDetail)
                    .Include(x => x.CropSeason).ThenInclude(cs => cs.Farmer).ThenInclude(f => f.User),
                asNoTracking: true
            );
            if (d == null) return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không tìm thấy chi tiết mùa vụ.");
            if (!isAdmin && d.CropSeason?.Farmer?.UserId != userId)
                return new ServiceResult(Const.FAIL_READ_CODE, "Bạn không có quyền xem chi tiết này.");

            return new ServiceResult(Const.SUCCESS_READ_CODE, Const.SUCCESS_READ_MSG, d.MapToCropSeasonDetailViewDto());
        }

        public async Task<IServiceResult> Create(CropSeasonDetailCreateDto dto, Guid userId, bool isAdmin = false)
        {
            // owner check
            var season = await _uow.CropSeasonRepository.GetByIdAsync(
                s => s.CropSeasonId == dto.CropSeasonId && !s.IsDeleted,
                include: q => q.Include(s => s.Farmer),
                asNoTracking: true
            );
            if (season == null) return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không tìm thấy mùa vụ.");
            if (!isAdmin && season.Farmer?.UserId != userId)
                return new ServiceResult(Const.FAIL_CREATE_CODE, "Bạn không có quyền thêm vùng trồng cho mùa vụ này.");

            // commitment detail must exist & belong to same commitment via season.CommitmentId
            var cdetail = await _uow.FarmingCommitmentsDetailRepository.GetByIdAsync(
                cd => cd.CommitmentDetailId == dto.CommitmentDetailId && !cd.IsDeleted,
                include: q => q.Include(cd => cd.PlanDetail).ThenInclude(pd => pd.CoffeeType),
                asNoTracking: true
            );
            if (cdetail == null) return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không tìm thấy dòng cam kết.");
            if (cdetail.CommitmentId != season.CommitmentId)
                return new ServiceResult(Const.FAIL_CREATE_CODE, "Chi tiết cam kết không thuộc cam kết của mùa vụ.");

            // date in season range (nếu có Expected ở DTO)
            if (dto.ExpectedHarvestStart.HasValue && dto.ExpectedHarvestEnd.HasValue)
            {
                if (dto.ExpectedHarvestStart.Value > dto.ExpectedHarvestEnd.Value)
                    return new ServiceResult(Const.FAIL_CREATE_CODE, "ExpectedHarvestStart phải <= ExpectedHarvestEnd.");
                if (dto.ExpectedHarvestStart.Value < season.StartDate || dto.ExpectedHarvestEnd.Value > season.EndDate)
                    return new ServiceResult(Const.FAIL_CREATE_CODE, "Khoảng thu hoạch dự kiến nằm ngoài thời gian mùa vụ.");
            }

            // area constraint: other details + this new must <= season.Area
            var siblings = await _uow.CropSeasonDetailRepository.GetAllAsync(
                d => d.CropSeasonId == dto.CropSeasonId && !d.IsDeleted, asNoTracking: true);
            double otherAllocated = siblings.Sum(d => d.AreaAllocated ?? 0);
            double newTotal = otherAllocated + (dto.AreaAllocated ?? 0);
            double maxArea = season.Area ?? 0;
            if (newTotal > maxArea)
                return new ServiceResult(Const.FAIL_CREATE_CODE, $"Tổng diện tích phân bổ ({newTotal} ha) vượt quá {maxArea} ha.");

            var entity = dto.MapToNewCropSeasonDetail();
            entity.CreatedAt = DateHelper.NowVietnamTime();
            entity.UpdatedAt = entity.CreatedAt;
            await _uow.CropSeasonDetailRepository.CreateAsync(entity);

            var saved = await _uow.SaveChangesAsync();
            if (saved <= 0) return new ServiceResult(Const.FAIL_CREATE_CODE, "Tạo vùng trồng thất bại.");

            var created = await _uow.CropSeasonDetailRepository.GetByIdAsync(
                d => d.DetailId == entity.DetailId,
                include: q => q
                    .Include(d => d.CommitmentDetail).ThenInclude(cd => cd.PlanDetail).ThenInclude(p => p.CoffeeType)
                    .Include(d => d.CropSeason).ThenInclude(cs => cs.Farmer).ThenInclude(f => f.User),
                asNoTracking: true
            );

            return new ServiceResult(Const.SUCCESS_CREATE_CODE, Const.SUCCESS_CREATE_MSG, created?.MapToCropSeasonDetailViewDto());
        }

        public async Task<IServiceResult> Update(CropSeasonDetailUpdateDto dto, Guid userId, bool isAdmin = false)
        {
            var existing = await _uow.CropSeasonDetailRepository.GetByIdAsync(
                predicate: d => d.DetailId == dto.DetailId && !d.IsDeleted,
                include: q => q
                    .Include(d => d.CommitmentDetail).ThenInclude(cd => cd.PlanDetail),
                asNoTracking: false
            );
            if (existing == null) return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không tìm thấy vùng trồng.");

            var season = await _uow.CropSeasonRepository.GetByIdAsync(
                cs => cs.CropSeasonId == existing.CropSeasonId && !cs.IsDeleted,
                include: q => q.Include(cs => cs.Farmer),
                asNoTracking: true
            );
            if (season == null) return new ServiceResult(Const.FAIL_UPDATE_CODE, "Không tìm thấy mùa vụ.");
            if (!isAdmin && season.Farmer?.UserId != userId)
                return new ServiceResult(Const.FAIL_UPDATE_CODE, "Bạn không có quyền cập nhật vùng trồng này.");

            // area sum rule
            var others = await _uow.CropSeasonDetailRepository.GetAllAsync(
                d => d.CropSeasonId == existing.CropSeasonId && d.DetailId != dto.DetailId && !d.IsDeleted,
                asNoTracking: true
            );
            double otherAllocated = others.Sum(d => d.AreaAllocated ?? 0);
            double newTotal = otherAllocated + (dto.AreaAllocated ?? 0);
            double maxArea = season.Area ?? 0;
            if (newTotal > maxArea)
                return new ServiceResult(Const.FAIL_UPDATE_CODE, $"Tổng diện tích phân bổ ({newTotal} ha) vượt quá {maxArea} ha.");

            // expected harvest range (if provided)
            if (dto.ExpectedHarvestStart.HasValue && dto.ExpectedHarvestEnd.HasValue)
            {
                if (dto.ExpectedHarvestStart.Value > dto.ExpectedHarvestEnd.Value)
                    return new ServiceResult(Const.FAIL_UPDATE_CODE, "ExpectedHarvestStart phải <= ExpectedHarvestEnd.");
                if (dto.ExpectedHarvestStart.Value < season.StartDate || dto.ExpectedHarvestEnd.Value > season.EndDate)
                    return new ServiceResult(Const.FAIL_UPDATE_CODE, "Khoảng thu hoạch dự kiến nằm ngoài thời gian mùa vụ.");
            }

            // map & recalc EstimatedYield by CoffeeType.DefaultYieldPerHectare
            dto.MapToExistingEntity(existing);
            var coffeeType = await _uow.CoffeeTypeRepository.GetByIdAsync(
                ct => ct.CoffeeTypeId == existing.CommitmentDetail.PlanDetail.CoffeeTypeId && !ct.IsDeleted,
                asNoTracking: true
            );
            double defaultYieldPerHa = coffeeType?.DefaultYieldPerHectare ?? 0;
            existing.EstimatedYield = (existing.AreaAllocated ?? 0) * defaultYieldPerHa;
            existing.UpdatedAt = DateHelper.NowVietnamTime();

            await _uow.CropSeasonDetailRepository.UpdateAsync(existing);
            var saved = await _uow.SaveChangesAsync();
            if (saved <= 0) return new ServiceResult(Const.FAIL_UPDATE_CODE, Const.FAIL_UPDATE_MSG);

            var updated = await _uow.CropSeasonDetailRepository.GetDetailWithIncludesAsync(dto.DetailId);
            return new ServiceResult(Const.SUCCESS_UPDATE_CODE, Const.SUCCESS_UPDATE_MSG, updated?.MapToCropSeasonDetailViewDto());
        }

        public async Task<IServiceResult> DeleteById(Guid detailId, Guid userId, bool isAdmin = false)
        {
            var existing = await _uow.CropSeasonDetailRepository.GetByIdAsync(
                predicate: d => d.DetailId == detailId && !d.IsDeleted,
                include: q => q.Include(d => d.CropSeason).ThenInclude(cs => cs.Farmer),
                asNoTracking: false
            );
            if (existing == null) return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không tìm thấy vùng trồng.");
            if (!isAdmin && existing.CropSeason?.Farmer?.UserId != userId)
                return new ServiceResult(Const.FAIL_DELETE_CODE, "Bạn không có quyền xoá vùng trồng này.");

            // remove progresses hard
            var progresses = await _uow.CropProgressRepository.FindAsync(p => p.CropSeasonDetailId == detailId);
            foreach (var p in progresses) await _uow.CropProgressRepository.RemoveAsync(p);

            await _uow.CropSeasonDetailRepository.RemoveAsync(existing);
            var saved = await _uow.SaveChangesAsync();

            // re-evaluate season status
            await _cropSeasonService.AutoUpdateCropSeasonStatusAsync(existing.CropSeasonId);

            return saved > 0
                ? new ServiceResult(Const.SUCCESS_DELETE_CODE, "Xoá vùng trồng và tiến độ liên quan thành công.")
                : new ServiceResult(Const.FAIL_DELETE_CODE, "Xoá thất bại.");
        }

        public async Task<IServiceResult> SoftDeleteById(Guid detailId, Guid userId, bool isAdmin = false)
        {
            var existing = await _uow.CropSeasonDetailRepository.GetByIdAsync(
                predicate: d => d.DetailId == detailId && !d.IsDeleted,
                include: q => q.Include(d => d.CropSeason).ThenInclude(cs => cs.Farmer),
                asNoTracking: false
            );
            if (existing == null) return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không tìm thấy vùng trồng.");
            if (!isAdmin && existing.CropSeason?.Farmer?.UserId != userId)
                return new ServiceResult(Const.FAIL_DELETE_CODE, "Bạn không có quyền xoá mềm vùng trồng này.");

            var progresses = await _uow.CropProgressRepository.FindAsync(p => p.CropSeasonDetailId == detailId && !p.IsDeleted);
            foreach (var p in progresses)
            {
                p.IsDeleted = true;
                p.UpdatedAt = DateHelper.NowVietnamTime();
                await _uow.CropProgressRepository.UpdateAsync(p);
            }

            existing.IsDeleted = true;
            existing.UpdatedAt = DateHelper.NowVietnamTime();
            await _uow.CropSeasonDetailRepository.UpdateAsync(existing);

            var saved = await _uow.SaveChangesAsync();

            await _cropSeasonService.AutoUpdateCropSeasonStatusAsync(existing.CropSeasonId);

            return saved > 0
                ? new ServiceResult(Const.SUCCESS_DELETE_CODE, "Xoá mềm vùng trồng và tiến độ liên quan thành công.")
                : new ServiceResult(Const.FAIL_DELETE_CODE, "Xoá mềm thất bại.");
        }

        public async Task<IServiceResult> UpdateStatusAsync(Guid detailId, CropDetailStatus newStatus, Guid userId, bool isAdmin = false)
        {
            var d = await _uow.CropSeasonDetailRepository.GetByIdAsync(
                predicate: x => x.DetailId == detailId && !x.IsDeleted,
                include: q => q.Include(x => x.CropSeason).ThenInclude(cs => cs.Farmer),
                asNoTracking: false
            );
            if (d == null) return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không tìm thấy vùng trồng.");
            if (!isAdmin && d.CropSeason?.Farmer?.UserId != userId)
                return new ServiceResult(Const.FAIL_UPDATE_CODE, "Bạn không có quyền cập nhật trạng thái vùng trồng này.");

            if (!Enum.TryParse(d.Status, out CropDetailStatus current))
                return new ServiceResult(Const.FAIL_UPDATE_CODE, "Trạng thái hiện tại không hợp lệ.");

            if (!IsValidDetailStatusTransition(current, newStatus))
                return new ServiceResult(Const.FAIL_UPDATE_CODE, $"Không thể chuyển từ {current} sang {newStatus}.");

            d.Status = newStatus.ToString();
            d.UpdatedAt = DateHelper.NowVietnamTime();
            await _uow.CropSeasonDetailRepository.UpdateAsync(d);

            var saved = await _uow.SaveChangesAsync();
            await _cropSeasonService.AutoUpdateCropSeasonStatusAsync(d.CropSeasonId);

            return saved > 0
                ? new ServiceResult(Const.SUCCESS_UPDATE_CODE, $"Cập nhật trạng thái vùng trồng thành công: {newStatus}")
                : new ServiceResult(Const.FAIL_UPDATE_CODE, "Cập nhật trạng thái vùng trồng thất bại.");
        }

        public async Task<IServiceResult> AutoUpdateStatusBasedOnProgressAsync(Guid cropSeasonDetailId)
        {
            try
            {
                var detail = await _uow.CropSeasonDetailRepository.GetByIdAsync(
                    predicate: d => d.DetailId == cropSeasonDetailId && !d.IsDeleted,
                    asNoTracking: false
                );

                if (detail == null)
                    return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không tìm thấy vùng trồng.");

                // Lấy tất cả progress của vùng trồng này
                var progresses = await _uow.CropProgressRepository.GetAllAsync(
                    predicate: p => p.CropSeasonDetailId == cropSeasonDetailId && !p.IsDeleted,
                    include: q => q.Include(p => p.Stage),
                    asNoTracking: true
                );

                if (!progresses.Any())
                    return new ServiceResult(Const.SUCCESS_UPDATE_CODE, "Chưa có tiến độ nào.");

                // Parse current status
                if (!Enum.TryParse<CropDetailStatus>(detail.Status, out var currentStatus))
                {
                    currentStatus = CropDetailStatus.Planned;
                }

                CropDetailStatus newStatus = currentStatus;

                // Logic update status:
                // 1. Nếu có progress đầu tiên và đang Planned -> InProgress
                if (currentStatus == CropDetailStatus.Planned && progresses.Count() == 1)
                {
                    newStatus = CropDetailStatus.InProgress;
                }
                // 2. Nếu có progress thu hoạch và có ActualYield -> Completed
                else if (currentStatus == CropDetailStatus.InProgress)
                {
                    var hasHarvestProgress = progresses.Any(p => p.Stage?.StageCode == "harvesting");
                    if (hasHarvestProgress && detail.ActualYield.HasValue && detail.ActualYield.Value > 0)
                    {
                        newStatus = CropDetailStatus.Completed;
                    }
                }

                // Update status nếu có thay đổi
                if (newStatus != currentStatus)
                {
                    detail.Status = newStatus.ToString();
                    detail.UpdatedAt = DateHelper.NowVietnamTime();
                    await _uow.CropSeasonDetailRepository.UpdateAsync(detail);
                    await _uow.SaveChangesAsync();

                    // Auto update CropSeason status
                    await _cropSeasonService.AutoUpdateCropSeasonStatusAsync(detail.CropSeasonId);

                    return new ServiceResult(Const.SUCCESS_UPDATE_CODE, 
                        $"Cập nhật trạng thái vùng trồng thành công: {newStatus}");
                }

                return new ServiceResult(Const.SUCCESS_UPDATE_CODE, "Không cần cập nhật trạng thái.");
            }
            catch (Exception ex)
            {
                return new ServiceResult(Const.ERROR_EXCEPTION, $"Lỗi cập nhật trạng thái: {ex.Message}");
            }
        }

        private bool IsValidDetailStatusTransition(CropDetailStatus current, CropDetailStatus next)
        {
            return (current, next) switch
            {
                (CropDetailStatus.Planned, CropDetailStatus.InProgress) => true,
                (CropDetailStatus.InProgress, CropDetailStatus.Completed) => true,
                (CropDetailStatus.Planned, CropDetailStatus.Cancelled) => true,
                (CropDetailStatus.InProgress, CropDetailStatus.Cancelled) => true,
                _ => false
            };
        }
    }
}
