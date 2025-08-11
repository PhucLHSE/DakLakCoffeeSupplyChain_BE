using DakLakCoffeeSupplyChain.Repositories.Base;
using DakLakCoffeeSupplyChain.Repositories.Models;

namespace DakLakCoffeeSupplyChain.Repositories.IRepositories
{
    public interface ICultivationRegistrationRepository : IGenericRepository<CultivationRegistration>
    {
        Task<CultivationRegistration?> GetByIdAsync(Guid id);

        Task<CropSeasonDetail?> GetCropSeasonDetailByIdAsync(Guid cropSeasonDetailId);

        Task<int> CountCultivationRegistrationsInYearAsync(int year);
    }
}
