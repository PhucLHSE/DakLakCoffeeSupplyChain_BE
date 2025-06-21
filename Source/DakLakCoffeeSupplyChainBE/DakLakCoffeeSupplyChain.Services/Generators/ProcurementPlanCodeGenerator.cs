using DakLakCoffeeSupplyChain.Repositories.UnitOfWork;

namespace DakLakCoffeeSupplyChain.Services.Generators
{
    public class ProcurementPlanCodeGenerator (IUnitOfWork unitOfWork) : IProcurementPlanCodeGenerator
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        public async Task<string> GenerateProcurementPlanCodeAsync()
        {
            var currentYear = DateTime.UtcNow.Year;

            // Đếm số procurement plan tạo trong năm
            var count = await _unitOfWork.ProcurementPlanRepository.CountProcurementPlansInYearAsync(currentYear);

            return $"PLAN-{currentYear}-{(count + 1):D4}";
        }

        public async Task<string> GenerateProcurementPlanDetailsCodeAsync()
        {
            var currentYear = DateTime.UtcNow.Year;

            // Đếm số procurement plan detail tạo trong năm
            var count = await _unitOfWork.ProcurementPlanDetailsRepository.CountProcurementPlanDetailsInYearAsync(currentYear);

            return $"PLD-{currentYear}-{(count + 1):D4}";
        }
    }
}
