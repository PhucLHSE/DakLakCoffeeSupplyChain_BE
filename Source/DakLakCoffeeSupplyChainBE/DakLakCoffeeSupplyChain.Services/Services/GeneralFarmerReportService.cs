using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Repositories.UnitOfWork;
using DakLakCoffeeSupplyChain.Services.Base;
using DakLakCoffeeSupplyChain.Services.IServices;
using DakLakCoffeeSupplyChain.Services.Mappers;

namespace DakLakCoffeeSupplyChain.Services.Services
{
    public class GeneralFarmerReportService : IGeneralFarmerReportService
    {
        private readonly IUnitOfWork _unitOfWork;

        public GeneralFarmerReportService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IServiceResult> GetAll()
        {
            var reports = await _unitOfWork.GeneralFarmerReportRepository.GetAllWithIncludesAsync();

            if (reports == null || !reports.Any())
                return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không có báo cáo nào.");

            var dtoList = reports.Select(r => r.MapToGeneralFarmerReportViewAllDto()).ToList();
            return new ServiceResult(Const.SUCCESS_READ_CODE, Const.SUCCESS_READ_MSG, dtoList);
        }

        public async Task<IServiceResult> GetById(Guid reportId)
        {
            var report = await _unitOfWork.GeneralFarmerReportRepository.GetByIdWithIncludesAsync(reportId);

            if (report == null)
                return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không tìm thấy báo cáo.");

            var dto = report.MapToGeneralFarmerReportViewDetailsDto();
            return new ServiceResult(Const.SUCCESS_READ_CODE, Const.SUCCESS_READ_MSG, dto);
        }
    }
}
