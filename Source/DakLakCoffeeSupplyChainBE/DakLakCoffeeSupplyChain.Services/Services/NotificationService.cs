using DakLakCoffeeSupplyChain.Repositories.Models;
using DakLakCoffeeSupplyChain.Repositories.UnitOfWork;
using DakLakCoffeeSupplyChain.Services.IServices;
using DakLakCoffeeSupplyChain.Services.Generators; 
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
}
