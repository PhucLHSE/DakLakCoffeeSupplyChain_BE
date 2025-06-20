using DakLakCoffeeSupplyChain.Repositories.Base;

using DakLakCoffeeSupplyChain.Repositories.Models;

namespace DakLakCoffeeSupplyChain.Repositories.IRepositories
{
    public interface ICropStageRepository : IGenericRepository<CropStage>
    {
        Task<CropStage?> GetByCodeAsync(string code);
        Task<List<CropStage>> GetAllOrderedAsync();
        Task<CropStage?> GetByIdAsync(int stageId);

        Task DeleteCropCropStageByStageIdAsync(int stageId);

    }
}
