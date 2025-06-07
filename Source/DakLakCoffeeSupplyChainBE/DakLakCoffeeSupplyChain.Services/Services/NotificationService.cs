using DakLakCoffeeSupplyChain.Repositories.DBContext;
using DakLakCoffeeSupplyChain.Repositories.Models;
using DakLakCoffeeSupplyChain.Services.IServices;
using Microsoft.EntityFrameworkCore;
using System;

public class NotificationService : INotificationService
{
    private readonly DakLakCoffee_SCMContext _context;
    private readonly IEmailService _emailService;

    public NotificationService(DakLakCoffee_SCMContext context, IEmailService emailService)
    {
        _context = context;
        _emailService = emailService;
    }

    public async Task<SystemNotification> NotifyInboundRequestCreatedAsync(Guid requestId, Guid farmerId)
    {
        var title = "📥 Yêu cầu nhập kho mới";
        var message = $"Nông dân có mã ID: {farmerId} vừa gửi yêu cầu nhập kho (Mã yêu cầu: {requestId}).";

        var notification = new SystemNotification
        {
            NotificationId = Guid.NewGuid(),
            NotificationCode = "NT-" + DateTime.UtcNow.Ticks,
            Title = title,
            Message = message,
            Type = "WarehouseInbound",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = null
        };

        _context.SystemNotifications.Add(notification);

        var businessStaffs = await _context.BusinessStaffs
            .Include(bs => bs.User)
            .ToListAsync();

        foreach (var staff in businessStaffs)
        {
            _context.SystemNotificationRecipients.Add(new SystemNotificationRecipient
            {
                Id = Guid.NewGuid(),
                NotificationId = notification.NotificationId,
                RecipientId = staff.UserId,
                IsRead = false,
                ReadAt = null
            });

            // Gửi mail nếu có email
            if (!string.IsNullOrWhiteSpace(staff.User?.Email))
            {
                await _emailService.SendEmailAsync(staff.User.Email, title, message);
            }
        }

        await _context.SaveChangesAsync();
        return notification;
    }
}

