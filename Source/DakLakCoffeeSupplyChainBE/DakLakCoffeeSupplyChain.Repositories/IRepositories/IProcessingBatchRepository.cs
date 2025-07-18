﻿using DakLakCoffeeSupplyChain.Repositories.Base;
using DakLakCoffeeSupplyChain.Repositories.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Repositories.IRepositories
{
    public interface IProcessingBatchRepository : IGenericRepository<ProcessingBatch>
    {
        Task<List<ProcessingBatch>> GetAll();
        IQueryable<ProcessingBatch> GetQueryable();
        Task<int> CountSystemBatchCreatedInYearAsync(int year);
    }
}
