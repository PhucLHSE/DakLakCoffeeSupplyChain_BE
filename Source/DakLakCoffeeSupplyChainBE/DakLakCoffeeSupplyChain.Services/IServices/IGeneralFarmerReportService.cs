using DakLakCoffeeSupplyChain.Common;
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
        Task<IServiceResult> CreateGeneralFarmerReports(GeneralFarmerReportCreateDto dto, Guid userId);
        Task<IServiceResult> UpdateGeneralFarmerReport(GeneralFarmerReportUpdateDto dto);
        Task<IServiceResult> SoftDeleteGeneralFarmerReport(Guid reportId);
        Task<IServiceResult> HardDeleteGeneralFarmerReport(Guid reportId);
        Task<IServiceResult> ResolveGeneralFarmerReportAsync(Guid reportId, Guid expertId);
    }
}
