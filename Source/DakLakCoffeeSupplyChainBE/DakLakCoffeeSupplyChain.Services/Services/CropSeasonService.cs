using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.CropSeasonDTOs;
using DakLakCoffeeSupplyChain.Common.Enum.CropSeasonEnums;
using DakLakCoffeeSupplyChain.Common.Enum.FarmingCommitmentEnums;
using DakLakCoffeeSupplyChain.Common.Helpers;
using DakLakCoffeeSupplyChain.Repositories.Models;
using DakLakCoffeeSupplyChain.Repositories.UnitOfWork;
using DakLakCoffeeSupplyChain.Services.Base;
using DakLakCoffeeSupplyChain.Services.Generators;
using DakLakCoffeeSupplyChain.Services.Mappers;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

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
            var predicate = isAdmin || isManager
                ? (Expression<Func<CropSeason, bool>>)(cs => !cs.IsDeleted)
                : (cs => cs.Farmer.UserId == userId && !cs.IsDeleted);

            var cropSeasons = await _unitOfWork.CropSeasonRepository.GetAllAsync(
                predicate: predicate,
                include: q => q.Include(cs => cs.Farmer).ThenInclude(f => f.User),
                orderBy: q => q.OrderByDescending(cs => cs.StartDate),
                asNoTracking: true
            );

            if (!cropSeasons.Any())
                return new ServiceResult(Const.WARNING_NO_DATA_CODE, Const.WARNING_NO_DATA_MSG);

            var dtoList = cropSeasons.Select(cs => cs.MapToCropSeasonViewAllDto()).ToList();
            return new ServiceResult(Const.SUCCESS_READ_CODE, Const.SUCCESS_READ_MSG, dtoList);
        }

        public async Task<IServiceResult> GetById(Guid cropSeasonId, Guid userId, bool isAdmin = false, bool isManager = false)
        {
            var cropSeason = await _unitOfWork.CropSeasonRepository.GetByIdAsync(
                predicate: cs => cs.CropSeasonId == cropSeasonId && !cs.IsDeleted,
                include: query => query
                    .Include(cs => cs.Farmer).ThenInclude(f => f.User)
                    .Include(cs => cs.CropSeasonDetails)
                        .ThenInclude(d => d.CommitmentDetail).ThenInclude(cd => cd.PlanDetail).ThenInclude(pd => pd.CoffeeType)
                    .Include(cs => cs.Commitment)
                    .Include(cs => cs.Commitment).ThenInclude(c => c.Registration),
                asNoTracking: true
            );

            if (cropSeason == null)
                return new ServiceResult(Const.WARNING_NO_DATA_CODE, Const.WARNING_NO_DATA_MSG);

            if (!isAdmin && !isManager && cropSeason.Farmer?.UserId != userId)
                return new ServiceResult(Const.FAIL_READ_CODE, "Bạn không có quyền truy cập mùa vụ này.");

            var registration = cropSeason.Commitment?.Registration;
            var dto = cropSeason.MapToCropSeasonViewDetailsDto(registration);
            return new ServiceResult(Const.SUCCESS_READ_CODE, Const.SUCCESS_READ_MSG, dto);
        }

        public async Task<IServiceResult> Create(CropSeasonCreateDto dto, Guid userId)
        {
            var commitment = await _unitOfWork.FarmingCommitmentRepository.GetByIdAsync(
                predicate: c => c.CommitmentId == dto.CommitmentId && !c.IsDeleted,
                asNoTracking: true
            );
            if (commitment == null)
                return new ServiceResult(Const.FAIL_CREATE_CODE, "Không tìm thấy cam kết canh tác.");

            var farmer = await _unitOfWork.FarmerRepository.GetByIdAsync(
                f => f.FarmerId == commitment.FarmerId && !f.IsDeleted
            );
            if (farmer == null)
                return new ServiceResult(Const.FAIL_CREATE_CODE, "Không tìm thấy nông hộ tương ứng.");

            if (farmer.UserId != userId)
                return new ServiceResult(Const.FAIL_CREATE_CODE, "Cam kết không thuộc về bạn.");

            if (!string.Equals(commitment.Status, FarmingCommitmentStatus.Active.ToString(), StringComparison.OrdinalIgnoreCase))
                return new ServiceResult(Const.FAIL_CREATE_CODE, "Cam kết chưa được duyệt hoặc không hợp lệ.");

            var existingSeasons = await _unitOfWork.CropSeasonRepository.GetAllAsync(
                x => x.CommitmentId == dto.CommitmentId && !x.IsDeleted,
                include: q => q.Include(cs => cs.CropSeasonDetails),
                asNoTracking: true
            );

            if (dto.StartDate >= dto.EndDate)
                return new ServiceResult(Const.FAIL_CREATE_CODE, "Ngày bắt đầu phải trước ngày kết thúc.");

            bool overlaps = existingSeasons.Any(cs =>
                (dto.StartDate < cs.EndDate) && (dto.EndDate > cs.StartDate) && 
                (dto.StartDate != cs.EndDate) && (dto.EndDate != cs.StartDate));
            if (overlaps)
                return new ServiceResult(Const.FAIL_CREATE_CODE, "Thời gian mùa vụ trùng với một mùa vụ khác trong cùng cam kết.");

            string code = await _codeGenerator.GenerateCropSeasonCodeAsync(dto.StartDate.Year);
            Guid cropSeasonId = Guid.NewGuid();

            var registration = await _unitOfWork.CultivationRegistrationRepository.GetByIdAsync(
                predicate: r => r.RegistrationId == commitment.RegistrationId && !r.IsDeleted,
                asNoTracking: true
            );
            if (registration == null)
                return new ServiceResult(Const.FAIL_CREATE_CODE, "Không tìm thấy đơn đăng ký canh tác.");

            var cropSeason = dto.MapToCropSeasonCreateDto(code, farmer.FarmerId, cropSeasonId);
            cropSeason.Area = registration.RegisteredArea ?? 0;
            cropSeason.CommitmentId = commitment.CommitmentId;

            await _unitOfWork.CropSeasonRepository.CreateAsync(cropSeason);

            var commitmentDetails = await _unitOfWork.FarmingCommitmentsDetailRepository.GetAllAsync(
                predicate: cd => cd.CommitmentId == commitment.CommitmentId && !cd.IsDeleted,
                include: q => q.Include(cd => cd.PlanDetail).ThenInclude(pd => pd.CoffeeType),
                asNoTracking: true
            );
            if (!commitmentDetails.Any())
                return new ServiceResult(Const.FAIL_CREATE_CODE, "Không có chi tiết cam kết để tạo vùng trồng.");

            foreach (var detail in commitmentDetails)
            {
                var seasonDetail = new CropSeasonDetail
                {
                    DetailId = Guid.NewGuid(),
                    CropSeasonId = cropSeason.CropSeasonId,
                    CommitmentDetailId = detail.CommitmentDetailId,
                    ExpectedHarvestStart = detail.EstimatedDeliveryStart ?? dto.StartDate,
                    ExpectedHarvestEnd = detail.EstimatedDeliveryEnd ?? dto.EndDate,
                    AreaAllocated = 0,
                    EstimatedYield = 0,
                    PlannedQuality = null,
                    QualityGrade = null,
                    Status = CropDetailStatus.Planned.ToString(),
                    CreatedAt = DateHelper.NowVietnamTime(),
                    UpdatedAt = DateHelper.NowVietnamTime(),
                    IsDeleted = false
                };

                await _unitOfWork.CropSeasonDetailRepository.CreateAsync(seasonDetail);
            }

            var result = await _unitOfWork.SaveChangesAsync();
            if (result > 0)
            {
                var fullEntity = await _unitOfWork.CropSeasonRepository.GetWithDetailsByIdAsync(cropSeason.CropSeasonId);
                if (fullEntity == null)
                    return new ServiceResult(Const.FAIL_CREATE_CODE, "Tạo mùa vụ thành công nhưng không lấy được dữ liệu.");

                var responseDto = fullEntity.MapToCropSeasonViewDetailsDto(registration);
                return new ServiceResult(Const.SUCCESS_CREATE_CODE, Const.SUCCESS_CREATE_MSG, responseDto);
            }

            return new ServiceResult(Const.FAIL_CREATE_CODE, Const.FAIL_CREATE_MSG);
        }

        public async Task<IServiceResult> Update(CropSeasonUpdateDto dto, Guid userId, bool isAdmin = false)
        {
            var cropSeason = await _unitOfWork.CropSeasonRepository.GetWithDetailsByIdForUpdateAsync(dto.CropSeasonId);
            if (cropSeason == null)
                return new ServiceResult(Const.FAIL_UPDATE_CODE, Const.WARNING_NO_DATA_MSG);

            if (!isAdmin && cropSeason.Farmer?.UserId != userId)
                return new ServiceResult(Const.FAIL_UPDATE_CODE, "Bạn không có quyền cập nhật mùa vụ này.");

            var commitment = await _unitOfWork.FarmingCommitmentRepository.GetByIdAsync(
                c => c.CommitmentId == cropSeason.CommitmentId && !c.IsDeleted,
                asNoTracking: true
            );
            if (commitment == null)
                return new ServiceResult(Const.FAIL_UPDATE_CODE, "Cam kết canh tác không hợp lệ hoặc không tồn tại.");

            if (!string.Equals(commitment.Status, FarmingCommitmentStatus.Active.ToString(), StringComparison.OrdinalIgnoreCase))
                return new ServiceResult(Const.FAIL_UPDATE_CODE, "Cam kết chưa được duyệt hoặc không hợp lệ.");

            var overlapping = await _unitOfWork.CropSeasonRepository.GetAllAsync(
                x => x.CommitmentId == cropSeason.CommitmentId
                    && x.CropSeasonId != dto.CropSeasonId
                    && !x.IsDeleted
                    && dto.StartDate < x.EndDate && dto.EndDate > x.StartDate
                    && dto.StartDate != x.EndDate && dto.EndDate != x.StartDate,
                asNoTracking: true
            );
            if (overlapping.Any())
                return new ServiceResult(Const.FAIL_UPDATE_CODE, "Thời gian mùa vụ trùng với mùa vụ khác trong cam kết.");

            if (dto.StartDate >= dto.EndDate)
                return new ServiceResult(Const.FAIL_UPDATE_CODE, "Ngày bắt đầu phải trước ngày kết thúc.");

            dto.MapToExistingEntity(cropSeason);
            cropSeason.UpdatedAt = DateHelper.NowVietnamTime();

            // tránh vòng lặp tracking sâu
            foreach (var detail in cropSeason.CropSeasonDetails)
            {
                if (detail?.CommitmentDetail?.PlanDetail?.CoffeeType != null)
                {
                    detail.CommitmentDetail.PlanDetail.CoffeeType = null;
                }
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

            if (!isAdmin && cropSeason.Farmer?.UserId != userId)
                return new ServiceResult(Const.FAIL_DELETE_CODE, "Bạn không có quyền xoá mùa vụ này.");

            if (cropSeason.Status != CropSeasonStatus.Cancelled.ToString())
                return new ServiceResult(Const.FAIL_DELETE_CODE, "Chỉ có thể xoá mùa vụ đã huỷ.");

            if (cropSeason.CropSeasonDetails != null && cropSeason.CropSeasonDetails.Any())
            {
                foreach (var detail in cropSeason.CropSeasonDetails)
                {
                    await _unitOfWork.CropSeasonDetailRepository.RemoveAsync(detail);
                }
            }

            _unitOfWork.CropSeasonRepository.PrepareRemove(cropSeason);
            var result = await _unitOfWork.SaveChangesAsync();

            return result > 0
                ? new ServiceResult(Const.SUCCESS_DELETE_CODE, "Xoá mùa vụ và toàn bộ vùng trồng liên quan thành công.")
                : new ServiceResult(Const.FAIL_DELETE_CODE, "Xoá mùa vụ thất bại.");
        }

        // ====== ĐÃ SỬA GỌN, TRÁNH LỖI TRACKING/SEVERED RELATIONSHIP ======
        public async Task<IServiceResult> SoftDeleteAsync(Guid cropSeasonId, Guid userId, bool isAdmin)
        {
            // Chỉ load Farmer và danh sách Details (không ThenInclude sâu)
            var cropSeason = await _unitOfWork.CropSeasonRepository.GetByIdAsync(
                predicate: cs => cs.CropSeasonId == cropSeasonId && !cs.IsDeleted,
                include: q => q
                    .Include(cs => cs.Farmer)
                    .Include(cs => cs.CropSeasonDetails),
                asNoTracking: false // cần tracking để set cờ IsDeleted
            );

            if (cropSeason == null)
                return new ServiceResult(Const.WARNING_NO_DATA_CODE, Const.WARNING_NO_DATA_MSG);

            if (!isAdmin && cropSeason.Farmer?.UserId != userId)
                return new ServiceResult(Const.FAIL_DELETE_CODE, "Bạn không có quyền xoá mùa vụ này.");

            var now = DateHelper.NowVietnamTime();

            // Đánh dấu IsDeleted cho từng vùng trồng (KHÔNG đụng tới CommitmentDetail để tránh attach trùng)
            if (cropSeason.CropSeasonDetails != null && cropSeason.CropSeasonDetails.Any())
            {
                foreach (var d in cropSeason.CropSeasonDetails)
                {
                    d.IsDeleted = true;
                    d.UpdatedAt = now;
                    await _unitOfWork.CropSeasonDetailRepository.UpdateAsync(d);
                }
            }

            // Đánh dấu IsDeleted cho mùa vụ
            cropSeason.IsDeleted = true;
            cropSeason.UpdatedAt = now;
            await _unitOfWork.CropSeasonRepository.UpdateAsync(cropSeason);

            var result = await _unitOfWork.SaveChangesAsync();

            return result > 0
                ? new ServiceResult(Const.SUCCESS_DELETE_CODE, "Xoá mềm mùa vụ và toàn bộ vùng trồng thành công.")
                : new ServiceResult(Const.FAIL_DELETE_CODE, "Xoá mềm mùa vụ thất bại.");
        }
        // ================================================================

        public async Task AutoUpdateCropSeasonStatusAsync(Guid cropSeasonId)
        {
            var cropSeason = await _unitOfWork.CropSeasonRepository.GetWithDetailsByIdAsync(cropSeasonId);
            if (cropSeason == null || cropSeason.IsDeleted) return;

            var allDetails = cropSeason.CropSeasonDetails?.Where(d => !d.IsDeleted).ToList();
            if (allDetails == null || !allDetails.Any()) return;

            // Đếm số lượng details theo từng status
            var completedCount = allDetails.Count(d => d.Status == CropDetailStatus.Completed.ToString());
            var inProgressCount = allDetails.Count(d => d.Status == CropDetailStatus.InProgress.ToString());
            var cancelledCount = allDetails.Count(d => d.Status == CropDetailStatus.Cancelled.ToString());
            var plannedCount = allDetails.Count(d => d.Status == CropDetailStatus.Planned.ToString());
            
            var totalDetails = allDetails.Count;

            // Parse current status
            CropSeasonStatus currentStatus;
            if (!Enum.TryParse<CropSeasonStatus>(cropSeason.Status, out currentStatus))
            {
                currentStatus = CropSeasonStatus.Active;
            }

            CropSeasonStatus? newStatus = null;

            // Logic chuyển đổi status:
            // 1. Nếu tất cả details đã Completed -> Completed
            if (completedCount == totalDetails && currentStatus != CropSeasonStatus.Completed)
            {
                newStatus = CropSeasonStatus.Completed;
            }
            // 2. Nếu tất cả details bị Cancelled -> Cancelled  
            else if (cancelledCount == totalDetails && currentStatus != CropSeasonStatus.Cancelled)
            {
                newStatus = CropSeasonStatus.Cancelled;
            }
            // 3. Nếu có ít nhất 1 detail đang InProgress -> Active
            else if (inProgressCount > 0 && currentStatus != CropSeasonStatus.Active)
            {
                newStatus = CropSeasonStatus.Active;
            }
            // 4. Nếu tất cả details vẫn Planned -> Active (hoặc Paused tùy business logic)
            else if (plannedCount == totalDetails && currentStatus != CropSeasonStatus.Active)
            {
                newStatus = CropSeasonStatus.Active;
            }
            // 5. Nếu có mix status (Completed + Cancelled, InProgress + Cancelled, etc.) -> Active
            else if ((completedCount > 0 && cancelledCount > 0) || 
                     (inProgressCount > 0 && cancelledCount > 0) ||
                     (plannedCount > 0 && cancelledCount > 0))
            {
                if (currentStatus != CropSeasonStatus.Active)
                {
                    newStatus = CropSeasonStatus.Active;
                }
            }

            if (newStatus != null)
            {
                cropSeason.Status = newStatus.ToString();
                cropSeason.UpdatedAt = DateHelper.NowVietnamTime();
                await _unitOfWork.CropSeasonRepository.UpdateAsync(cropSeason);
                await _unitOfWork.SaveChangesAsync();
            }
        }
    }
}
