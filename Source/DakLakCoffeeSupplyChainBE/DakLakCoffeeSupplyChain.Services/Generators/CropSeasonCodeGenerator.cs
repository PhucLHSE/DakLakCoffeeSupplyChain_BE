using DakLakCoffeeSupplyChain.Repositories.UnitOfWork;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Services.Generators
{
    public class CropSeasonCodeGenerator : ICropSeasonCodeGenerator
    {
        private readonly IUnitOfWork _unitOfWork;

        public CropSeasonCodeGenerator(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<string> GenerateCropSeasonCodeAsync(int year)
        {
            int count = await _unitOfWork.CropSeasonRepository.CountByYearAsync(year);
            return $"SEASON-{year}-{(count + 1):D4}";
        }
    }
}
