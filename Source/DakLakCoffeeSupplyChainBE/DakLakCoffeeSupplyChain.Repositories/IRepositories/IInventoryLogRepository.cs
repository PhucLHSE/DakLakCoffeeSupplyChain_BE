﻿using DakLakCoffeeSupplyChain.Repositories.Base;
using DakLakCoffeeSupplyChain.Repositories.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Repositories.IRepositories
{
    public interface IInventoryLogRepository : IGenericRepository<InventoryLog>
    {
        Task<IEnumerable<InventoryLog>> GetByInventoryIdAsync(Guid inventoryId);
        Task<IEnumerable<InventoryLog>> GetAllAsync();
        Task<InventoryLog?> GetByIdWithAllRelationsAsync(Guid logId);
    }
}
