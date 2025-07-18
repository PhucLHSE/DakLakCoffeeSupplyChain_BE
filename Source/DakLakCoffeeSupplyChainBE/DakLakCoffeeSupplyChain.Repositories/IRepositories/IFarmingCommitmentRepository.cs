using DakLakCoffeeSupplyChain.Repositories.Base;
using DakLakCoffeeSupplyChain.Repositories.Models;

namespace DakLakCoffeeSupplyChain.Repositories.IRepositories
{
    public interface IFarmingCommitmentRepository : IGenericRepository<FarmingCommitment>
    {
        Task<FarmingCommitment?> GetByIdAsync(Guid id);
        Task<FarmingCommitment?> GetWithRegistrationAsync(Guid commitmentId);
        Task<FarmingCommitment?> GetByRegistrationDetailIdAsync(Guid registrationDetailId);
        Task<int> CountFarmingCommitmentsInYearAsync(int year);

    }
}
