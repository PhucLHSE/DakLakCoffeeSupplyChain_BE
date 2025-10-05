using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.Helpers;
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
            NotificationCode = await _codeGenerator.GenerateNotificationCodeAsync(), // SỬ DỤNG GENERATOR
            Title = title,
            Message = message,
            Type = "WarehouseInbound",
            CreatedAt = DateHelper.NowVietnamTime(),
            CreatedBy = null
        };

        await _unitOfWork.SystemNotificationRepository.CreateAsync(notification);

        var request = await _unitOfWork.WarehouseInboundRequests.GetByIdAsync(requestId);
        if (request == null)
            return notification;

        var batch = await _unitOfWork.ProcessingBatchRepository.GetByIdAsync(
            predicate: b => b.BatchId == request.BatchId,
            include: b => b
               .Include(b => b.Farmer)
                  .ThenInclude(f => f.FarmingCommitments)
        );

        var managerId = batch?.Farmer?.FarmingCommitments?
            .FirstOrDefault(fc => !fc.IsDeleted)?.ApprovedBy;

        if (managerId == null || managerId == Guid.Empty)
            return notification;

        var businessStaffs = await _unitOfWork.BusinessStaffRepository
            .GetBySupervisorIdAsync(managerId.Value);

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

            await _unitOfWork.SystemNotificationRecipientRepository
                .CreateAsync(recipient);

            if (!string.IsNullOrWhiteSpace(staff.User?.Email))
                await _emailService.SendEmailAsync(staff.User.Email, title, message);
        }

        await _unitOfWork.SaveChangesAsync();

        return notification;
    }

    public async Task<SystemNotification> NotifyEvaluationFailedAsync(
        Guid batchId, Guid farmerId, string evaluationComments)
    {
        var title = "⚠️ Lô sơ chế cần cải thiện";
        var message = $"Lô sơ chế của bạn đã được đánh giá không đạt. Vui lòng xem chi tiết và cải thiện theo hướng dẫn.";

        var notification = new SystemNotification
        {
            NotificationId = Guid.NewGuid(),
            NotificationCode = await _codeGenerator.GenerateNotificationCodeAsync(),
            Title = title,
            Message = message,
            Type = "EvaluationFailed",
            CreatedAt = DateHelper.NowVietnamTime(),
            CreatedBy = null
        };

        await _unitOfWork.SystemNotificationRepository.CreateAsync(notification);

        // Tạo recipient cho Farmer
        var recipient = new SystemNotificationRecipient
        {
            Id = Guid.NewGuid(),
            NotificationId = notification.NotificationId,
            RecipientId = farmerId,
            IsRead = false,
            ReadAt = null
        };

        await _unitOfWork.SystemNotificationRecipientRepository
            .CreateAsync(recipient);

        // Gửi email nếu có
        var farmerUser = await _unitOfWork.UserAccountRepository
            .GetByIdAsync(farmerId);

        if (farmerUser != null && 
            !string.IsNullOrWhiteSpace(farmerUser.Email))
        {
            var emailMessage = $"{message}\n\nChi tiết đánh giá:\n{evaluationComments}\n\nVui lòng đăng nhập vào hệ thống để xem chi tiết và cải thiện.";

            await _emailService.SendEmailAsync(farmerUser.Email, title, emailMessage);
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
            NotificationCode = await _codeGenerator.GenerateNotificationCodeAsync(), // SỬ DỤNG GENERATOR 
            Title = title,
            Message = message,
            Type = "WarehouseInbound",
            CreatedAt = DateHelper.NowVietnamTime(),
            CreatedBy = null
        };

        await _unitOfWork.SystemNotificationRepository
            .CreateAsync(notification);

        var farmerUser = await _unitOfWork.UserAccountRepository
            .GetByIdAsync(farmerId);

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

            await _unitOfWork.SystemNotificationRecipientRepository
                .CreateAsync(recipient);

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
            NotificationCode = await _codeGenerator.GenerateNotificationCodeAsync(), // SỬ DỤNG GENERATOR
            Title = title,
            Message = message,
            Type = "WarehouseOutbound",
            CreatedAt = DateHelper.NowVietnamTime(),
            CreatedBy = null
        };

        await _unitOfWork.SystemNotificationRepository
            .CreateAsync(notification);

        var businessStaffs = await _unitOfWork.BusinessStaffRepository
            .GetAllWithUserAsync();

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

            await _unitOfWork.SystemNotificationRecipientRepository
                .CreateAsync(recipient);

            if (!string.IsNullOrWhiteSpace(staff.User?.Email))
                await _emailService.SendEmailAsync(staff.User.Email, title, message);
        }

        await _unitOfWork.SaveChangesAsync();

        return notification;
    }

    // Thêm 2 phương thức mới
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
            CreatedAt = DateHelper.NowVietnamTime(),
            CreatedBy = farmerId
        };

        await _unitOfWork.SystemNotificationRepository
            .CreateAsync(notification);

        // Gửi thông báo cho tất cả chuyên gia nông nghiệp
        var experts = await _unitOfWork.AgriculturalExpertRepository.GetAllAsync(
            predicate: e => 
               !e.IsDeleted && 
               e.IsVerified == true,
            include: e => 
               e.Include(e => e.User),
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

            await _unitOfWork.SystemNotificationRecipientRepository
                .CreateAsync(recipient);
        }

        await _unitOfWork.SaveChangesAsync();

        return notification;
    }

    public async Task<SystemNotification> NotifyManagerNewRegistrationdAsync(
        Guid recipientId, Guid senderId, string farmerName, string content)
    {
        var title = "Có đơn đăng ký mới cho kế hoạch của bạn";
        var message = $"Nông dân {farmerName} đã đăng ký vào kế hoạch {content}";

        var notification = new SystemNotification
        {
            NotificationId = Guid.NewGuid(),
            NotificationCode = await _codeGenerator.GenerateNotificationCodeAsync(),
            Title = title,
            Message = message,
            Type = "NewRegistration",
            CreatedAt = DateHelper.NowVietnamTime(),
            CreatedBy = senderId
        };

        await _unitOfWork.SystemNotificationRepository
            .CreateAsync(notification);

        var recipient = new SystemNotificationRecipient
        {
            Id = Guid.NewGuid(),
            NotificationId = notification.NotificationId,
            RecipientId = recipientId,
            IsRead = false,
            ReadAt = null
        };

        await _unitOfWork.SystemNotificationRecipientRepository
            .CreateAsync(recipient);

        await _unitOfWork.SaveChangesAsync();

        return notification;
    }

    public async Task<SystemNotification> NotifyFarmerApprovedRegistrationAsync(
        Guid recipientId, Guid senderId, string companyName, string content)
    {
        var title = "Đơn đăng ký tham gia kế hoạch của bạn đã được duyệt";
        var message = $"Doanh nghiệp {companyName} đã duyệt vào đơn đăng ký {content}";

        var notification = new SystemNotification
        {
            NotificationId = Guid.NewGuid(),
            NotificationCode = await _codeGenerator.GenerateNotificationCodeAsync(),
            Title = title,
            Message = message,
            Type = "ApprovedRegistration",
            CreatedAt = DateHelper.NowVietnamTime(),
            CreatedBy = senderId
        };

        await _unitOfWork.SystemNotificationRepository
            .CreateAsync(notification);

        var recipient = new SystemNotificationRecipient
        {
            Id = Guid.NewGuid(),
            NotificationId = notification.NotificationId,
            RecipientId = recipientId,
            IsRead = false,
            ReadAt = null
        };

        await _unitOfWork.SystemNotificationRecipientRepository
            .CreateAsync(recipient);

        await _unitOfWork.SaveChangesAsync();

        return notification;
    }

    public async Task<SystemNotification> NotifyFarmerRejectedRegistrationAsync(
        Guid recipientId, Guid senderId, string companyName, string content)
    {
        var title = "Đơn đăng ký tham gia kế hoạch của bạn đã bị từ chối";
        var message = $"Doanh nghiệp {companyName} đã từ chối đơn đăng ký {content}";

        var notification = new SystemNotification
        {
            NotificationId = Guid.NewGuid(),
            NotificationCode = await _codeGenerator.GenerateNotificationCodeAsync(),
            Title = title,
            Message = message,
            Type = "RejectedRegistration",
            CreatedAt = DateHelper.NowVietnamTime(),
            CreatedBy = senderId
        };

        await _unitOfWork.SystemNotificationRepository
            .CreateAsync(notification);

        var recipient = new SystemNotificationRecipient
        {
            Id = Guid.NewGuid(),
            NotificationId = notification.NotificationId,
            RecipientId = recipientId,
            IsRead = false,
            ReadAt = null
        };

        await _unitOfWork.SystemNotificationRecipientRepository
            .CreateAsync(recipient);

        await _unitOfWork.SaveChangesAsync();

        return notification;
    }

    public async Task<SystemNotification> NotifyFarmerNewCommitmentAsync(
        Guid recipientId, Guid senderId, string companyName, string content)
    {
        var title = "Bạn có đơn cam kết mới";
        var message = $"Doanh nghiệp {companyName} đã tạo cam kết với bạn {content}";

        var notification = new SystemNotification
        {
            NotificationId = Guid.NewGuid(),
            NotificationCode = await _codeGenerator.GenerateNotificationCodeAsync(),
            Title = title,
            Message = message,
            Type = "NewCommitment",
            CreatedAt = DateHelper.NowVietnamTime(),
            CreatedBy = senderId
        };

        await _unitOfWork.SystemNotificationRepository
            .CreateAsync(notification);

        var recipient = new SystemNotificationRecipient
        {
            Id = Guid.NewGuid(),
            NotificationId = notification.NotificationId,
            RecipientId = recipientId,
            IsRead = false,
            ReadAt = null
        };

        await _unitOfWork.SystemNotificationRecipientRepository
            .CreateAsync(recipient);

        await _unitOfWork.SaveChangesAsync();

        return notification;
    }

    public async Task<SystemNotification> NotifyFarmerUpdatedCommitmentAsync(
        Guid recipientId, Guid senderId, string companyName, string content)
    {
        var title = "Cam kết của bạn đã được doanh nghiệp cập nhật";
        var message = $"Doanh nghiệp {companyName} vừa cập nhật lại cam kết với bạn {content}";

        var notification = new SystemNotification
        {
            NotificationId = Guid.NewGuid(),
            NotificationCode = await _codeGenerator.GenerateNotificationCodeAsync(),
            Title = title,
            Message = message,
            Type = "UpdatedCommitment",
            CreatedAt = DateHelper.NowVietnamTime(),
            CreatedBy = senderId
        };

        await _unitOfWork.SystemNotificationRepository
            .CreateAsync(notification);

        var recipient = new SystemNotificationRecipient
        {
            Id = Guid.NewGuid(),
            NotificationId = notification.NotificationId,
            RecipientId = recipientId,
            IsRead = false,
            ReadAt = null
        };

        await _unitOfWork.SystemNotificationRecipientRepository
            .CreateAsync(recipient);

        await _unitOfWork.SaveChangesAsync();

        return notification;
    }

    public async Task<SystemNotification> NotifyManagerApprovedCommitmentAsync(
        Guid recipientId, Guid senderId, string farmerName, string content)
    {
        var title = "Cam kết của bạn đã được nông dân chấp nhận";
        var message = $"Nông dân {farmerName} vừa đã chấp nhận cam kết với bạn {content}";

        var notification = new SystemNotification
        {
            NotificationId = Guid.NewGuid(),
            NotificationCode = await _codeGenerator.GenerateNotificationCodeAsync(),
            Title = title,
            Message = message,
            Type = "ApprovedCommitment",
            CreatedAt = DateHelper.NowVietnamTime(),
            CreatedBy = senderId
        };

        await _unitOfWork.SystemNotificationRepository
            .CreateAsync(notification);

        var recipient = new SystemNotificationRecipient
        {
            Id = Guid.NewGuid(),
            NotificationId = notification.NotificationId,
            RecipientId = recipientId,
            IsRead = false,
            ReadAt = null
        };

        await _unitOfWork.SystemNotificationRecipientRepository
            .CreateAsync(recipient);

        await _unitOfWork.SaveChangesAsync();

        return notification;
    }

    public async Task<SystemNotification> NotifyManagerRejectedCommitmentAsync(
        Guid recipientId, Guid senderId, string farmerName, string content)
    {
        var title = "Cam kết của bạn đã được nông dân từ chối";
        var message = $"Nông dân {farmerName} vừa đã từ chối cam kết với bạn {content}";

        var notification = new SystemNotification
        {
            NotificationId = Guid.NewGuid(),
            NotificationCode = await _codeGenerator.GenerateNotificationCodeAsync(),
            Title = title,
            Message = message,
            Type = "ApprovedCommitment",
            CreatedAt = DateHelper.NowVietnamTime(),
            CreatedBy = senderId
        };

        await _unitOfWork.SystemNotificationRepository
            .CreateAsync(notification);

        var recipient = new SystemNotificationRecipient
        {
            Id = Guid.NewGuid(),
            NotificationId = notification.NotificationId,
            RecipientId = recipientId,
            IsRead = false,
            ReadAt = null
        };

        await _unitOfWork.SystemNotificationRecipientRepository
            .CreateAsync(recipient);

        await _unitOfWork.SaveChangesAsync();

        return notification;
    }

    public async Task<SystemNotification> NotifyManagerCommitmentCouldNotBeAcceptAsync(
        Guid recipientId, Guid senderId, string farmerName, string content)
    {
        var title = "Nông dân không thể chấp nhận cam kết của bạn";
        var message = $"Nông dân {farmerName} đã đồng ý cam kết với bạn {content}";

        var notification = new SystemNotification
        {
            NotificationId = Guid.NewGuid(),
            NotificationCode = await _codeGenerator.GenerateNotificationCodeAsync(),
            Title = title,
            Message = message,
            Type = "CouldNotAcceptCommitment",
            CreatedAt = DateHelper.NowVietnamTime(),
            CreatedBy = senderId
        };

        await _unitOfWork.SystemNotificationRepository
            .CreateAsync(notification);

        var recipient = new SystemNotificationRecipient
        {
            Id = Guid.NewGuid(),
            NotificationId = notification.NotificationId,
            RecipientId = recipientId,
            IsRead = false,
            ReadAt = null
        };

        await _unitOfWork.SystemNotificationRecipientRepository
            .CreateAsync(recipient);

        await _unitOfWork.SaveChangesAsync();

        return notification;
    }

    public async Task<SystemNotification> NotifyExpertAdviceCreatedAsync(
        Guid reportId, Guid expertId, string expertName, string adviceText)
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
            CreatedAt = DateHelper.NowVietnamTime(),
            CreatedBy = expertId
        };

        await _unitOfWork.SystemNotificationRepository
            .CreateAsync(notification);

        // Lấy thông tin Farmer từ báo cáo
        var report = await _unitOfWork.GeneralFarmerReportRepository.GetByIdAsync(
            predicate: r => 
               r.ReportId == reportId && 
               !r.IsDeleted,
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

            await _unitOfWork.SystemNotificationRecipientRepository
                .CreateAsync(recipient);
        }

        await _unitOfWork.SaveChangesAsync();

        return notification;
    }

    public async Task<SystemNotification> NotifyShipmentStatusUpdatedAsync(
        Guid shipmentId, Guid orderId, string shipmentCode, string orderCode, string oldStatus, string newStatus, Guid businessManagerUserId, string deliveryStaffName = null)
    {
        var title = "📦 Cập nhật trạng thái giao hàng";
        
        // Tạo message song ngữ - chỉ hiển thị trạng thái hiện tại
        var deliveryStaffInfo = !string.IsNullOrEmpty(deliveryStaffName) 
            ? $" bởi nhân viên {deliveryStaffName}" 
            : "";
            
        // Chuyển đổi trạng thái sang tiếng Việt
        var statusInVietnamese = ConvertStatusToVietnamese(newStatus);
            
        var message = $"Chuyến giao hàng {shipmentCode} (Đơn hàng: {orderCode}) đã được cập nhật thành trạng thái '{statusInVietnamese}'{deliveryStaffInfo}.";
        
        // Message tiếng Anh
        var messageEn = $"Shipment {shipmentCode} (Order: {orderCode}) has been updated to status '{newStatus}'{deliveryStaffInfo}.";

        var notification = new SystemNotification
        {
            NotificationId = Guid.NewGuid(),
            NotificationCode = await _codeGenerator.GenerateNotificationCodeAsync(),
            Title = title,
            Message = message,
            Type = "ShipmentStatusUpdate",
            CreatedAt = DateHelper.NowVietnamTime(),
            CreatedBy = null
        };

        await _unitOfWork.SystemNotificationRepository
            .CreateAsync(notification);

        // Tạo recipient cho BusinessManager (businessManagerUserId là UserId)
        var recipient = new SystemNotificationRecipient
        {
            Id = Guid.NewGuid(),
            NotificationId = notification.NotificationId,
            RecipientId = businessManagerUserId,  // businessManagerUserId là UserId từ BusinessManager.User.UserId
            IsRead = false,
            ReadAt = null
        };

        await _unitOfWork.SystemNotificationRecipientRepository
            .CreateAsync(recipient);

        // Gửi email nếu có (businessManagerUserId là UserId)
        var user = await _unitOfWork.UserAccountRepository.GetByIdAsync(
            predicate: u => 
               u.UserId == businessManagerUserId && 
               !u.IsDeleted,
            asNoTracking: true
        );

        if (user != null && 
            !string.IsNullOrWhiteSpace(user.Email))
        {
            var emailMessage = $"{message}\n\nChi tiết:\n- Mã chuyến giao: {shipmentCode}\n- Mã đơn hàng: {orderCode}\n- Trạng thái hiện tại: {statusInVietnamese}\n\nVui lòng đăng nhập vào hệ thống để xem chi tiết.";
            
            var emailMessageEn = $"{messageEn}\n\nDetails:\n- Shipment Code: {shipmentCode}\n- Order Code: {orderCode}\n- Current Status: {newStatus}\n\nPlease login to the system to view details.";

            // Gửi email song ngữ
            await _emailService.SendEmailAsync(user.Email, title, emailMessage + "\n\n---\n" + emailMessageEn);
        }

        await _unitOfWork.SaveChangesAsync();

        return notification;
    }

    public async Task<SystemNotification> NotifyBusinessBuyerOutboundRequestReadyAsync(Guid outboundRequestId, string outboundRequestCode, string companyName, string buyerEmail, string productName, double quantity, string unit)
    {
        // Lấy thông tin thời gian từ database
        var outboundRequest = await _unitOfWork.WarehouseOutboundRequests.GetByIdAsync(outboundRequestId);
        var currentTime = DateHelper.NowVietnamTime();
        var outboundCreatedDate = outboundRequest?.CreatedAt ?? currentTime;
        
        var title = "📦 Hàng đã sẵn sàng để lấy";
        var message = $"Kính gửi quý công ty {companyName},\n\nHàng hóa đã được chuẩn bị sẵn sàng và có thể đến lấy tại kho của chúng tôi.\n\nThông tin chi tiết:\n- Mã phiếu xuất kho: {outboundRequestCode}\n- Sản phẩm: {productName}\n- Số lượng: {quantity:n0} {unit}\n- Ngày tạo phiếu xuất kho: {outboundCreatedDate:dd/MM/yyyy HH:mm}\n- Thời gian gửi thông báo: {currentTime:dd/MM/yyyy HH:mm}\n\nVui lòng liên hệ với chúng tôi để sắp xếp thời gian đến lấy hàng.\n\nTrân trọng,\nĐội ngũ DakLak Coffee Supply Chain";

        var notification = new SystemNotification
        {
            NotificationId = Guid.NewGuid(),
            NotificationCode = await _codeGenerator.GenerateNotificationCodeAsync(),
            Title = title,
            Message = message,
            Type = "BusinessBuyerOutboundReady",
            CreatedAt = DateHelper.NowVietnamTime(),
            CreatedBy = null
        };

        await _unitOfWork.SystemNotificationRepository.CreateAsync(notification);

        // Gửi email cho business buyer
        if (!string.IsNullOrWhiteSpace(buyerEmail))
        {
            var emailSubject = $"🚚 Thông báo hàng sẵn sàng lấy - Phiếu xuất kho {outboundRequestCode}";
            var emailBody = $@"
                <html>
                <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
                    <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                        <h2 style='color: #2c5530; border-bottom: 2px solid #2c5530; padding-bottom: 10px;'>
                            🚚 Thông báo hàng sẵn sàng lấy
                        </h2>
                        
                        <p>Kính gửi quý công ty <strong>{companyName}</strong>,</p>
                        
                        <p>Chúng tôi xin thông báo rằng hàng hóa đã được chuẩn bị sẵn sàng và có thể đến lấy tại kho của chúng tôi.</p>
                        
                        <div style='background-color: #f8f9fa; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                            <h3 style='color: #2c5530; margin-top: 0;'>📋 Thông tin chi tiết:</h3>
                            <ul style='margin: 0; padding-left: 20px;'>
                                <li><strong>Mã phiếu xuất kho:</strong> {outboundRequestCode}</li>
                                <li><strong>Sản phẩm:</strong> {productName}</li>
                                <li><strong>Số lượng:</strong> {quantity:n0} {unit}</li>
                                <li><strong>Ngày tạo phiếu xuất kho:</strong> {outboundCreatedDate:dd/MM/yyyy HH:mm}</li>
                                <li><strong>Thời gian gửi thông báo:</strong> {currentTime:dd/MM/yyyy HH:mm}</li>
                            </ul>
                        </div>
                        
                        <div style='background-color: #fff3cd; padding: 15px; border-radius: 5px; border-left: 4px solid #ffc107; margin: 20px 0;'>
                            <p style='margin: 0;'><strong>⚠️ Lưu ý:</strong> Vui lòng liên hệ với chúng tôi để sắp xếp thời gian đến lấy hàng phù hợp.</p>
                        </div>
                        
                        <p>Nếu có bất kỳ thắc mắc nào, vui lòng liên hệ với chúng tôi qua hệ thống hoặc số điện thoại đã cung cấp .</p>
                        
                        <p>Trân trọng,<br>
                        <strong>Đội ngũ DakLak Coffee Supply Chain</strong></p>
                        
                        <hr style='border: none; border-top: 1px solid #eee; margin: 30px 0;'>
                        <p style='font-size: 12px; color: #666; text-align: center;'>
                            Email này được gửi tự động từ hệ thống DakLak Coffee Supply Chain Management
                        </p>
                    </div>
                </body>
                </html>";

            await _emailService.SendEmailAsync(buyerEmail, emailSubject, emailBody);
        }

        await _unitOfWork.SaveChangesAsync();

        return notification;
    }

    // Thêm các phương thức để Frontend gọi
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

            return new ServiceResult(
                Const.SUCCESS_READ_CODE, 
                Const.SUCCESS_READ_MSG, 
                result
            );
        }
        catch (Exception ex)
        {
            return new ServiceResult(
                Const.ERROR_EXCEPTION, 
                "Lỗi hệ thống: " + ex.Message
            );
        }
    }

    public async Task<IServiceResult> GetUnreadCountAsync(Guid userId)
    {
        try
        {
            var count = await _unitOfWork.SystemNotificationRecipientRepository.GetQuery()
                .Where(r => r.RecipientId == userId && !r.IsDeleted && (r.IsRead == null || r.IsRead == false))
                .CountAsync();

            return new ServiceResult(
                Const.SUCCESS_READ_CODE,
                Const.SUCCESS_READ_MSG, 
                count);
        }
        catch (Exception ex)
        {
            return new ServiceResult(
                Const.ERROR_EXCEPTION, 
                "Lỗi hệ thống: " + ex.Message
            );
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
            recipient.ReadAt = DateHelper.NowVietnamTime();

            _unitOfWork.SystemNotificationRecipientRepository
                .Update(recipient);

            await _unitOfWork.SaveChangesAsync();

            return new ServiceResult(
                Const.SUCCESS_UPDATE_CODE, 
                "Đã đánh dấu thông báo đã đọc."
            );
        }
        catch (Exception ex)
        {
            return new ServiceResult(
                Const.ERROR_EXCEPTION,
                "Lỗi hệ thống: " + ex.Message
            );
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
                recipient.ReadAt = DateHelper.NowVietnamTime();

                _unitOfWork.SystemNotificationRecipientRepository
                    .Update(recipient);
            }

            await _unitOfWork.SaveChangesAsync();

            return new ServiceResult(
                Const.SUCCESS_UPDATE_CODE, 
                $"Đã đánh dấu {unreadRecipients.Count} thông báo đã đọc."
            );
        }
        catch (Exception ex)
        {
            return new ServiceResult(
                Const.ERROR_EXCEPTION,
                "Lỗi hệ thống: " + ex.Message
            );
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

            return new ServiceResult(
                Const.SUCCESS_READ_CODE,
                Const.SUCCESS_READ_MSG, 
                notification
            );
        }
        catch (Exception ex)
        {
            return new ServiceResult(
                Const.ERROR_EXCEPTION, 
                "Lỗi hệ thống: " + ex.Message
            );
        }
    }

    /// <summary>
    /// Chuyển đổi trạng thái shipment từ tiếng Anh sang tiếng Việt
    /// </summary>
    private string ConvertStatusToVietnamese(string status)
    {
        return status?.ToLower() switch
        {
            "pending" => "Chờ xử lý",
            "preparing" => "Đang chuẩn bị",
            "intransit" => "Đang giao hàng",
            "delivered" => "Đã giao hàng",
            "failed" => "Giao hàng thất bại",
            "canceled" => "Đã hủy",
            "cancelled" => "Đã hủy",
            "shipped" => "Đã xuất hàng",
            "processing" => "Đang xử lý",
            "completed" => "Hoàn thành",
            "returned" => "Đã trả hàng",
            "refunded" => "Đã hoàn tiền",
            _ => status // Nếu không tìm thấy, giữ nguyên giá trị gốc
        };
    }
}
