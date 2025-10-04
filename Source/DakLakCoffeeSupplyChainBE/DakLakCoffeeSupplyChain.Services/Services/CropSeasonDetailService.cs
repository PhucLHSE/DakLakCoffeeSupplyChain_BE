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

        public async Task<IServiceResult> GetAll(Guid userId, bool isAdmin = false, bool isManager = false)
        {
            var details = await _uow.CropSeasonDetailRepository.GetAllAsync(
                predicate: d => !d.IsDeleted && (isAdmin || isManager || d.CropSeason.Farmer.UserId == userId),
                include: q => q
                    .Include(d => d.CommitmentDetail).ThenInclude(cd => cd.PlanDetail).ThenInclude(pd => pd.CoffeeType)
                    .Include(d => d.CommitmentDetail).ThenInclude(cd => cd.RegistrationDetail) // ✅ Add RegistrationDetail
                    .Include(d => d.CropSeason).ThenInclude(cs => cs.Farmer).ThenInclude(f => f.User)
                    .Include(d => d.Crop),
                asNoTracking: true
            );

            if (details == null || !details.Any())
                return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không có dòng mùa vụ nào.");

            var dtos = details.Select(d => d.MapToCropSeasonDetailViewDto()).ToList();
            return new ServiceResult(Const.SUCCESS_READ_CODE, Const.SUCCESS_READ_MSG, dtos);
        }

        public async Task<IServiceResult> GetById(Guid detailId, Guid userId, bool isAdmin = false, bool isManager = false)
        {
            var d = await _uow.CropSeasonDetailRepository.GetByIdAsync(
                predicate: x => x.DetailId == detailId && !x.IsDeleted,
                include: q => q
                    .Include(x => x.CommitmentDetail).ThenInclude(cd => cd.PlanDetail).ThenInclude(pd => pd.CoffeeType)
                    .Include(x => x.CommitmentDetail).ThenInclude(cd => cd.RegistrationDetail) // ✅ Add RegistrationDetail
                    .Include(x => x.CropSeason).ThenInclude(cs => cs.Farmer).ThenInclude(f => f.User)
                    .Include(x => x.Crop),
                asNoTracking: true
            );
            if (d == null) return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không tìm thấy chi tiết mùa vụ.");
            if (!isAdmin && !isManager && d.CropSeason?.Farmer?.UserId != userId)
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
            
            // Lấy ExpectedYield từ RegistrationDetail thay vì để mặc định = 0
            var registrationDetail = await _uow.CultivationRegistrationsDetailRepository.GetByIdAsync(
                rd => rd.PlanDetailId == cdetail.PlanDetailId && !rd.IsDeleted,
                asNoTracking: true
            );
            
            // Cập nhật EstimatedYield từ RegistrationDetail.ExpectedYield
            if (registrationDetail?.EstimatedYield.HasValue == true)
            {
                entity.EstimatedYield = registrationDetail.EstimatedYield.Value;
            }
            
            // Cập nhật CropId từ RegistrationDetail
            if (registrationDetail?.CropId.HasValue == true)
            {
                entity.CropId = registrationDetail.CropId.Value;
            }
            
            entity.CreatedAt = DateHelper.NowVietnamTime();
            entity.UpdatedAt = entity.CreatedAt;
            await _uow.CropSeasonDetailRepository.CreateAsync(entity);

            var saved = await _uow.SaveChangesAsync();
            if (saved <= 0) return new ServiceResult(Const.FAIL_CREATE_CODE, "Tạo vùng trồng thất bại.");

            var created = await _uow.CropSeasonDetailRepository.GetByIdAsync(
                d => d.DetailId == entity.DetailId,
                include: q => q
                    .Include(d => d.CommitmentDetail).ThenInclude(cd => cd.PlanDetail).ThenInclude(p => p.CoffeeType)
                    .Include(d => d.CropSeason).ThenInclude(cs => cs.Farmer).ThenInclude(f => f.User)
                    .Include(d => d.Crop),
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

            // map dữ liệu từ DTO
            dto.MapToExistingEntity(existing);
            
            // Lấy ExpectedYield từ RegistrationDetail thay vì tính toán
            var registrationDetail = await _uow.CultivationRegistrationsDetailRepository.GetByIdAsync(
                rd => rd.PlanDetailId == existing.CommitmentDetail.PlanDetailId && !rd.IsDeleted,
                asNoTracking: true
            );
            
            // Cập nhật EstimatedYield từ RegistrationDetail.ExpectedYield
            if (registrationDetail?.EstimatedYield.HasValue == true)
            {
                existing.EstimatedYield = registrationDetail.EstimatedYield.Value;
            }
            
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

        public async Task<IServiceResult> GetAvailableForWarehouseRequestAsync(Guid userId)
        {
            try
            {
                // Lấy thông tin farmer
                var farmer = await _uow.FarmerRepository.FindByUserIdAsync(userId);
                if (farmer == null)
                    return new ServiceResult(Const.FAIL_READ_CODE, "Không tìm thấy nông dân.");

                // Lấy tất cả crop season details đã hoàn thành của farmer này
                var completedDetails = await _uow.CropSeasonDetailRepository.GetAllAsync(
                    predicate: d => !d.IsDeleted && 
                                   d.CropSeason.FarmerId == farmer.FarmerId &&
                                   d.Status == "Completed" &&
                                   d.ActualYield.HasValue &&
                                   d.ActualYield.Value > 0,
                    include: q => q
                        .Include(d => d.CommitmentDetail).ThenInclude(cd => cd.PlanDetail).ThenInclude(pd => pd.CoffeeType)
                        .Include(d => d.CropSeason),
                    asNoTracking: true
                );


                // ✅ THÊM: Phân loại theo ràng buộc hợp đồng
                var freshCoffeeDetails = completedDetails.Where(d => 
                    d.CommitmentDetail?.PlanDetail?.ProcessMethodId == null || 
                    d.CommitmentDetail.PlanDetail.ProcessMethodId.Value <= 0
                ).ToList();

                var processedCoffeeDetails = completedDetails.Where(d => 
                    d.CommitmentDetail?.PlanDetail?.ProcessMethodId.HasValue == true && 
                    d.CommitmentDetail.PlanDetail.ProcessMethodId.Value > 0
                ).ToList();


                // ✅ THÊM: Thông báo rõ ràng cho từng trường hợp
                if (!completedDetails.Any())
                {
                    return new ServiceResult(Const.WARNING_NO_DATA_CODE, 
                        "Không có vùng trồng nào đã hoàn thành để tạo yêu cầu nhập kho.", 
                        new List<object>());
                }

                if (!freshCoffeeDetails.Any() && processedCoffeeDetails.Any())
                {
                    return new ServiceResult(Const.WARNING_NO_DATA_CODE, 
                        "Cam kết sơ chế, vui lòng gửi hàng sơ chế hoặc nếu chưa có hãy tạo lô sơ chế trước khi gửi yêu cầu nhập kho.", 
                        new List<object>());
                }

                // Tính toán available quantity cho mỗi detail
                var result = new List<object>();
                foreach (var detail in freshCoffeeDetails)
                {
                    // Lấy tất cả inbound requests đã được xử lý cho detail này
                    var allRequests = await _uow.WarehouseInboundRequests.GetAllAsync(
                        r => r.DetailId == detail.DetailId && !r.IsDeleted
                    );

                    // Tính tổng đã yêu cầu
                    double totalRequested = allRequests
                        .Where(r => r.Status == "Completed" || r.Status == "Pending" || r.Status == "Approved")
                        .Sum(r => r.RequestedQuantity ?? 0);

                    // Tính available quantity
                    double availableQuantity = Math.Max(0, (detail.ActualYield ?? 0) - totalRequested);

                    // Chỉ trả về detail có available quantity > 0
                    if (availableQuantity > 0)
                    {
                        result.Add(new
                        {
                            detailId = detail.DetailId,
                            detailCode = detail.CropSeason?.SeasonName ?? "N/A",
                            coffeeTypeName = detail.CommitmentDetail?.PlanDetail?.CoffeeType?.TypeName ?? "N/A",
                            cropSeasonName = detail.CropSeason?.SeasonName ?? "N/A",
                            actualYield = detail.ActualYield ?? 0,
                            totalRequested = totalRequested,
                            availableQuantity = availableQuantity,
                            availableQuantityText = $"{availableQuantity} kg"
                        });
                    }
                }

                return new ServiceResult(Const.SUCCESS_READ_CODE, 
                    $"Đã tìm thấy {result.Count} vùng trồng cà phê tươi có thể tạo yêu cầu nhập kho", 
                    result);
            }
            catch (Exception ex)
            {
                return new ServiceResult(Const.ERROR_EXCEPTION, $"Lỗi: {ex.Message}");
            }
        }
    }
}
