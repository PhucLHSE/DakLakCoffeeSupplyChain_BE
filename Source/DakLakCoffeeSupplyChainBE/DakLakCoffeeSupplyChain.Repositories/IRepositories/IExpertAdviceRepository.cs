using DakLakCoffeeSupplyChain.Repositories.Models;
using DakLakCoffeeSupplyChain.Repositories.Base;

namespace DakLakCoffeeSupplyChain.Repositories.IRepositories
{
    public interface IExpertAdviceRepository : IGenericRepository<ExpertAdvice>
    {
        Task AddAsync(ExpertAdvice entity);

        void Update(ExpertAdvice entity);

        void Delete(ExpertAdvice entity);
    }
}
