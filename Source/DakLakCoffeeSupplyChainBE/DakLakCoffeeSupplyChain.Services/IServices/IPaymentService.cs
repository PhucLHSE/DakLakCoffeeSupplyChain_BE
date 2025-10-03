using DakLakCoffeeSupplyChain.Repositories.Models;

namespace DakLakCoffeeSupplyChain.Services.IServices
{
    public interface IPaymentService
    {
      
        int? GetCurrentUserRoleId();

        (string email, string userId) GetCurrentUserInfo();

        string GetClientIpAddress();

       
        Task<PaymentConfiguration?> GetPaymentConfigurationByContext(int roleId, string feeType, Guid planId);


        Payment CreatePaymentRecord(Guid planId, PaymentConfiguration paymentConfig, string userEmail, string userId);

      
        Task<Payment> CreateOrUpdatePaymentRecordWithTxnRef(Guid planId, PaymentConfiguration paymentConfig, string userEmail, string userId, string txnRef);

        Task SavePaymentAsync(Payment payment);

       
        Task<ProcurementPlan?> ValidatePlanOwnership(Guid planId, string userId);

      
        Task CreateWalletTopupPaymentAsync(Guid walletId, int amount, string txnRef, string userEmail, string userId);

       
        Task<(bool Success, string NewTxnRef, string Message)> RecreateWalletTopupPaymentAsync(Guid paymentId, string userEmail, string userId);

        Task<Wallet> GetOrCreateSystemWalletAsync();
       
        Task AddToSystemWalletAsync(Guid paymentId, double amount, string description);

      
        Task CreatePlanPostingFeeTransactionsAsync(Guid paymentId, double amount, Guid userId, Guid planId, string description);

       
        Task CreatePlanPostingFeeTransactionsFromWalletAsync(Guid paymentId, double amount, Guid userId, Guid planId, string description);

        Task CreatePlanPostingFeeTransactionsByMethodAsync(Guid paymentId, double amount, Guid userId, Guid planId, string description, string paymentMethod);

        Task<Wallet> GetOrCreateUserWalletAsync(Guid userId);

       
        Task<(bool Success, string Message, string? TransactionId)> ProcessWalletPaymentAsync(Guid planId, double amount, Guid userId, string? description = null);
        Task<(string RspCode, string Message)> ProcessRealIpnAsync(Dictionary<string, string> vnpParams);

        Task<IEnumerable<Payment>> GetPaymentHistoryAsync(Guid userId);
    }
}
