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
        /// Tạo Payment record với txnRef cụ thể
        /// </summary>
        /// <param name="planId">Plan ID</param>
        /// <param name="paymentConfig">Payment configuration</param>
        /// <param name="userEmail">User email</param>
        /// <param name="userId">User ID</param>
        /// <param name="txnRef">Transaction reference</param>
        /// <returns>Payment object</returns>
        Payment CreatePaymentRecordWithTxnRef(Guid planId, PaymentConfiguration paymentConfig, string userEmail, string userId, string txnRef);

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
        //Task ProcessMockIpnAsync(Guid planId, string txnRef, PaymentConfiguration paymentConfig);

        /// <summary>
        /// Lấy hoặc tạo ví System (Admin wallet)
        /// </summary>
        /// <returns>System wallet</returns>
        Task<Wallet> GetOrCreateSystemWalletAsync();
        
        /// <summary>
        /// Cộng tiền vào ví System khi thanh toán phí thành công
        /// </summary>
        /// <param name="paymentId">Payment ID</param>
        /// <param name="amount">Số tiền</param>
        /// <param name="description">Mô tả</param>
        /// <returns>Task</returns>
        Task AddToSystemWalletAsync(Guid paymentId, double amount, string description);

        /// <summary>
        /// Tạo Wallet Transaction cho Admin khi thanh toán phí đăng ký kế hoạch qua VNPay
        /// </summary>
        /// <param name="paymentId">Payment ID</param>
        /// <param name="amount">Số tiền</param>
        /// <param name="userId">User ID của người trả phí</param>
        /// <param name="planId">Plan ID</param>
        /// <param name="description">Mô tả</param>
        /// <returns>Task</returns>
        Task CreatePlanPostingFeeTransactionsAsync(Guid paymentId, double amount, Guid userId, Guid planId, string description);

        /// <summary>
        /// Tạo Wallet Transaction cho cả Admin và User khi thanh toán phí bằng ví
        /// </summary>
        /// <param name="paymentId">Payment ID</param>
        /// <param name="amount">Số tiền</param>
        /// <param name="userId">User ID của người trả phí</param>
        /// <param name="planId">Plan ID</param>
        /// <param name="description">Mô tả</param>
        /// <returns>Task</returns>
        Task CreatePlanPostingFeeTransactionsFromWalletAsync(Guid paymentId, double amount, Guid userId, Guid planId, string description);

        /// <summary>
        /// Tạo Wallet Transaction dựa trên loại thanh toán
        /// </summary>
        /// <param name="paymentId">Payment ID</param>
        /// <param name="amount">Số tiền</param>
        /// <param name="userId">User ID của người trả phí</param>
        /// <param name="planId">Plan ID</param>
        /// <param name="description">Mô tả</param>
        /// <param name="paymentMethod">Phương thức thanh toán (VNPay, Wallet, etc.)</param>
        /// <returns>Task</returns>
        Task CreatePlanPostingFeeTransactionsByMethodAsync(Guid paymentId, double amount, Guid userId, Guid planId, string description, string paymentMethod);

        /// <summary>
        /// Lấy hoặc tạo ví của User
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>User wallet</returns>
        Task<Wallet> GetOrCreateUserWalletAsync(Guid userId);

        /// <summary>
        /// Xử lý thanh toán qua ví nội bộ
        /// </summary>
        /// <param name="planId">ID kế hoạch thu mua</param>
        /// <param name="amount">Số tiền thanh toán</param>
        /// <param name="userId">ID người dùng</param>
        /// <param name="description">Mô tả giao dịch</param>
        /// <returns>Kết quả thanh toán</returns>
        Task<(bool Success, string Message, string? TransactionId)> ProcessWalletPaymentAsync(Guid planId, double amount, Guid userId, string? description = null);
        Task<(string RspCode, string Message)> ProcessRealIpnAsync(Dictionary<string, string> vnpParams);
    }
}
