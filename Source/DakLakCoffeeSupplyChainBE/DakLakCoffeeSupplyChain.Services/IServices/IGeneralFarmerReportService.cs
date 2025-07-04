﻿using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.GeneralFarmerReportDTOs;
using DakLakCoffeeSupplyChain.Services.Base;
using System;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Services.IServices
{
    public interface IGeneralFarmerReportService
    {
        Task<IServiceResult> GetAll();
        Task<IServiceResult> GetById(Guid reportId);
        Task<IServiceResult> CreateGeneralFarmerReports(GeneralFarmerReportCreateDto dto);

    }
}
