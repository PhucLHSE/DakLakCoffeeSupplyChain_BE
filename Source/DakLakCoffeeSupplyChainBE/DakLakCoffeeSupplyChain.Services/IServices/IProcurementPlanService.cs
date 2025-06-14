﻿using DakLakCoffeeSupplyChain.Services.Base;

namespace DakLakCoffeeSupplyChain.Services.IServices
{
    public interface IProcurementPlanService
    {
        Task<IServiceResult> GetAllProcurementPlansAvailable();
        Task<IServiceResult> GetAll();
        Task<IServiceResult> GetById(Guid planId);
        Task<IServiceResult> DeleteById(Guid planId);
    }
}
