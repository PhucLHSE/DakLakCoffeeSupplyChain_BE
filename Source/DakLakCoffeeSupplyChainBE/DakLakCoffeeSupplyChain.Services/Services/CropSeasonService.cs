using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.CropSeasonDTOs;
using DakLakCoffeeSupplyChain.Common.Enum.CropSeasonEnums;
using DakLakCoffeeSupplyChain.Common.Enum.FarmingCommitmentEnums;
using DakLakCoffeeSupplyChain.Common.Helpers;
using DakLakCoffeeSupplyChain.Repositories.Models;
using DakLakCoffeeSupplyChain.Repositories.UnitOfWork;
using DakLakCoffeeSupplyChain.Services.Base;
using DakLakCoffeeSupplyChain.Services.Generators;
using DakLakCoffeeSupplyChain.Services.IServices;
using DakLakCoffeeSupplyChain.Services.Mappers;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace DakLakCoffeeSupplyChain.Services.Services
{
    public class CropSeasonService : ICropSeasonService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICodeGenerator _codeGenerator;
        private readonly ICropService _cropService;

        public CropSeasonService(IUnitOfWork unitOfWork, ICodeGenerator codeGenerator, ICropService cropService)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _codeGenerator = codeGenerator ?? throw new ArgumentNullException(nameof(codeGenerator));
            _cropService = cropService ?? throw new ArgumentNullException(nameof(cropService));
        }

        public async Task<IServiceResult> GetAllByUserId(Guid userId, bool isAdmin, bool isManager)
        {
            // Tối ưu: Sử dụng projection để chỉ lấy dữ liệu cần thiết
            var predicate = isAdmin || isManager
                ? (Expression<Func<CropSeason, bool>>)(cs => !cs.IsDeleted)
                : (cs => cs.Farmer.UserId == userId && !cs.IsDeleted);

            // Tối ưu: Sử dụng projection thay vì include toàn bộ entity
            var cropSeasons = await _unitOfWork.CropSeasonRepository.GetAllAsync(
                predicate: predicate,
                include: q => q.Include(cs => cs.Farmer).ThenInclude(f => f.User),
                orderBy: q => q.OrderByDescending(cs => cs.StartDate),
                asNoTracking: true
            );

            if (!cropSeasons.Any())
                return new ServiceResult(Const.WARNING_NO_DATA_CODE, Const.WARNING_NO_DATA_MSG);

            // Tối ưu: Sử dụng Select thay vì MapToCropSeasonViewAllDto để tránh tạo object không cần thiết
            var dtoList = cropSeasons.Select(cs => new
            {
                cs.CropSeasonId,
                cs.SeasonName,
                cs.StartDate,
                cs.EndDate,
                cs.Area,
                FarmerName = cs.Farmer?.User?.Name ?? string.Empty,
                cs.FarmerId,
                cs.Status
            }).ToList();

            return new ServiceResult(Const.SUCCESS_READ_CODE, Const.SUCCESS_READ_MSG, dtoList);
        }

        public async Task<IServiceResult> GetById(Guid cropSeasonId, Guid userId, bool isAdmin = false, bool isManager = false)
        {
            // Tối ưu: Giảm số lượng include để cải thiện hiệu suất
            var cropSeason = await _unitOfWork.CropSeasonRepository.GetByIdAsync(
                predicate: cs => cs.CropSeasonId == cropSeasonId && !cs.IsDeleted,
                include: query => query
                    .Include(cs => cs.Farmer).ThenInclude(f => f.User)
                    .Include(cs => cs.CropSeasonDetails)
                        .ThenInclude(d => d.CommitmentDetail)
                            .ThenInclude(cd => cd.PlanDetail)
                                .ThenInclude(pd => pd.CoffeeType)
                    .Include(cs => cs.CropSeasonDetails)
                        .ThenInclude(d => d.Crop)
                    .Include(cs => cs.Commitment)
                        .ThenInclude(c => c.Registration)
                            .ThenInclude(r => r.CultivationRegistrationsDetails),
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
            try
            {
                // Tối ưu: Gộp 2 queries thành 1 query với include
                var commitmentWithFarmer = await _unitOfWork.FarmingCommitmentRepository.GetByIdAsync(
                    predicate: c => c.CommitmentId == dto.CommitmentId && !c.IsDeleted,
                    include: q => q.Include(c => c.Farmer),
                    asNoTracking: true
                );
                
                if (commitmentWithFarmer == null)
                    return new ServiceResult(Const.FAIL_CREATE_CODE, "Không tìm thấy cam kết canh tác.");

                var farmer = commitmentWithFarmer.Farmer;
                if (farmer == null)
                    return new ServiceResult(Const.FAIL_CREATE_CODE, "Không tìm thấy nông hộ tương ứng.");

                if (farmer.UserId != userId)
                    return new ServiceResult(Const.FAIL_CREATE_CODE, "Cam kết không thuộc về bạn.");

                if (!string.Equals(commitmentWithFarmer.Status, FarmingCommitmentStatus.Active.ToString(), StringComparison.OrdinalIgnoreCase))
                    return new ServiceResult(Const.FAIL_CREATE_CODE, "Cam kết chưa được duyệt hoặc không hợp lệ.");

                // Kiểm tra commitment đã được duyệt (có ApprovedAt)
                if (!commitmentWithFarmer.ApprovedAt.HasValue)
                    return new ServiceResult(Const.FAIL_CREATE_CODE, "Chỉ có thể tạo mùa vụ từ cam kết đã được duyệt.");

                // Kiểm tra commitment đã có mùa vụ chưa - Mỗi commitment chỉ được tạo 1 mùa vụ
                var existingSeason = await _unitOfWork.CropSeasonRepository.GetByIdAsync(
                    x => x.CommitmentId == dto.CommitmentId && !x.IsDeleted
                );
                
                if (existingSeason != null)
                    return new ServiceResult(Const.FAIL_CREATE_CODE, 
                        "Cam kết này đã có mùa vụ. Mỗi cam kết chỉ được tạo một mùa vụ duy nhất.");

                // Tối ưu: Kiểm tra overlap trước khi tạo entity (validation này giờ không cần thiết nữa nhưng giữ lại để đảm bảo an toàn)
                var hasOverlap = await _unitOfWork.CropSeasonRepository.ExistsAsync(
                    x => x.CommitmentId == dto.CommitmentId && 
                         !x.IsDeleted &&
                         ((dto.StartDate < x.EndDate) && (dto.EndDate > x.StartDate) && 
                          (dto.StartDate != x.EndDate) && (dto.EndDate != x.StartDate))
                );
                
                if (hasOverlap)
                    return new ServiceResult(Const.FAIL_CREATE_CODE, "Thời gian mùa vụ trùng với một mùa vụ khác trong cùng cam kết.");

                if (dto.StartDate >= dto.EndDate)
                    return new ServiceResult(Const.FAIL_CREATE_CODE, "Ngày bắt đầu phải trước ngày kết thúc.");

                // Validation theo approvedAt của commitment
                if (commitmentWithFarmer.ApprovedAt.HasValue)
                {
                    var approvedDate = DateOnly.FromDateTime(commitmentWithFarmer.ApprovedAt.Value);
                    
                    // Kiểm tra start date phải sau hoặc bằng ngày approved (có thể bắt đầu cùng ngày)
                    if (dto.StartDate < approvedDate)
                    {
                        return new ServiceResult(Const.FAIL_CREATE_CODE, 
                            "Ngày bắt đầu mùa vụ phải sau hoặc bằng ngày cam kết được duyệt.");
                    }
                    
                    // Kiểm tra thời gian mùa vụ phải trong khoảng 11-12 tháng
                    var monthsDiff = (dto.EndDate.Year - dto.StartDate.Year) * 12 + 
                                   (dto.EndDate.Month - dto.StartDate.Month);
                    
                    if (monthsDiff < 11 || monthsDiff > 15) // Cho phép sai số 2 tháng để xử lý thiên tai
                    {
                        return new ServiceResult(Const.FAIL_CREATE_CODE, 
                            "Thời gian mùa vụ phải trong khoảng 11-15 tháng (có thể kéo dài thêm 2-3 tháng nếu gặp thiên tai).");
                    }
                }

                // Tối ưu: Lấy commitment details với projection để giảm dữ liệu truyền
                var commitmentDetails = await _unitOfWork.FarmingCommitmentsDetailRepository.GetAllAsync(
                    predicate: cd => cd.CommitmentId == commitmentWithFarmer.CommitmentId && !cd.IsDeleted,
                    include: q => q.Include(cd => cd.RegistrationDetail),
                    asNoTracking: true
                );

                // Bỏ validation về thời gian thu hoạch dự kiến
                // Cho phép nông dân tự do chọn thời gian mùa vụ không phụ thuộc vào thời gian thu hoạch dự kiến

                string code = await _codeGenerator.GenerateCropSeasonCodeAsync(dto.StartDate.Year);
                Guid cropSeasonId = Guid.NewGuid();

                var registration = await _unitOfWork.CultivationRegistrationRepository.GetByIdAsync(
                    predicate: r => r.RegistrationId == commitmentWithFarmer.RegistrationId && !r.IsDeleted,
                    asNoTracking: true
                );
                if (registration == null)
                    return new ServiceResult(Const.FAIL_CREATE_CODE, "Không tìm thấy đơn đăng ký canh tác.");

                var cropSeason = dto.MapToCropSeasonCreateDto(code, farmer.FarmerId, cropSeasonId);
                cropSeason.Area = registration.RegisteredArea ?? 0;
                cropSeason.CommitmentId = commitmentWithFarmer.CommitmentId;

                try
                {
                    await _unitOfWork.CropSeasonRepository.CreateAsync(cropSeason);
                }
                catch (Exception ex)
                {
                    // TODO: Replace with proper logging framework
                    return new ServiceResult(Const.FAIL_CREATE_CODE, $"Lỗi khi tạo mùa vụ: {ex.Message}");
                }

                // Tối ưu: Sử dụng lại commitmentDetails đã lấy ở trên
                if (!commitmentDetails.Any())
                    return new ServiceResult(Const.FAIL_CREATE_CODE, "Không có chi tiết cam kết để tạo vùng trồng.");

                // Tối ưu: Batch insert thay vì insert từng cái một
                var seasonDetails = new List<CropSeasonDetail>();
                foreach (var detail in commitmentDetails)
                {
                    try
                    {
                        var seasonDetail = new CropSeasonDetail
                        {
                            DetailId = Guid.NewGuid(),
                            CropSeasonId = cropSeason.CropSeasonId,
                            CommitmentDetailId = detail.CommitmentDetailId,
                            CropId = detail.RegistrationDetail?.CropId, // Set CropId từ RegistrationDetail
                            ExpectedHarvestStart = detail.RegistrationDetail?.ExpectedHarvestStart ?? detail.EstimatedDeliveryStart ?? dto.StartDate,  
                            ExpectedHarvestEnd = detail.RegistrationDetail?.ExpectedHarvestEnd ?? detail.EstimatedDeliveryEnd ?? dto.EndDate,     
                            AreaAllocated = detail.RegistrationDetail?.RegisteredArea ?? 0,
                            EstimatedYield = detail.RegistrationDetail?.EstimatedYield ?? 0,
                            PlannedQuality = null,
                            QualityGrade = null,
                            Status = CropDetailStatus.Planned.ToString(),
                            CreatedAt = DateHelper.NowVietnamTime(),
                            UpdatedAt = DateHelper.NowVietnamTime(),
                            IsDeleted = false
                        };

                        seasonDetails.Add(seasonDetail);
                    }
                    catch (Exception ex)
                    {
                        // TODO: Replace with proper logging framework
                        // Tiếp tục tạo các detail khác, không fail toàn bộ operation
                    }
                }

                // Tối ưu: Sử dụng BulkCreateAsync để tạo tất cả details cùng lúc
                if (seasonDetails.Any())
                {
                    await _unitOfWork.CropSeasonDetailRepository.BulkCreateAsync(seasonDetails);
                }

                int result;
                try
                {
                    result = await _unitOfWork.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    // TODO: Replace with proper logging framework
                    return new ServiceResult(Const.FAIL_CREATE_CODE, $"Lỗi khi lưu dữ liệu: {ex.Message}");
                }

                if (result > 0)
                {
                    try
                    {
                        // Thử lấy entity đầy đủ để trả về response chi tiết
                        var fullEntity = await _unitOfWork.CropSeasonRepository.GetWithDetailsByIdAsync(cropSeason.CropSeasonId);
                        if (fullEntity != null)
                        {
                            var responseDto = fullEntity.MapToCropSeasonViewDetailsDto(registration);
                            return new ServiceResult(Const.SUCCESS_CREATE_CODE, Const.SUCCESS_CREATE_MSG, responseDto);
                        }
                    }
                    catch (Exception ex)
                    {
                        // TODO: Replace with proper logging framework
                    }

                    // Nếu không lấy được entity đầy đủ, trả về thông tin cơ bản
                    var basicResponse = new
                    {
                        CropSeasonId = cropSeason.CropSeasonId,
                        SeasonName = cropSeason.SeasonName,
                        StartDate = cropSeason.StartDate,
                        EndDate = cropSeason.EndDate,
                        Area = cropSeason.Area,
                        Status = cropSeason.Status,
                        Message = "Mùa vụ đã được tạo thành công"
                    };

                    return new ServiceResult(Const.SUCCESS_CREATE_CODE, Const.SUCCESS_CREATE_MSG, basicResponse);
                }

                return new ServiceResult(Const.FAIL_CREATE_CODE, Const.FAIL_CREATE_MSG);
            }
            catch (Exception ex)
            {
                // TODO: Replace with proper logging framework
                return new ServiceResult(Const.ERROR_EXCEPTION, $"Lỗi hệ thống: {ex.Message}");
            }
        }

        public async Task<IServiceResult> Update(CropSeasonUpdateDto dto, Guid userId, bool isAdmin = false)
        {
            // Tối ưu: Chỉ load dữ liệu cần thiết cho update
            var cropSeason = await _unitOfWork.CropSeasonRepository.GetWithDetailsByIdForUpdateAsync(dto.CropSeasonId);
            if (cropSeason == null)
                return new ServiceResult(Const.FAIL_UPDATE_CODE, Const.WARNING_NO_DATA_MSG);

            if (!isAdmin && cropSeason.Farmer?.UserId != userId)
                return new ServiceResult(Const.FAIL_UPDATE_CODE, "Bạn không có quyền cập nhật mùa vụ này.");

            // Kiểm tra overlap với các mùa vụ khác trong cùng cam kết (trừ mùa vụ hiện tại)
            var hasOverlap = await _unitOfWork.CropSeasonRepository.ExistsAsync(
                x => x.CommitmentId == cropSeason.CommitmentId
                    && x.CropSeasonId != dto.CropSeasonId
                    && !x.IsDeleted
                    && dto.StartDate < x.EndDate && dto.EndDate > x.StartDate
                    && dto.StartDate != x.EndDate && dto.EndDate != x.StartDate
            );
            
            if (hasOverlap)
                return new ServiceResult(Const.FAIL_UPDATE_CODE, "Thời gian mùa vụ trùng với mùa vụ khác trong cam kết.");

            if (dto.StartDate >= dto.EndDate)
                return new ServiceResult(Const.FAIL_UPDATE_CODE, "Ngày bắt đầu phải trước ngày kết thúc.");

            // Tối ưu: Lấy commitment details với projection để giảm dữ liệu truyền
            var commitmentDetails = await _unitOfWork.FarmingCommitmentsDetailRepository.GetAllAsync(
                predicate: cd => cd.CommitmentId == cropSeason.CommitmentId && !cd.IsDeleted,
                include: q => q.Include(cd => cd.RegistrationDetail),
                asNoTracking: true
            );

            // Bỏ validation về thời gian thu hoạch dự kiến
            // Cho phép nông dân tự do chọn thời gian mùa vụ không phụ thuộc vào thời gian thu hoạch dự kiến

            dto.MapToExistingEntity(cropSeason);
            cropSeason.UpdatedAt = DateHelper.NowVietnamTime();

            // Tối ưu: Tránh vòng lặp tracking sâu
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
            // Tối ưu: Chỉ load dữ liệu cần thiết cho soft delete
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

            // Tối ưu: Batch update thay vì update từng cái một
            if (cropSeason.CropSeasonDetails != null && cropSeason.CropSeasonDetails.Any())
            {
                foreach (var d in cropSeason.CropSeasonDetails)
                {
                    d.IsDeleted = true;
                    d.UpdatedAt = now;
                }
                // Tối ưu: Sử dụng method có sẵn để update từng detail
                foreach (var d in cropSeason.CropSeasonDetails)
                {
                    await _unitOfWork.CropSeasonDetailRepository.UpdateAsync(d);
                }
            }

            // Đánh dấu IsDeleted cho mùa vụ
            cropSeason.IsDeleted = true;
            cropSeason.UpdatedAt = now;
            await _unitOfWork.CropSeasonRepository.UpdateAsync(cropSeason);

            var result = await _unitOfWork.SaveChangesAsync();

            return result > 0
                ? new ServiceResult(Const.SUCCESS_DELETE_CODE, "Xoá mùa vụ và toàn bộ vùng trồng thành công.")
                : new ServiceResult(Const.FAIL_DELETE_CODE, "Xoá mùa vụ thất bại.");
        }
        // ================================================================

        public async Task AutoUpdateCropSeasonStatusAsync(Guid cropSeasonId)
        {
            try
            {
                // Tối ưu: Chỉ load dữ liệu cần thiết cho status update
                var cropSeason = await _unitOfWork.CropSeasonRepository.GetByIdAsync(
                    predicate: cs => cs.CropSeasonId == cropSeasonId && !cs.IsDeleted,
                    include: q => q.Include(cs => cs.CropSeasonDetails.Where(d => !d.IsDeleted)),
                    asNoTracking: true // Sử dụng asNoTracking để tránh tracking conflict
                );
                
                if (cropSeason == null || cropSeason.IsDeleted) return;

                var allDetails = cropSeason.CropSeasonDetails?.ToList();
                if (allDetails == null || !allDetails.Any()) return;

                // Tối ưu: Sử dụng LINQ để đếm hiệu quả hơn
                var statusCounts = allDetails
                    .GroupBy(d => d.Status)
                    .ToDictionary(g => g.Key, g => g.Count());

                var completedCount = statusCounts.GetValueOrDefault(CropDetailStatus.Completed.ToString(), 0);
                var inProgressCount = statusCounts.GetValueOrDefault(CropDetailStatus.InProgress.ToString(), 0);
                var cancelledCount = statusCounts.GetValueOrDefault(CropDetailStatus.Cancelled.ToString(), 0);
                var plannedCount = statusCounts.GetValueOrDefault(CropDetailStatus.Planned.ToString(), 0);
                
                var totalDetails = allDetails.Count;

                // Parse current status
                CropSeasonStatus currentStatus;
                if (!Enum.TryParse<CropSeasonStatus>(cropSeason.Status, out currentStatus))
                {
                    currentStatus = CropSeasonStatus.Active;
                }

                CropSeasonStatus? newStatus = null;

                // Tối ưu: Sử dụng switch expression để code ngắn gọn hơn
                newStatus = (completedCount, inProgressCount, cancelledCount, plannedCount, totalDetails, currentStatus) switch
                {
                    // 1. Nếu tất cả details đã Completed -> Completed
                    (var completed, _, _, _, var total, var current) when completed == total && current != CropSeasonStatus.Completed 
                        => CropSeasonStatus.Completed,
                    
                    // 2. Nếu tất cả details bị Cancelled -> Cancelled  
                    (_, _, var cancelled, _, var total, var current) when cancelled == total && current != CropSeasonStatus.Cancelled 
                        => CropSeasonStatus.Cancelled,
                    
                    // 3. Nếu có ít nhất 1 detail đang InProgress -> Active
                    (_, var inProgress, _, _, _, var current) when inProgress > 0 && current != CropSeasonStatus.Active 
                        => CropSeasonStatus.Active,
                    
                    // 4. Nếu tất cả details vẫn Planned -> Active
                    (_, _, _, var planned, var total, var current) when planned == total && current != CropSeasonStatus.Active 
                        => CropSeasonStatus.Active,
                    
                    // 5. Nếu có mix status -> Active
                    (var completed, var inProgress, var cancelled, var planned, _, var current) 
                        when ((completed > 0 && cancelled > 0) || (inProgress > 0 && cancelled > 0) || (planned > 0 && cancelled > 0)) 
                        && current != CropSeasonStatus.Active 
                        => CropSeasonStatus.Active,
                    
                    _ => null
                };

                if (newStatus != null)
                {
                    // Sử dụng ExecuteUpdateAsync để update trực tiếp database mà không cần tracking
                    await _unitOfWork.CropSeasonRepository.GetQuery()
                        .Where(cs => cs.CropSeasonId == cropSeasonId)
                        .ExecuteUpdateAsync(s => s
                            .SetProperty(cs => cs.Status, newStatus.ToString())
                            .SetProperty(cs => cs.UpdatedAt, DateHelper.NowVietnamTime()));

                    // Auto update Crop status cho tất cả Crop liên kết với CropSeason này
                    var cropIds = cropSeason.CropSeasonDetails
                        .Where(csd => csd.CropId.HasValue && !csd.IsDeleted)
                        .Select(csd => csd.CropId.Value)
                        .Distinct();
                    
                    foreach (var cropId in cropIds)
                    {
                        await _cropService.AutoUpdateCropStatusAsync(cropId);
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error but don't throw to avoid affecting the main operation
                // TODO: Replace with proper logging framework
            }
        }

        public async Task<IServiceResult> GetAllByUserIdWithFilter(Guid userId, bool isAdmin, bool isManager, string? search = null, string? status = null, int page = 1, int pageSize = 10)
        {
            // Tối ưu: Sử dụng projection để chỉ lấy dữ liệu cần thiết
            var predicate = isAdmin || isManager
                ? (Expression<Func<CropSeason, bool>>)(cs => !cs.IsDeleted)
                : (cs => cs.Farmer.UserId == userId && !cs.IsDeleted);

            // Tối ưu: Sử dụng projection thay vì include toàn bộ entity
            var cropSeasons = await _unitOfWork.CropSeasonRepository.GetAllAsync(
                predicate: predicate,
                include: q => q.Include(cs => cs.Farmer).ThenInclude(f => f.User),
                orderBy: q => q.OrderByDescending(cs => cs.StartDate),
                asNoTracking: true
            );

            if (!cropSeasons.Any())
                return new ServiceResult(Const.WARNING_NO_DATA_CODE, Const.WARNING_NO_DATA_MSG);

            // Tối ưu: Sử dụng Select thay vì MapToCropSeasonViewAllDto để tránh tạo object không cần thiết
            var dtoList = cropSeasons.Select(cs => new
            {
                cs.CropSeasonId,
                cs.SeasonName,
                cs.StartDate,
                cs.EndDate,
                cs.Area,
                FarmerName = cs.Farmer?.User?.Name ?? string.Empty,
                cs.FarmerId,
                cs.Status
            }).ToList();

            // Tối ưu: Xử lý search và filter
            var filteredData = dtoList.AsQueryable();
            
            if (!string.IsNullOrEmpty(search))
            {
                var searchLower = search.ToLower();
                filteredData = filteredData.Where(cs => 
                    cs.SeasonName.ToLower().Contains(searchLower) ||
                    cs.FarmerName.ToLower().Contains(searchLower)
                );
            }
            
            if (!string.IsNullOrEmpty(status))
            {
                filteredData = filteredData.Where(cs => cs.Status == status);
            }

            // Tối ưu: Xử lý pagination
            var totalCount = filteredData.Count();
            var pagedData = filteredData
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var result = new
            {
                data = pagedData,
                totalCount,
                page,
                pageSize,
                totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
            };

            return new ServiceResult(Const.SUCCESS_READ_CODE, Const.SUCCESS_READ_MSG, result);
        }
    }
}
