using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.ExpertAdviceDTOs;
using DakLakCoffeeSupplyChain.Repositories.Models;
using DakLakCoffeeSupplyChain.Repositories.UnitOfWork;
using DakLakCoffeeSupplyChain.Services.Base;
using Microsoft.EntityFrameworkCore;

public class ExpertAdviceService : IExpertAdviceService
{
    private readonly IUnitOfWork _unitOfWork;

    public ExpertAdviceService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
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
            AdviceSource = dto.AdviceSource,
            AdviceText = dto.AdviceText,
            AttachedFileUrl = dto.AttachedFileUrl,
            ExpertId = expert.ExpertId, // ✅ CẦN THIẾT: tránh lỗi khóa ngoại
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false,
        };

        await _unitOfWork.ExpertAdviceRepository.AddAsync(advice);

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
