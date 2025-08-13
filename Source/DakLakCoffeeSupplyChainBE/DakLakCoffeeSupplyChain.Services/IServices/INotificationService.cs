using DakLakCoffeeSupplyChain.Repositories.Models;
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
    }
}
