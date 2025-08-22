using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.ExpertAdviceDTOs;
using DakLakCoffeeSupplyChain.Repositories.Models;
using DakLakCoffeeSupplyChain.Repositories.UnitOfWork;
using DakLakCoffeeSupplyChain.Services.Base;
using DakLakCoffeeSupplyChain.Services.IServices;
using Microsoft.EntityFrameworkCore;
using DakLakCoffeeSupplyChain.Services.IServices;

public class ExpertAdviceService : IExpertAdviceService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationService _notificationService; // ✅ Thêm notification service
    private readonly IMediaService _mediaService; // ✅ Thêm media service

    public ExpertAdviceService(
        IUnitOfWork unitOfWork,
        INotificationService notificationService, // ✅ Inject notification service
        IMediaService mediaService) // ✅ Inject media service
    {
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
        _mediaService = mediaService;
    }

    public async Task<IServiceResult> GetAllByUserIdAsync(Guid userId, bool isAdmin = false)
    {
        var advices = await _unitOfWork.ExpertAdviceRepository.GetAllAsync(
            predicate: a => !a.IsDeleted && (isAdmin || a.Expert.UserId == userId),
            include: q => q.Include(a => a.Expert).ThenInclude(e => e.User),
            orderBy: q => q.OrderByDescending(a => a.CreatedAt),
            asNoTracking: true
        );

        var result = advices.Select(a => a.MapToViewAllDto()).ToList();

        if (!result.Any())
            return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Chưa có phản hồi nào.");

        return new ServiceResult(Const.SUCCESS_READ_CODE, Const.SUCCESS_READ_MSG, result);
    }

    // ✅ Thêm method cho BusinessManager xem tất cả expert advice
    public async Task<IServiceResult> GetAllForManagerAsync()
    {
        var advices = await _unitOfWork.ExpertAdviceRepository.GetAllAsync(
            predicate: a => !a.IsDeleted,
            include: q => q.Include(a => a.Expert).ThenInclude(e => e.User)
                        .Include(a => a.Report).ThenInclude(r => r.ReportedByNavigation),
            orderBy: q => q.OrderByDescending(a => a.CreatedAt),
            asNoTracking: true
        );

        var result = advices.Select(a => new ExpertAdviceViewForManagerDto
        {
            AdviceId = a.AdviceId,
            ReportId = a.ReportId,
            ReportTitle = a.Report?.Title ?? "Không xác định",
            ReportCode = a.Report?.ReportCode ?? "Không xác định",
            ExpertId = a.ExpertId,
            ExpertName = a.Expert?.User?.Name ?? "Không xác định",
            ExpertEmail = a.Expert?.User?.Email ?? "Không xác định",
            ResponseType = a.ResponseType ?? "Không xác định",
            AdviceSource = a.AdviceSource ?? "Không xác định",
            AdviceText = a.AdviceText,
            AttachedFileUrl = a.AttachedFileUrl,
            CreatedAt = a.CreatedAt,
            ReportedByName = a.Report?.ReportedByNavigation?.Name ?? "Không xác định",
            ReportedByEmail = a.Report?.ReportedByNavigation?.Email ?? "Không xác định"
        }).ToList();

        if (!result.Any())
            return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Chưa có phản hồi nào.");

        return new ServiceResult(Const.SUCCESS_READ_CODE, Const.SUCCESS_READ_MSG, result);
    }

    public async Task<IServiceResult> GetByIdAsync(Guid adviceId, Guid userId, bool isAdmin = false)
    {
        var advice = await _unitOfWork.ExpertAdviceRepository.GetByIdAsync(
            predicate: a => a.AdviceId == adviceId && !a.IsDeleted,
            include: q => q.Include(a => a.Expert).ThenInclude(e => e.User),
            asNoTracking: true
        );

        if (advice == null)
            return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không tìm thấy lời khuyên.");

        if (!isAdmin && advice.Expert.UserId != userId)
            return new ServiceResult(Const.FAIL_READ_CODE, "Bạn không có quyền xem lời khuyên này.");

        return new ServiceResult(Const.SUCCESS_READ_CODE, Const.SUCCESS_READ_MSG, advice.MapToViewDetailDto());
    }

    public async Task<IServiceResult> CreateAsync(ExpertAdviceCreateDto dto, Guid userId)
    {
        // 1. Lấy thông tin chuyên gia từ userId
        var expert = await _unitOfWork.AgriculturalExpertRepository.GetByUserIdAsync(userId);
        if (expert == null)
            return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Chuyên gia không tồn tại.");

        // 2. Kiểm tra báo cáo tồn tại
        var report = await _unitOfWork.GeneralFarmerReportRepository.GetByIdAsync(
            predicate: r => r.ReportId == dto.ReportId && !r.IsDeleted,
            asNoTracking: true
        );
        if (report == null)
            return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không tìm thấy báo cáo của nông dân.");

        // 3. Tạo phản hồi
        var advice = new ExpertAdvice
        {
            AdviceId = Guid.NewGuid(),
            ReportId = dto.ReportId,
            ResponseType = dto.ResponseType,
            AdviceSource = dto.AdviceSource ?? "Kinh nghiệm chuyên môn",  // ✅ Sửa: Xử lý null
            AdviceText = dto.AdviceText,
            AttachedFileUrl = dto.AttachedFileUrl,
            ExpertId = expert.ExpertId, // ✅ CẦN THIẾT: tránh lỗi khóa ngoại
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false,
        };

        await _unitOfWork.ExpertAdviceRepository.AddAsync(advice);

        // Upload files nếu có
        if (dto.AttachedFiles?.Any() == true)
        {
            try
            {
                var allFiles = dto.AttachedFiles.ToList();
                
                // Upload files lên Cloudinary
                var mediaList = await _mediaService.UploadAndSaveMediaAsync(
                    allFiles,
                    relatedEntity: "ExpertAdvice",
                    relatedId: advice.AdviceId,
                    uploadedBy: userId.ToString()
                );

                // Cập nhật AttachedFileUrl từ file đầu tiên (hoặc tất cả URLs)
                if (mediaList.Any())
                {
                    var fileUrls = mediaList.Select(m => m.MediaUrl).ToList();
                    advice.AttachedFileUrl = string.Join(";", fileUrls); // Nối nhiều URL bằng dấu ;
                    
                    await _unitOfWork.ExpertAdviceRepository.UpdateAsync(advice);
                    await _unitOfWork.SaveChangesAsync();
                }
            }
            catch (Exception mediaEx)
            {
                Console.WriteLine($"ERROR: File upload failed for ExpertAdvice {advice.AdviceId}: {mediaEx.Message}");
                Console.WriteLine($"Stack trace: {mediaEx.StackTrace}");
                
                // Nếu file upload thất bại, có thể rollback hoặc throw exception
                // Tùy theo business logic, có thể:
                // 1. Rollback toàn bộ transaction
                // 2. Tiếp tục với advice không có file
                // 3. Throw exception để frontend biết
                
                // Hiện tại: tiếp tục với advice không có file
                // Nhưng log chi tiết để debug
            }
        }

        // ✅ Cập nhật trạng thái Resolve cho báo cáo nông dân
        var reportToUpdate = await _unitOfWork.GeneralFarmerReportRepository.GetByIdAsync(dto.ReportId);
        if (reportToUpdate != null && reportToUpdate.IsResolved != true)
        {
            reportToUpdate.IsResolved = true;
            reportToUpdate.ResolvedAt = DateTime.UtcNow;
            reportToUpdate.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.GeneralFarmerReportRepository.Update(reportToUpdate);
        }

        await _unitOfWork.SaveChangesAsync();

        // ✅ Gửi thông báo cho Farmer
        var expertUser = await _unitOfWork.AgriculturalExpertRepository.GetByUserIdAsync(userId);
        if (expertUser?.User != null)
        {
            await _notificationService.NotifyExpertAdviceCreatedAsync(
                dto.ReportId,
                userId,
                expertUser.User.Name,
                dto.AdviceText
            );
        }

        // 4. Trả lại kết quả chi tiết
        var savedAdvice = await _unitOfWork.ExpertAdviceRepository.GetByIdAsync(
            predicate: a => a.AdviceId == advice.AdviceId,
            include: q => q.Include(a => a.Expert).ThenInclude(e => e.User),
            asNoTracking: true
        );

        return new ServiceResult(Const.SUCCESS_CREATE_CODE, Const.SUCCESS_CREATE_MSG, savedAdvice?.MapToViewDetailDto());
    }
    public async Task<IServiceResult> UpdateAsync(Guid adviceId, ExpertAdviceUpdateDto dto, Guid userId)
    {
        var advice = await _unitOfWork.ExpertAdviceRepository.GetByIdAsync(
            predicate: a => a.AdviceId == adviceId && !a.IsDeleted,
            include: q => q.Include(a => a.Expert),
            asNoTracking: false
        );

        if (advice == null)
            return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không tìm thấy lời khuyên.");

        if (advice.Expert.UserId != userId)
            return new ServiceResult(Const.FAIL_UPDATE_CODE, "Bạn không có quyền cập nhật lời khuyên này.");

        // Cập nhật nội dung
        advice.ResponseType = dto.ResponseType;
        advice.AdviceSource = dto.AdviceSource;
        advice.AdviceText = dto.AdviceText;
        advice.AttachedFileUrl = dto.AttachedFileUrl;

        _unitOfWork.ExpertAdviceRepository.Update(advice);
        await _unitOfWork.SaveChangesAsync();

        var updated = await _unitOfWork.ExpertAdviceRepository.GetByIdAsync(
            predicate: a => a.AdviceId == advice.AdviceId,
            include: q => q.Include(a => a.Expert).ThenInclude(e => e.User),
            asNoTracking: true
        );

        return new ServiceResult(Const.SUCCESS_UPDATE_CODE, Const.SUCCESS_UPDATE_MSG, updated?.MapToViewDetailDto());
    }
    public async Task<IServiceResult> SoftDeleteAsync(Guid adviceId, Guid userId, bool isAdmin = false)
    {
        var advice = await _unitOfWork.ExpertAdviceRepository.GetByIdAsync(
            predicate: a => a.AdviceId == adviceId && !a.IsDeleted,
            include: q => q.Include(a => a.Expert),
            asNoTracking: false
        );

        if (advice == null)
            return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không tìm thấy lời khuyên.");

        if (!isAdmin && advice.Expert.UserId != userId)
            return new ServiceResult(Const.FAIL_DELETE_CODE, "Bạn không có quyền xoá lời khuyên này.");

        advice.IsDeleted = true;

        _unitOfWork.ExpertAdviceRepository.Update(advice);
        await _unitOfWork.SaveChangesAsync();

        return new ServiceResult(Const.SUCCESS_DELETE_CODE, "Đã xoá mềm lời khuyên.");
    }
    public async Task<IServiceResult> HardDeleteAsync(Guid adviceId, Guid userId, bool isAdmin = false)
    {
        var advice = await _unitOfWork.ExpertAdviceRepository.GetByIdAsync(
            predicate: a => a.AdviceId == adviceId,
            include: q => q.Include(a => a.Expert),
            asNoTracking: false
        );

        if (advice == null)
        {
            return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không tìm thấy lời khuyên.");
        }

        if (!isAdmin && advice.Expert.UserId != userId)
        {
            return new ServiceResult(Const.FAIL_DELETE_CODE, "Bạn không có quyền xoá lời khuyên này.");
        }

        _unitOfWork.ExpertAdviceRepository.Delete(advice);
        await _unitOfWork.SaveChangesAsync();

        return new ServiceResult(Const.SUCCESS_DELETE_CODE, "Đã xoá vĩnh viễn lời khuyên.");
    }

}
