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
        Task<SystemNotification> NotifyInboundRequestCreatedAsync(Guid inboundRequestId, Guid farmerId);
        Task<SystemNotification> NotifyInboundRequestApprovedAsync(Guid requestId, Guid farmerId);
    }
}
