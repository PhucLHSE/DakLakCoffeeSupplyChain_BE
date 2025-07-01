using DakLakCoffeeSupplyChain.Services.Base;

public interface IExpertAdviceService
{
    Task<IServiceResult> GetAllAsync();
    Task<IServiceResult> GetByIdAsync(Guid adviceId);
}
