using DakLakCoffeeSupplyChain.Repositories.Models;
using DakLakCoffeeSupplyChain.Services.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Services.IServices
{
    public interface INotificationService
    {
        Task<SystemNotification> NotifyInboundRequestCreatedAsync(Guid requestId, Guid farmerId);

        Task<SystemNotification> NotifyInboundRequestApprovedAsync(Guid requestId, Guid farmerId);

        Task<SystemNotification> NotifyOutboundRequestCreatedAsync(Guid requestId, Guid staffId);

        Task<SystemNotification> NotifyEvaluationFailedAsync(Guid batchId, Guid farmerId, string evaluationComments);
        
        Task<SystemNotification> NotifyFarmerReportCreatedAsync(Guid reportId, Guid farmerId, string reportTitle);

        Task<SystemNotification> NotifyExpertAdviceCreatedAsync(Guid reportId, Guid expertId, string expertName, string adviceText);

        Task<SystemNotification> NotifyShipmentStatusUpdatedAsync(Guid shipmentId, Guid orderId, string shipmentCode, string orderCode, string oldStatus, string newStatus, Guid businessManagerUserId, string deliveryStaffName = null);

        Task<IServiceResult> GetUserNotificationsAsync(Guid userId, int page, int pageSize);

        Task<IServiceResult> GetUnreadCountAsync(Guid userId);

        Task<IServiceResult> MarkAsReadAsync(Guid notificationId, Guid userId);

        Task<IServiceResult> MarkAllAsReadAsync(Guid userId);

        Task<IServiceResult> GetNotificationByIdAsync(Guid notificationId, Guid userId);

        Task<SystemNotification> NotifyManagerNewRegistrationdAsync(
        Guid recipientId, Guid senderId, string farmerName, string content);

        Task<SystemNotification> NotifyFarmerApprovedRegistrationAsync(
        Guid recipientId, Guid senderId, string companyName, string content);

        Task<SystemNotification> NotifyFarmerRejectedRegistrationAsync(
        Guid recipientId, Guid senderId, string companyName, string content);

        Task<SystemNotification> NotifyFarmerNewCommitmentAsync(
        Guid recipientId, Guid senderId, string companyName, string content);

        Task<SystemNotification> NotifyFarmerUpdatedCommitmentAsync(
        Guid recipientId, Guid senderId, string companyName, string content);

        Task<SystemNotification> NotifyManagerApprovedCommitmentAsync(
        Guid recipientId, Guid senderId, string farmerName, string content);

        Task<SystemNotification> NotifyManagerRejectedCommitmentAsync(
        Guid recipientId, Guid senderId, string farmerName, string content);

        Task<SystemNotification> NotifyManagerCommitmentCouldNotBeAcceptAsync(
        Guid recipientId, Guid senderId, string farmerName, string content);
    }
}
