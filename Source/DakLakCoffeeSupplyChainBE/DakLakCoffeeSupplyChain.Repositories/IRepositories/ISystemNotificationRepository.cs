using DakLakCoffeeSupplyChain.Repositories.Base;
using DakLakCoffeeSupplyChain.Repositories.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Repositories.IRepositories
{
    public interface ISystemNotificationRepository : IGenericRepository<SystemNotification>
    {
        IQueryable<SystemNotification> GetQuery();
        
        Task CreateAsync(SystemNotification entity);
    }
}
