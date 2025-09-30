using DakLakCoffeeSupplyChain.Common.Helpers;
using DakLakCoffeeSupplyChain.Repositories.Models;
using DakLakCoffeeSupplyChain.Repositories.UnitOfWork;
using DakLakCoffeeSupplyChain.Services.IServices;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DakLakCoffeeSupplyChain.Services.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public PaymentService(IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor)
        {
            _unitOfWork = unitOfWork;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Lấy RoleId từ JWT token của user hiện tại
        /// </summary>
        /// <returns>RoleId hoặc null nếu không tìm thấy</returns>
        public int? GetCurrentUserRoleId()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext?.User == null) return null;

            var roleClaim = httpContext.User.FindFirst(ClaimTypes.Role)?.Value;
            if (string.IsNullOrEmpty(roleClaim))
                return null;

            // Map role name to role ID
            return roleClaim.ToLower() switch
            {
                "businessmanager" => 2,
                "farmer" => 4,
                "admin" => 1,
                _ => null
            };
        }

        /// <summary>
        /// Lấy thông tin user từ JWT token
        /// </summary>
        /// <returns>Tuple chứa (email, userId)</returns>
        public (string email, string userId) GetCurrentUserInfo()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext?.User == null) return (string.Empty, string.Empty);

            var email = httpContext.User.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty;
            var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;

            return (email, userId);
        }

        /// <summary>
        /// Lấy IP address của client
        /// </summary>
        /// <returns>IP address</returns>
        public string GetClientIpAddress()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            return httpContext?.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
        }

        /// <summary>
        /// Lấy PaymentConfiguration dựa vào RoleId và FeeType
        /// </summary>
        /// <param name="roleId">ID của role</param>
        /// <param name="feeType">Loại phí</param>
        /// <returns>PaymentConfiguration phù hợp hoặc null nếu không tìm thấy</returns>
        public async Task<PaymentConfiguration?> GetPaymentConfigurationByContext(int roleId, string feeType)
        {
            var currentDate = DateOnly.FromDateTime(DateTime.UtcNow);
            
            var configs = await _unitOfWork.PaymentConfigurationRepository.GetAllAsync(
                predicate: pc => pc.RoleId == roleId && 
                                pc.FeeType == feeType && 
                                pc.IsActive == true &&
                                pc.IsDeleted == false &&
                                pc.EffectiveFrom <= currentDate &&
                                (pc.EffectiveTo == null || pc.EffectiveTo >= currentDate),
                orderBy: query => query.OrderByDescending(pc => pc.EffectiveFrom),
                asNoTracking: true
            );

            return configs.FirstOrDefault();
        }

        /// <summary>
        /// Tạo Payment record
        /// </summary>
        /// <param name="planId">Plan ID</param>
        /// <param name="paymentConfig">Payment configuration</param>
        /// <param name="userEmail">User email</param>
        /// <param name="userId">User ID</param>
        /// <returns>Payment object</returns>
        public Payment CreatePaymentRecord(Guid planId, PaymentConfiguration paymentConfig, string userEmail, string userId)
        {
            var now = DateTime.UtcNow;
            // Sử dụng txnRef làm PaymentCode để dễ dàng tìm kiếm sau này
            var txnRef = PaymentHelper.GenerateTxnRef(planId);

            return new Payment
            {
                PaymentId = Guid.NewGuid(),
                Email = userEmail,
                ConfigId = paymentConfig.ConfigId,
                UserId = !string.IsNullOrEmpty(userId) ? Guid.Parse(userId) : null,
                PaymentCode = txnRef.Length > 20 ? txnRef[..20] : txnRef, // Cắt 20 ký tự đầu để fit vào PaymentCode
                PaymentAmount = (int)paymentConfig.Amount,
                PaymentMethod = "VNPay",
                PaymentPurpose = "PlanPosting",
                PaymentStatus = "Success", // Chuyển thành Success ngay lập tức
                PaymentTime = now, // Thời gian thanh toán
                AdminVerified = false, // Chưa xác thực
                CreatedAt = now,
                UpdatedAt = now,
                RelatedEntityId = planId,
                IsDeleted = false
            };
        }

        /// <summary>
        /// Tạo Payment record với txnRef cụ thể
        /// </summary>
        /// <param name="planId">Plan ID</param>
        /// <param name="paymentConfig">Payment configuration</param>
        /// <param name="userEmail">User email</param>
        /// <param name="userId">User ID</param>
        /// <param name="txnRef">Transaction reference</param>
        /// <returns>Payment object</returns>
        public Payment CreatePaymentRecordWithTxnRef(Guid planId, PaymentConfiguration paymentConfig, string userEmail, string userId, string txnRef)
        {
            var now = DateTime.UtcNow;
            var paymentCode = txnRef.Length > 20 ? txnRef[..20] : txnRef;

            return new Payment
            {
                PaymentId = Guid.NewGuid(),
                Email = userEmail,
                ConfigId = paymentConfig.ConfigId,
                UserId = !string.IsNullOrEmpty(userId) ? Guid.Parse(userId) : null,
                PaymentCode = paymentCode,
                PaymentAmount = (int)paymentConfig.Amount,
                PaymentMethod = "VNPay",
                PaymentPurpose = "PlanPosting",
                PaymentStatus = "Pending", // <<< THAY ĐỔI TỪ "Success"
                PaymentTime = null,      // <<< THAY ĐỔI: null vì chưa thanh toán
                AdminVerified = false,
                CreatedAt = now,
                UpdatedAt = now,
                RelatedEntityId = planId,
                IsDeleted = false
            };
        }
        public async Task<(string RspCode, string Message)> ProcessRealIpnAsync(Dictionary<string, string> vnpParams)
        {
            // Ở Controller bạn đã validate chữ ký, nên ở đây xử lý nghiệp vụ
            var vnp_TxnRef = vnpParams.GetValueOrDefault("vnp_TxnRef") ?? "";
            var vnp_ResponseCode = vnpParams.GetValueOrDefault("vnp_ResponseCode");
            var vnp_AmountStr = vnpParams.GetValueOrDefault("vnp_Amount");
            var vnp_OrderInfo = vnpParams.GetValueOrDefault("vnp_OrderInfo") ?? "";

            if (string.IsNullOrEmpty(vnp_TxnRef) || string.IsNullOrEmpty(vnp_AmountStr))
                return ("99", "Missing fields");

            if (!long.TryParse(vnp_AmountStr, out var vnpAmountX100))
                return ("04", "Invalid amount");

            // PaymentCode trong DB cắt 20 ký tự đầu
            var paymentCode = vnp_TxnRef.Length > 20 ? vnp_TxnRef[..20] : vnp_TxnRef;

            // 1) Tìm payment
            var payment = (await _unitOfWork.PaymentRepository
                .GetAllAsync(p => p.PaymentCode == paymentCode && !p.IsDeleted))
                .FirstOrDefault();

            if (payment == null)
                return ("01", "Order not found");

            // 2) Idempotency
            if (!string.Equals(payment.PaymentStatus, "Pending", StringComparison.OrdinalIgnoreCase))
                return ("02", "Order already confirmed");

            // 3) So khớp amount (VNPay *100)
            var requiredAmountX100 = (long)payment.PaymentAmount * 100;
            if (vnpAmountX100 != requiredAmountX100)
                return ("04", "Invalid amount");

            // 4) Cập nhật trạng thái
            var now = DateTime.UtcNow;
            var isSuccess = vnp_ResponseCode == "00";
            payment.PaymentStatus = isSuccess ? "Success" : "Failed";
            payment.PaymentTime = isSuccess ? now : null;
            payment.UpdatedAt = now;

            await _unitOfWork.PaymentRepository.UpdateAsync(payment);
            await _unitOfWork.SaveChangesAsync();

            if (!isSuccess)
                return ("00", "Confirm Success"); // Không retry IPN

            // 5) Business theo OrderInfo
            if (vnp_OrderInfo.StartsWith("PlanPosting:", StringComparison.OrdinalIgnoreCase)
                && payment.RelatedEntityId.HasValue && payment.UserId.HasValue)
            {
                var plan = await _unitOfWork.ProcurementPlanRepository.GetByIdAsync(payment.RelatedEntityId.Value);
                var planName = plan?.Title ?? $"Plan ID: {payment.RelatedEntityId.Value}";

                await CreatePlanPostingFeeTransactionsByMethodAsync(
                    payment.PaymentId,
                    payment.PaymentAmount,
                    payment.UserId.Value,
                    payment.RelatedEntityId.Value,
                    planName,
                    "VNPay"
                );
            }
            else if (vnp_OrderInfo.StartsWith("WalletTopup:", StringComparison.OrdinalIgnoreCase)
                     && payment.RelatedEntityId.HasValue && payment.UserId.HasValue)
            {
                // Nạp tiền ví người dùng
                var walletId = payment.RelatedEntityId.Value;
                var wallet = await _unitOfWork.WalletRepository.GetByIdAsync(walletId);
                if (wallet != null && !wallet.IsDeleted)
                {
                    wallet.TotalBalance += payment.PaymentAmount;
                    wallet.LastUpdated = now;
                    await _unitOfWork.WalletRepository.UpdateAsync(wallet);

                    var topupTx = new WalletTransaction
                    {
                        TransactionId = Guid.NewGuid(),
                        WalletId = wallet.WalletId,
                        PaymentId = payment.PaymentId,
                        Amount = payment.PaymentAmount,
                        TransactionType = "Topup",
                        Description = "Nạp tiền vào ví qua VNPay (IPN)",
                        CreatedAt = now,
                        IsDeleted = false
                    };
                    await _unitOfWork.WalletTransactionRepository.CreateAsync(topupTx);
                    await _unitOfWork.SaveChangesAsync();
                }
            }

            return ("00", "Confirm Success");
        }



        /// <summary>
        /// Lưu Payment vào database
        /// </summary>
        /// <param name="payment">Payment object</param>
        /// <returns>Task</returns>
        public async Task SavePaymentAsync(Payment payment)
        {
            await _unitOfWork.PaymentRepository.CreateAsync(payment);
            await _unitOfWork.SaveChangesAsync();
        }

        /// <summary>
        /// Kiểm tra kế hoạch có tồn tại và thuộc về user không
        /// </summary>
        /// <param name="planId">Plan ID</param>
        /// <param name="userId">User ID</param>
        /// <returns>ProcurementPlan hoặc null</returns>
        public async Task<ProcurementPlan?> ValidatePlanOwnership(Guid planId, string userId)
        {
            var plan = await _unitOfWork.ProcurementPlanRepository.GetByIdAsync(
                predicate: p => p.PlanId == planId && !p.IsDeleted
            );

            if (plan == null) return null;

            // Check if plan belongs to user
            var user = await _unitOfWork.UserAccountRepository.GetByIdAsync(plan.CreatedBy);
            if (user?.UserId.ToString() != userId)
                return null;

            return plan;
        }

        /// <summary>
        /// Tạo Payment record cho wallet topup
        /// </summary>
        /// <param name="walletId">Wallet ID</param>
        /// <param name="amount">Amount</param>
        /// <param name="txnRef">Transaction reference</param>
        /// <param name="userEmail">User email</param>
        /// <param name="userId">User ID</param>
        /// <returns>Task</returns>
        public async Task CreateWalletTopupPaymentAsync(Guid walletId, int amount, string txnRef, string userEmail, string userId)
        {
            var cfg = (await _unitOfWork.PaymentConfigurationRepository.GetAllAsync()).FirstOrDefault();
            if (cfg == null) return;

            var now = DateTime.UtcNow;
            var payment = new DakLakCoffeeSupplyChain.Repositories.Models.Payment
            {
                PaymentId = Guid.NewGuid(),
                Email = userEmail,
                ConfigId = cfg.ConfigId,
                UserId = !string.IsNullOrEmpty(userId) ? Guid.Parse(userId) : null,
                PaymentCode = txnRef[..20], // Cắt 20 ký tự đầu để fit vào PaymentCode
                PaymentAmount = amount,
                PaymentMethod = "VNPay",
                PaymentPurpose = "WalletTopup",
                PaymentStatus = "Pending",
                PaymentTime = null,
                AdminVerified = false,
                CreatedAt = now,
                UpdatedAt = now,
                RelatedEntityId = walletId,
                IsDeleted = false
            };

            await _unitOfWork.PaymentRepository.CreateAsync(payment);
            await _unitOfWork.SaveChangesAsync();
        }

        /// <summary>
        /// Xử lý MockIpn - tạo hoặc cập nhật payment record và cộng tiền vào ví System
        /// </summary>
        /// <param name="planId">Plan ID</param>
        /// <param name="txnRef">Transaction reference</param>
        /// <param name="paymentConfig">Payment configuration</param>
        /// <returns>Task</returns>
        //public async Task ProcessMockIpnAsync(Guid planId, string txnRef, PaymentConfiguration paymentConfig)
        //{
        //    // Tìm payment bằng RelatedEntityId (planId) thay vì PaymentCode để tránh conflict
        //    var payment = (await _unitOfWork.PaymentRepository.GetAllAsync(p => 
        //        p.RelatedEntityId == planId && 
        //        p.PaymentPurpose == "PlanPosting" && 
        //        !p.IsDeleted)).FirstOrDefault();
        //    var now = DateTime.UtcNow;
        //    var isNewPayment = false;

        //    if (payment == null)
        //    {
        //        // Lấy UserId từ plan
        //        var plan = await _unitOfWork.ProcurementPlanRepository.GetByIdAsync(planId);
        //        var userId = plan?.CreatedBy;

        //        // Cắt txnRef xuống 20 ký tự để fit vào PaymentCode
        //        var paymentCode = txnRef.Length > 20 ? txnRef[..20] : txnRef;

        //        // Create new payment
        //        payment = new DakLakCoffeeSupplyChain.Repositories.Models.Payment
        //        {
        //            PaymentId = Guid.NewGuid(),
        //            Email = string.Empty,
        //            ConfigId = paymentConfig.ConfigId,
        //            UserId = userId, // Lưu UserId từ plan
        //            PaymentCode = paymentCode, // Sử dụng paymentCode đã cắt
        //            PaymentAmount = (int)paymentConfig.Amount,
        //            PaymentMethod = "VNPay",
        //            PaymentPurpose = "PlanPosting",
        //            PaymentStatus = "Success", // Sử dụng "Success" để match với logic check
        //            PaymentTime = now,
        //            CreatedAt = now,
        //            UpdatedAt = now,
        //            RelatedEntityId = planId,
        //            IsDeleted = false
        //        };
        //        await _unitOfWork.PaymentRepository.CreateAsync(payment);
        //        isNewPayment = true;
        //    }
        //    else
        //    {
        //        // Update existing payment
        //        payment.PaymentStatus = "Success"; // Sử dụng "Success" để match với logic check
        //        payment.PaymentTime = now;
        //        payment.UpdatedAt = now;
        //        await _unitOfWork.PaymentRepository.UpdateAsync(payment);
        //    }

        //    await _unitOfWork.SaveChangesAsync();

        //    // Tạo Wallet Transactions dựa trên phương thức thanh toán
        //    if (isNewPayment || payment.PaymentStatus == "Success")
        //    {
        //        // Lấy thông tin user từ payment hoặc từ plan
        //        var userId = payment.UserId ?? Guid.Empty;

        //        // Nếu không có userId từ payment, lấy từ plan
        //        if (userId == Guid.Empty)
        //        {
        //            var plan = await _unitOfWork.ProcurementPlanRepository.GetByIdAsync(planId);
        //            userId = plan?.CreatedBy ?? Guid.Empty;
        //        }

        //        if (userId != Guid.Empty)
        //        {
        //            // Lấy tên kế hoạch để hiển thị
        //            var plan = await _unitOfWork.ProcurementPlanRepository.GetByIdAsync(planId);
        //            var planName = plan?.Title ?? $"Plan ID: {planId}";

        //            // Tạo transaction cho cả Admin và User (nếu cần)
        //            await CreatePlanPostingFeeTransactionsByMethodAsync(
        //                payment.PaymentId,
        //                payment.PaymentAmount,
        //                userId,
        //                planId,
        //                planName,
        //                payment.PaymentMethod // Truyền phương thức thanh toán
        //            );
        //        }
        //        else
        //        {
        //            // Lấy tên kế hoạch để hiển thị
        //            var plan = await _unitOfWork.ProcurementPlanRepository.GetByIdAsync(planId);
        //            var planName = plan?.Title ?? $"Plan ID: {planId}";

        //            // Fallback: chỉ cộng vào ví System nếu không tìm thấy user
        //            await AddToSystemWalletAsync(
        //                payment.PaymentId, 
        //                payment.PaymentAmount, 
        //                $"Thu phí đăng ký kế hoạch thu mua - {planName}"
        //            );
        //        }
        //    }
        //}

        /// <summary>
        /// Lấy hoặc tạo ví System (Admin wallet)
        /// </summary>
        /// <returns>System wallet</returns>
        public async Task<Wallet> GetOrCreateSystemWalletAsync()
        {
            // Tìm ví System (UserID = null, WalletType = "System")
            var systemWallet = (await _unitOfWork.WalletRepository.GetAllAsync(
                predicate: w => w.UserId == null && 
                               w.WalletType == "System" && 
                               !w.IsDeleted,
                asNoTracking: false
            )).FirstOrDefault();

            if (systemWallet == null)
            {
                // Tạo ví System mới
                systemWallet = new Wallet
                {
                    WalletId = Guid.NewGuid(),
                    UserId = null, // System wallet không có UserID
                    WalletType = "System",
                    TotalBalance = 0,
                    LastUpdated = DateTime.UtcNow,
                    IsDeleted = false
                };

                await _unitOfWork.WalletRepository.CreateAsync(systemWallet);
                await _unitOfWork.SaveChangesAsync();
            }

            return systemWallet;
        }

        /// <summary>
        /// Cộng tiền vào ví System khi thanh toán phí thành công
        /// </summary>
        /// <param name="paymentId">Payment ID</param>
        /// <param name="amount">Số tiền</param>
        /// <param name="description">Mô tả</param>
        /// <returns>Task</returns>
        public async Task AddToSystemWalletAsync(Guid paymentId, double amount, string description)
        {
            // Lấy ví System
            var systemWallet = await GetOrCreateSystemWalletAsync();

            // Cập nhật số dư ví System
            systemWallet.TotalBalance += amount;
            systemWallet.LastUpdated = DateTime.UtcNow;

            await _unitOfWork.WalletRepository.UpdateAsync(systemWallet);

            // Tạo giao dịch ví cho System (Admin)
            var systemTransaction = new WalletTransaction
            {
                TransactionId = Guid.NewGuid(),
                WalletId = systemWallet.WalletId,
                PaymentId = paymentId,
                Amount = amount,
                TransactionType = "Fee", // Thu phí
                Description = description,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            await _unitOfWork.WalletTransactionRepository.CreateAsync(systemTransaction);
            await _unitOfWork.SaveChangesAsync();
        }

        /// <summary>
        /// Tạo Wallet Transaction cho Admin khi thanh toán phí đăng ký kế hoạch qua VNPay
        /// </summary>
        /// <param name="paymentId">Payment ID</param>
        /// <param name="amount">Số tiền</param>
        /// <param name="userId">User ID của người trả phí</param>
        /// <param name="planId">Plan ID</param>
        /// <param name="description">Mô tả</param>
        /// <returns>Task</returns>
        public async Task CreatePlanPostingFeeTransactionsAsync(Guid paymentId, double amount, Guid userId, Guid planId, string description)
        {
            var now = DateTime.UtcNow;

            // Lấy thông tin người thanh toán
            var user = await _unitOfWork.UserAccountRepository.GetUserAccountByIdAsync(userId);
            var userName = user?.Name ?? "Unknown";
            var userRole = user?.Role?.RoleName ?? "Unknown";

            // Chỉ tạo transaction cho ví System (Admin) - Thu phí từ VNPay
            var systemWallet = await GetOrCreateSystemWalletAsync();
            systemWallet.TotalBalance += amount;
            systemWallet.LastUpdated = now;
            await _unitOfWork.WalletRepository.UpdateAsync(systemWallet);

            var systemTransaction = new WalletTransaction
            {
                TransactionId = Guid.NewGuid(),
                WalletId = systemWallet.WalletId,
                PaymentId = paymentId,
                Amount = amount,
                TransactionType = "Fee", // Thu phí
                Description = $"Thu phí đăng ký kế hoạch thu mua từ VNPay - {userName} ({userRole}) - {description}",
                CreatedAt = now,
                IsDeleted = false
            };

            await _unitOfWork.WalletTransactionRepository.CreateAsync(systemTransaction);
            await _unitOfWork.SaveChangesAsync();
        }

        /// <summary>
        /// Tạo Wallet Transaction cho cả Admin và User khi thanh toán phí bằng ví
        /// </summary>
        /// <param name="paymentId">Payment ID</param>
        /// <param name="amount">Số tiền</param>
        /// <param name="userId">User ID của người trả phí</param>
        /// <param name="planId">Plan ID</param>
        /// <param name="description">Mô tả</param>
        /// <returns>Task</returns>
        public async Task CreatePlanPostingFeeTransactionsFromWalletAsync(Guid paymentId, double amount, Guid userId, Guid planId, string description)
        {
            var now = DateTime.UtcNow;

            // Lấy thông tin người thanh toán
            var user = await _unitOfWork.UserAccountRepository.GetUserAccountByIdAsync(userId);
            var userName = user?.Name ?? "Unknown";
            var userRole = user?.Role?.RoleName ?? "Unknown";

            // 1. Tạo transaction cho ví System (Admin) - Thu phí
            var systemWallet = await GetOrCreateSystemWalletAsync();
            systemWallet.TotalBalance += amount;
            systemWallet.LastUpdated = now;
            await _unitOfWork.WalletRepository.UpdateAsync(systemWallet);

            var systemTransaction = new WalletTransaction
            {
                TransactionId = Guid.NewGuid(),
                WalletId = systemWallet.WalletId,
                PaymentId = paymentId,
                Amount = amount,
                TransactionType = "Fee", // Thu phí
                Description = $"Thu phí đăng ký kế hoạch thu mua từ ví - {userName} ({userRole}) - {description}",
                CreatedAt = now,
                IsDeleted = false
            };

            await _unitOfWork.WalletTransactionRepository.CreateAsync(systemTransaction);

            // 2. Tạo transaction cho ví User - Chi phí
            var userWallet = await GetOrCreateUserWalletAsync(userId);
            userWallet.TotalBalance -= amount; // Trừ tiền từ ví user
            userWallet.LastUpdated = now;
            await _unitOfWork.WalletRepository.UpdateAsync(userWallet);

            var userTransaction = new WalletTransaction
            {
                TransactionId = Guid.NewGuid(),
                WalletId = userWallet.WalletId,
                PaymentId = paymentId,
                Amount = -amount, // Số âm vì là chi phí
                TransactionType = "Payment", // Thanh toán phí
                Description = $"Thanh toán phí đăng ký kế hoạch thu mua từ ví - {userName} ({userRole}) - {description}",
                CreatedAt = now,
                IsDeleted = false
            };

            await _unitOfWork.WalletTransactionRepository.CreateAsync(userTransaction);
            await _unitOfWork.SaveChangesAsync();
        }

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
        public async Task CreatePlanPostingFeeTransactionsByMethodAsync(Guid paymentId, double amount, Guid userId, Guid planId, string description, string paymentMethod)
        {
            if (paymentMethod?.ToLower() == "vnpay")
            {
                // Thanh toán VNPay: Chỉ tạo transaction cho Admin
                await CreatePlanPostingFeeTransactionsAsync(paymentId, amount, userId, planId, description);
            }
            else
            {
                // Thanh toán bằng ví: Tạo transaction cho cả Admin và User
                await CreatePlanPostingFeeTransactionsFromWalletAsync(paymentId, amount, userId, planId, description);
            }
        }

        /// <summary>
        /// Lấy hoặc tạo ví của User
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>User wallet</returns>
        public async Task<Wallet> GetOrCreateUserWalletAsync(Guid userId)
        {
            // Tìm ví của user
            var userWallet = (await _unitOfWork.WalletRepository.GetAllAsync(
                predicate: w => w.UserId == userId && !w.IsDeleted,
                asNoTracking: false
            )).FirstOrDefault();

            if (userWallet == null)
            {
                // Tạo ví mới cho user
                userWallet = new Wallet
                {
                    WalletId = Guid.NewGuid(),
                    UserId = userId,
                    WalletType = "Personal",
                    TotalBalance = 0,
                    LastUpdated = DateTime.UtcNow,
                    IsDeleted = false
                };

                await _unitOfWork.WalletRepository.CreateAsync(userWallet);
                await _unitOfWork.SaveChangesAsync();
            }

            return userWallet;
        }

        /// <summary>
        /// Xử lý thanh toán qua ví nội bộ
        /// </summary>
        public async Task<(bool Success, string Message, string? TransactionId)> ProcessWalletPaymentAsync(Guid planId, double amount, Guid userId, string? description = null)
        {
            try
            {
                // Kiểm tra kế hoạch có tồn tại không
                var plan = await _unitOfWork.ProcurementPlanRepository.GetByIdAsync(planId);
                if (plan == null)
                {
                    return (false, "Kế hoạch thu mua không tồn tại", null);
                }

                // Lấy ví của user
                var userWallet = await GetOrCreateUserWalletAsync(userId);
                if (userWallet.TotalBalance < amount)
                {
                    return (false, "Số dư ví không đủ để thanh toán", null);
                }

                // Lấy PaymentConfiguration cho PlanPosting
                var userRoleId = GetCurrentUserRoleId();
                if (userRoleId == null)
                {
                    return (false, "Không thể xác định vai trò của người dùng", null);
                }

                var paymentConfig = await GetPaymentConfigurationByContext(userRoleId.Value, "PlanPosting");
                if (paymentConfig == null)
                {
                    return (false, "Không tìm thấy cấu hình thanh toán", null);
                }

                // Tạo Payment record
                var payment = CreatePaymentRecordWithTxnRef(planId, paymentConfig, "", userId.ToString(), Guid.NewGuid().ToString());
                payment.PaymentMethod = "Wallet";
                payment.PaymentStatus = "Success";
                payment.PaymentTime = DateTime.UtcNow;

                await _unitOfWork.PaymentRepository.CreateAsync(payment);
                await _unitOfWork.SaveChangesAsync();

                // Tạo Wallet Transactions
                await CreatePlanPostingFeeTransactionsByMethodAsync(
                    payment.PaymentId, 
                    amount, 
                    userId, 
                    planId, 
                    description ?? $"Thanh toán phí đăng ký kế hoạch: {plan.Title}",
                    "Wallet"
                );

                return (true, "Thanh toán thành công", payment.PaymentId.ToString());
            }
            catch (Exception ex)
            {
                // Log error
                return (false, $"Lỗi xử lý thanh toán: {ex.Message}", null);
            }
        }
        public async Task<IEnumerable<Payment>> GetPaymentHistoryAsync(Guid userId)
        {
            var payments = await _unitOfWork.PaymentRepository.GetAllAsync(
                predicate: p => p.UserId==userId&&!p.IsDeleted,
                orderBy: query => query.OrderByDescending(p => p.CreatedAt),
                asNoTracking: true
            );
            return payments;
        }

        //public async Task<IEnumerable<Payment>> GetPlanPaymentHistoryAsync(Guid planId)
        //{
        //    var payments = await _unitOfWork.PaymentRepository.GetAllAsync(
        //        predicate: p => p.RelatedEntityId==planId&&!p.IsDeleted,
        //        orderBy: query => query.OrderByDescending(p => p.CreatedAt),
        //        asNoTracking: true
        //    );
        //    return payments;
        //}
    }
}
