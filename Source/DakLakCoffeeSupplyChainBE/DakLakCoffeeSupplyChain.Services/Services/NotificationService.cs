using DakLakCoffeeSupplyChain.Repositories.Models;
using DakLakCoffeeSupplyChain.Repositories.UnitOfWork;
using DakLakCoffeeSupplyChain.Services.IServices;
using System;
using System.Linq;
using System.Threading.Tasks;

public class NotificationService : INotificationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailService _emailService;

    public NotificationService(IUnitOfWork unitOfWork, IEmailService emailService)
    {
        _unitOfWork = unitOfWork;
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

        // Tạo thông báo
        await _unitOfWork.SystemNotificationRepository.CreateAsync(notification);

        // Lấy danh sách nhân viên doanh nghiệp để gửi thông báo
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
            {
                await _emailService.SendEmailAsync(staff.User.Email, title, message);
            }
        }

        await _unitOfWork.SaveChangesAsync();

        return notification;
    }
}
