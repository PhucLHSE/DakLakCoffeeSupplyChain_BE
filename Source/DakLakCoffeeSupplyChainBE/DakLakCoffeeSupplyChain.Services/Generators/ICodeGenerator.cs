namespace DakLakCoffeeSupplyChain.Services.Generators
{
    public interface ICodeGenerator
    {
        Task<string> GenerateUserCodeAsync();

        Task<string> GenerateManagerCodeAsync();

        Task<string> GenerateBuyerCodeAsync(Guid managerId);

        Task<string> GenerateContractCodeAsync();

        Task<string> GenerateContractItemCodeAsync(Guid contractId);

        Task<string> GenerateDeliveryBatchCodeAsync();

        Task<string> GenerateContractDeliveryItemCodeAsync(Guid deliveryBatchId);

        Task<string> GenerateCropSeasonCodeAsync(int year);

        Task<string> GenerateProcurementPlanCodeAsync();

        Task<string> GenerateProcurementPlanDetailsCodeAsync();

        Task<string> GenerateCoffeeTypeCodeAsync();
      
        Task<string> GenerateFarmerCodeAsync();

        Task<string> GenerateGeneralFarmerReportCodeAsync();

        Task<string> GenerateCultivationRegistrationCodeAsync();

        Task<string> GenerateStaffCodeAsync();

        Task<string> GenerateInboundRequestCodeAsync();

        Task<string> GenerateWarehouseCodeAsync();

        Task<string> GenerateWarehouseReceiptCodeAsync();

        Task<string> GenerateInventoryCodeAsync();
    }
}
