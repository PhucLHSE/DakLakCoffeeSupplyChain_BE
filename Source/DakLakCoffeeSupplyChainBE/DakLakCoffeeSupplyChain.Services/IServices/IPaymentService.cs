using DakLakCoffeeSupplyChain.Repositories.Models;

namespace DakLakCoffeeSupplyChain.Services.IServices
{
    public interface IPaymentService
    {
        /// <summary>
        /// Lấy RoleId từ JWT token của user hiện tại
        /// </summary>
        /// <returns>RoleId hoặc null nếu không tìm thấy</returns>
        int? GetCurrentUserRoleId();

        /// <summary>
        /// Lấy thông tin user từ JWT token
        /// </summary>
        /// <returns>Tuple chứa (email, userId)</returns>
        (string email, string userId) GetCurrentUserInfo();

        /// <summary>
        /// Lấy IP address của client
        /// </summary>
        /// <returns>IP address</returns>
        string GetClientIpAddress();

        /// <summary>
        /// Lấy PaymentConfiguration dựa vào RoleId và FeeType
        /// </summary>
        /// <param name="roleId">ID của role</param>
        /// <param name="feeType">Loại phí</param>
        /// <returns>PaymentConfiguration phù hợp hoặc null nếu không tìm thấy</returns>
        Task<PaymentConfiguration?> GetPaymentConfigurationByContext(int roleId, string feeType);

        /// <summary>
        /// Tạo Payment record
        /// </summary>
        /// <param name="planId">Plan ID</param>
        /// <param name="paymentConfig">Payment configuration</param>
        /// <param name="userEmail">User email</param>
        /// <param name="userId">User ID</param>
        /// <returns>Payment object</returns>
        Payment CreatePaymentRecord(Guid planId, PaymentConfiguration paymentConfig, string userEmail, string userId);

        /// <summary>
        /// Lưu Payment vào database
        /// </summary>
        /// <param name="payment">Payment object</param>
        /// <returns>Task</returns>
        Task SavePaymentAsync(Payment payment);

        /// <summary>
        /// Kiểm tra kế hoạch có tồn tại và thuộc về user không
        /// </summary>
        /// <param name="planId">Plan ID</param>
        /// <param name="userId">User ID</param>
        /// <returns>ProcurementPlan hoặc null</returns>
        Task<ProcurementPlan?> ValidatePlanOwnership(Guid planId, string userId);

        /// <summary>
        /// Tạo Payment record cho wallet topup
        /// </summary>
        /// <param name="walletId">Wallet ID</param>
        /// <param name="amount">Amount</param>
        /// <param name="txnRef">Transaction reference</param>
        /// <param name="userEmail">User email</param>
        /// <param name="userId">User ID</param>
        /// <returns>Task</returns>
        Task CreateWalletTopupPaymentAsync(Guid walletId, int amount, string txnRef, string userEmail, string userId);

        /// <summary>
        /// Xử lý MockIpn - tạo hoặc cập nhật payment record
        /// </summary>
        /// <param name="planId">Plan ID</param>
        /// <param name="txnRef">Transaction reference</param>
        /// <param name="paymentConfig">Payment configuration</param>
        /// <returns>Task</returns>
        Task ProcessMockIpnAsync(Guid planId, string txnRef, PaymentConfiguration paymentConfig);
    }
}
