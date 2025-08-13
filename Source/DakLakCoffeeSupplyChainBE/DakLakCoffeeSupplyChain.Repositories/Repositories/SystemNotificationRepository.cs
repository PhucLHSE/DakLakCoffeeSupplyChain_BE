using DakLakCoffeeSupplyChain.Repositories.Base;
using DakLakCoffeeSupplyChain.Repositories.DBContext;
using DakLakCoffeeSupplyChain.Repositories.IRepositories;
using DakLakCoffeeSupplyChain.Repositories.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Repositories.Repositories
{
    public class SystemNotificationRepository : GenericRepository<SystemNotification>, ISystemNotificationRepository
    {
        private readonly DakLakCoffee_SCMContext _context;

        public SystemNotificationRepository(DakLakCoffee_SCMContext context) : base(context)
        {
            _context = context;
        }

        public IQueryable<SystemNotification> GetQuery()
        {
            return _context.SystemNotifications.AsQueryable();
        }

        public async Task CreateAsync(SystemNotification entity)
        {
            await _context.SystemNotifications.AddAsync(entity);
        }
    }
}
