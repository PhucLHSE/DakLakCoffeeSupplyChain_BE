using DakLakCoffeeSupplyChain.Repositories.Base;
using DakLakCoffeeSupplyChain.Repositories.DBContext;
using DakLakCoffeeSupplyChain.Repositories.IRepositories;
using DakLakCoffeeSupplyChain.Repositories.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Repositories.Repositories
{
    public class SystemNotificationRecipientRepository : GenericRepository<SystemNotificationRecipient>, ISystemNotificationRecipientRepository
    {
        private readonly DakLakCoffee_SCMContext _context;

        public SystemNotificationRecipientRepository(DakLakCoffee_SCMContext context) : base(context) 
        {
            _context = context;
        }

        // ✅ Thêm method để lấy query
        public IQueryable<SystemNotificationRecipient> GetQuery()
        {
            return _context.SystemNotificationRecipients.AsQueryable();
        }

        // ✅ Thêm method Update
        public void Update(SystemNotificationRecipient entity)
        {
            _context.SystemNotificationRecipients.Update(entity);
        }

        // ✅ Thêm method CreateAsync
        public async Task CreateAsync(SystemNotificationRecipient entity)
        {
            await _context.SystemNotificationRecipients.AddAsync(entity);
        }
    }
}
