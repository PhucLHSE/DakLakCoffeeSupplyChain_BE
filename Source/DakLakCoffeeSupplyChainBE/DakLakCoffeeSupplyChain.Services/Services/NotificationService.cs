using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Repositories.Models;
using DakLakCoffeeSupplyChain.Repositories.UnitOfWork;
using DakLakCoffeeSupplyChain.Services.Base;
using DakLakCoffeeSupplyChain.Services.Generators; 
using DakLakCoffeeSupplyChain.Services.IServices;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

public class NotificationService : INotificationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailService _emailService;
    private readonly ICodeGenerator _codeGenerator; 

    public NotificationService(
        IUnitOfWork unitOfWork,
        IEmailService emailService,
        ICodeGenerator codeGenerator 
    )
    {
        _unitOfWork = unitOfWork;
        _emailService = emailService;
        _codeGenerator = codeGenerator;
    }

    public async Task<SystemNotification> NotifyInboundRequestCreatedAsync(Guid requestId, Guid farmerId)
    {
        var title = "📥 Yêu cầu nhập kho mới";
        var message = $"Nông dân có mã ID: {farmerId} vừa gửi yêu cầu nhập kho (Mã yêu cầu: {requestId}).";

        var notification = new SystemNotification
        {
            NotificationId = Guid.NewGuid(),
            NotificationCode = await _codeGenerator.GenerateNotificationCodeAsync(), // ✅ SỬ DỤNG GENERATOR
            Title = title,
            Message = message,
            Type = "WarehouseInbound",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = null
        };

        await _unitOfWork.SystemNotificationRepository.CreateAsync(notification);

        var request = await _unitOfWork.WarehouseInboundRequests.GetByIdAsync(requestId);
        if (request == null)
            return notification;

        var batch = await _unitOfWork.ProcessingBatchRepository.GetByIdAsync(
            predicate: b => b.BatchId == request.BatchId,
            include: b => b.Include(b => b.Farmer).ThenInclude(f => f.FarmingCommitments)
        );

        var managerId = batch?.Farmer?.FarmingCommitments?.FirstOrDefault(fc => !fc.IsDeleted)?.ApprovedBy;
        if (managerId == null || managerId == Guid.Empty)
            return notification;

        var businessStaffs = await _unitOfWork.BusinessStaffRepository.GetBySupervisorIdAsync(managerId.Value);

        foreach (var staff in businessStaffs)
        {
            var recipient = new SystemNotificationRecipient
            {
                Id = Guid.NewGuid(),
                NotificationId = notification.NotificationId,
                RecipientId = staff.UserId,
                IsRead = false,
                ReadAt = null
            };

            await _unitOfWork.SystemNotificationRecipientRepository.CreateAsync(recipient);

            if (!string.IsNullOrWhiteSpace(staff.User?.Email))
                await _emailService.SendEmailAsync(staff.User.Email, title, message);
        }

        await _unitOfWork.SaveChangesAsync();
        return notification;
    }

    public async Task<SystemNotification> NotifyInboundRequestApprovedAsync(Guid requestId, Guid farmerId)
    {
        var title = "✅ Yêu cầu nhập kho đã được duyệt";
        var message = $"Yêu cầu nhập kho mã {requestId} của bạn đã được duyệt.";

        var notification = new SystemNotification
        {
            NotificationId = Guid.NewGuid(),
            NotificationCode = await _codeGenerator.GenerateNotificationCodeAsync(), // ✅ SỬ DỤNG GENERATOR
            Title = title,
            Message = message,
            Type = "WarehouseInbound",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = null
        };

        await _unitOfWork.SystemNotificationRepository.CreateAsync(notification);

        var farmerUser = await _unitOfWork.UserAccountRepository.GetByIdAsync(farmerId);
        if (farmerUser != null)
        {
            var recipient = new SystemNotificationRecipient
            {
                Id = Guid.NewGuid(),
                NotificationId = notification.NotificationId,
                RecipientId = farmerUser.UserId,
                IsRead = false,
                ReadAt = null
            };

            await _unitOfWork.SystemNotificationRecipientRepository.CreateAsync(recipient);

            if (!string.IsNullOrWhiteSpace(farmerUser.Email))
                await _emailService.SendEmailAsync(farmerUser.Email, title, message);
        }

        await _unitOfWork.SaveChangesAsync();
        return notification;
    }

    public async Task<SystemNotification> NotifyOutboundRequestCreatedAsync(Guid requestId, Guid managerId)
    {
        var title = "📤 Yêu cầu xuất kho mới";
        var message = $"Quản lý doanh nghiệp (ID: {managerId}) đã gửi yêu cầu xuất kho (Mã: {requestId}).";

        var notification = new SystemNotification
        {
            NotificationId = Guid.NewGuid(),
            NotificationCode = await _codeGenerator.GenerateNotificationCodeAsync(), // ✅ SỬ DỤNG GENERATOR
            Title = title,
            Message = message,
            Type = "WarehouseOutbound",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = null
        };

        await _unitOfWork.SystemNotificationRepository.CreateAsync(notification);

        var businessStaffs = await _unitOfWork.BusinessStaffRepository.GetAllWithUserAsync();
        foreach (var staff in businessStaffs)
        {
            var recipient = new SystemNotificationRecipient
            {
                Id = Guid.NewGuid(),
                NotificationId = notification.NotificationId,
                RecipientId = staff.UserId,
                IsRead = false,
                ReadAt = null
            };

            await _unitOfWork.SystemNotificationRecipientRepository.CreateAsync(recipient);

            if (!string.IsNullOrWhiteSpace(staff.User?.Email))
                await _emailService.SendEmailAsync(staff.User.Email, title, message);
        }

        await _unitOfWork.SaveChangesAsync();
        return notification;
    }

    // ✅ Thêm 2 phương thức mới
    public async Task<SystemNotification> NotifyFarmerReportCreatedAsync(Guid reportId, Guid farmerId, string reportTitle)
    {
        var title = "🚨 Báo cáo mới từ nông dân";
        var message = $"Nông dân vừa gửi báo cáo: '{reportTitle}'. Vui lòng xem xét và phản hồi.";

        var notification = new SystemNotification
        {
            NotificationId = Guid.NewGuid(),
            NotificationCode = await _codeGenerator.GenerateNotificationCodeAsync(),
            Title = title,
            Message = message,
            Type = "FarmerReport",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = farmerId
        };

        await _unitOfWork.SystemNotificationRepository.CreateAsync(notification);

        // ✅ Gửi thông báo cho tất cả chuyên gia nông nghiệp
        var experts = await _unitOfWork.AgriculturalExpertRepository.GetAllAsync(
            predicate: e => !e.IsDeleted && e.IsVerified == true,
            include: e => e.Include(e => e.User),
            asNoTracking: true
        );

        foreach (var expert in experts)
        {
            var recipient = new SystemNotificationRecipient
            {
                Id = Guid.NewGuid(),
                NotificationId = notification.NotificationId,
                RecipientId = expert.UserId,
                IsRead = false,
                ReadAt = null
            };

            await _unitOfWork.SystemNotificationRecipientRepository.CreateAsync(recipient);
        }

        await _unitOfWork.SaveChangesAsync();
        return notification;
    }

    public async Task<SystemNotification> NotifyExpertAdviceCreatedAsync(Guid reportId, Guid expertId, string expertName, string adviceText)
    {
        var title = " Chuyên gia đã phản hồi báo cáo";
        var message = $"Chuyên gia {expertName} đã phản hồi báo cáo của bạn: '{adviceText.Substring(0, Math.Min(100, adviceText.Length))}...'";

        var notification = new SystemNotification
        {
            NotificationId = Guid.NewGuid(),
            NotificationCode = await _codeGenerator.GenerateNotificationCodeAsync(),
            Title = title,
            Message = message,
            Type = "ExpertAdvice",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = expertId
        };

        await _unitOfWork.SystemNotificationRepository.CreateAsync(notification);

        // ✅ Lấy thông tin Farmer từ báo cáo
        var report = await _unitOfWork.GeneralFarmerReportRepository.GetByIdAsync(
            predicate: r => r.ReportId == reportId && !r.IsDeleted,
            asNoTracking: true
        );

        if (report != null)
        {
            var recipient = new SystemNotificationRecipient
            {
                Id = Guid.NewGuid(),
                NotificationId = notification.NotificationId,
                RecipientId = report.ReportedBy,
                IsRead = false,
                ReadAt = null
            };

            await _unitOfWork.SystemNotificationRecipientRepository.CreateAsync(recipient);
        }

        await _unitOfWork.SaveChangesAsync();
        return notification;
    }

    // ✅ Thêm các phương thức để Frontend gọi
    public async Task<IServiceResult> GetUserNotificationsAsync(Guid userId, int page, int pageSize)
    {
        try
        {
            var query = _unitOfWork.SystemNotificationRecipientRepository.GetQuery()
                .Where(r => r.RecipientId == userId && !r.IsDeleted)
                .Include(r => r.Notification)
                .OrderByDescending(r => r.Notification.CreatedAt);

            var totalCount = await query.CountAsync();
            var notifications = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(r => new
                {
                    NotificationId = r.NotificationId,
                    NotificationCode = r.Notification.NotificationCode,
                    Title = r.Notification.Title,
                    Message = r.Notification.Message,
                    Type = r.Notification.Type,
                    CreatedAt = r.Notification.CreatedAt,
                    CreatedBy = r.Notification.CreatedBy,
                    IsRead = r.IsRead ?? false,
                    ReadAt = r.ReadAt
                })
                .ToListAsync();

            var result = new
            {
                Data = notifications,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
            };

            return new ServiceResult(Const.SUCCESS_READ_CODE, Const.SUCCESS_READ_MSG, result);
        }
        catch (Exception ex)
        {
            return new ServiceResult(Const.ERROR_EXCEPTION, "Lỗi hệ thống: " + ex.Message);
        }
    }

    public async Task<IServiceResult> GetUnreadCountAsync(Guid userId)
    {
        try
        {
            var count = await _unitOfWork.SystemNotificationRecipientRepository.GetQuery()
                .Where(r => r.RecipientId == userId && !r.IsDeleted && (r.IsRead == null || r.IsRead == false))
                .CountAsync();

            return new ServiceResult(Const.SUCCESS_READ_CODE, Const.SUCCESS_READ_MSG, count);
        }
        catch (Exception ex)
        {
            return new ServiceResult(Const.ERROR_EXCEPTION, "Lỗi hệ thống: " + ex.Message);
        }
    }

    public async Task<IServiceResult> MarkAsReadAsync(Guid notificationId, Guid userId)
    {
        try
        {
            var recipient = await _unitOfWork.SystemNotificationRecipientRepository.GetQuery()
                .FirstOrDefaultAsync(r => r.NotificationId == notificationId && r.RecipientId == userId && !r.IsDeleted);

            if (recipient == null)
                return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không tìm thấy thông báo.");

            recipient.IsRead = true;
            recipient.ReadAt = DateTime.UtcNow;

            _unitOfWork.SystemNotificationRecipientRepository.Update(recipient);
            await _unitOfWork.SaveChangesAsync();

            return new ServiceResult(Const.SUCCESS_UPDATE_CODE, "Đã đánh dấu thông báo đã đọc.");
        }
        catch (Exception ex)
        {
            return new ServiceResult(Const.ERROR_EXCEPTION, "Lỗi hệ thống: " + ex.Message);
        }
    }

    public async Task<IServiceResult> MarkAllAsReadAsync(Guid userId)
    {
        try
        {
            var unreadRecipients = await _unitOfWork.SystemNotificationRecipientRepository.GetQuery()
                .Where(r => r.RecipientId == userId && !r.IsDeleted && (r.IsRead == null || r.IsRead == false))
                .ToListAsync();

            foreach (var recipient in unreadRecipients)
            {
                recipient.IsRead = true;
                recipient.ReadAt = DateTime.UtcNow;
                _unitOfWork.SystemNotificationRecipientRepository.Update(recipient);
            }

            await _unitOfWork.SaveChangesAsync();

            return new ServiceResult(Const.SUCCESS_UPDATE_CODE, $"Đã đánh dấu {unreadRecipients.Count} thông báo đã đọc.");
        }
        catch (Exception ex)
        {
            return new ServiceResult(Const.ERROR_EXCEPTION, "Lỗi hệ thống: " + ex.Message);
        }
    }

    public async Task<IServiceResult> GetNotificationByIdAsync(Guid notificationId, Guid userId)
    {
        try
        {
            var recipient = await _unitOfWork.SystemNotificationRecipientRepository.GetQuery()
                .Where(r => r.NotificationId == notificationId && r.RecipientId == userId && !r.IsDeleted)
                .Include(r => r.Notification)
                .FirstOrDefaultAsync();

            if (recipient == null)
                return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không tìm thấy thông báo.");

            var notification = new
            {
                NotificationId = recipient.NotificationId,
                NotificationCode = recipient.Notification.NotificationCode,
                Title = recipient.Notification.Title,
                Message = recipient.Notification.Message,
                Type = recipient.Notification.Type,
                CreatedAt = recipient.Notification.CreatedAt,
                CreatedBy = recipient.Notification.CreatedBy,
                IsRead = recipient.IsRead ?? false,
                ReadAt = recipient.ReadAt
            };

            return new ServiceResult(Const.SUCCESS_READ_CODE, Const.SUCCESS_READ_MSG, notification);
        }
        catch (Exception ex)
        {
            return new ServiceResult(Const.ERROR_EXCEPTION, "Lỗi hệ thống: " + ex.Message);
        }
    }
}
