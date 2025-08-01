﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Repositories.IRepositories
{

    using global::DakLakCoffeeSupplyChain.Repositories.Base;
    using global::DakLakCoffeeSupplyChain.Repositories.Models;

    namespace DakLakCoffeeSupplyChain.Repositories.IRepositories
    {
        public interface IProcessingBatchProgressRepository : IGenericRepository<ProcessingBatchProgress>
        {
            Task<List<ProcessingBatchProgress>> GetAllWithIncludesAsync();
            Task<ProcessingBatchProgress?> GetByIdAsync(Guid id);
            Task<bool> UpdateAsync(ProcessingBatchProgress entity);
            Task<bool> SoftDeleteAsync(Guid progressId);
            Task<bool> HardDeleteAsync(Guid progressId);
            //Task<ProcessingBatchProgress?> GetByIdWithBatchAsync(Guid progressId);
        }
    }
}
