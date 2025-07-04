﻿using DakLakCoffeeSupplyChain.Services.Base;

namespace DakLakCoffeeSupplyChain.Services.IServices
{
    public interface IFarmerService
    {
        Task<IServiceResult> GetAll();
        Task<IServiceResult> GetById(Guid farmerId);
    }
}
