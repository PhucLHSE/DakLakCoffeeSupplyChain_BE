namespace DakLakCoffeeSupplyChain.Services.Generators
{
    public interface ICodeGenerator
    {
        Task<string> GenerateUserCodeAsync();

        Task<string> GenerateManagerCodeAsync();

        Task<string> GenerateBuyerCodeAsync(Guid managerId);
        Task<string> GenerateCropSeasonCodeAsync(int year);
        Task<string> GenerateProcurementPlanCodeAsync();
        Task<string> GenerateProcurementPlanDetailsCodeAsync();

    }
}
