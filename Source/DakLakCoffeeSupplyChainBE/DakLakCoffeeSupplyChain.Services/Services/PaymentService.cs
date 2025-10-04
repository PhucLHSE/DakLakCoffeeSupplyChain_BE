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

        public int? GetCurrentUserRoleId()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext?.User == null) return null;

            var roleClaim = httpContext.User.FindFirst(ClaimTypes.Role)?.Value;
            if (string.IsNullOrEmpty(roleClaim))
                return null;

            return roleClaim.ToLower() switch
            {
                "businessmanager" => 2,
                "farmer" => 4,
                "admin" => 1,
                _ => null
            };
        }

        public (string email, string userId) GetCurrentUserInfo()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext?.User == null) return (string.Empty, string.Empty);

            var email = httpContext.User.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty;
            var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;

            return (email, userId);
        }

        public string GetClientIpAddress()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            return httpContext?.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
        }

        public async Task<PaymentConfiguration?> GetPaymentConfigurationByContext(
            int roleId, string feeType, Guid planId)
        {
            var currentDate = DateHelper.ParseDateOnlyFormatVietNamCurrentTime();

            var configs = await _unitOfWork.PaymentConfigurationRepository.GetAllAsync(
                predicate: pc => pc.RoleId == roleId &&
                                pc.FeeType == feeType &&
                                pc.IsActive == true &&
                                pc.IsDeleted == false &&
                                pc.EffectiveFrom <= currentDate &&
                                (pc.EffectiveTo == null || pc.EffectiveTo >= currentDate),
                orderBy: query => query.OrderBy(pc => pc.MinTons),
                asNoTracking: true
            );

            if (!configs.Any())
                return null;

            var plan = await _unitOfWork.ProcurementPlanRepository.GetByIdWithDetailsAsync(planId);
            if (plan == null) return null;

            double totalKg = 0;
            if (plan.ProcurementPlansDetails != null && plan.ProcurementPlansDetails.Any())
            {
                totalKg = plan.ProcurementPlansDetails
                    .Where(d => d.TargetQuantity.HasValue)
                    .Sum(d => d.TargetQuantity.Value);
            }
            else
            {
                totalKg = plan.TotalQuantity ?? 0;
            }

            var totalTons = totalKg / 1000.0;

            var matchedConfig = configs.FirstOrDefault(pc =>
                (!pc.MinTons.HasValue || totalTons >= pc.MinTons.Value) &&
                (!pc.MaxTons.HasValue || totalTons <= pc.MaxTons.Value)
            );

            return matchedConfig;
        }

        public Payment CreatePaymentRecord(Guid planId, PaymentConfiguration paymentConfig, string userEmail, string userId)
        {
            var now = DateHelper.NowVietnamTime();
            var txnRef = PaymentHelper.GenerateTxnRef(planId);

            return new Payment
            {
                PaymentId = Guid.NewGuid(),
                Email = userEmail,
                ConfigId = paymentConfig.ConfigId,
                UserId = !string.IsNullOrEmpty(userId) ? Guid.Parse(userId) : null,
                PaymentCode = txnRef.Length > 20 ? txnRef[..20] : txnRef,
                PaymentAmount = (int)paymentConfig.Amount,
                PaymentMethod = "VNPay",
                PaymentPurpose = "PlanPosting",
                PaymentStatus = "Success",
                PaymentTime = now,
                AdminVerified = false,
                CreatedAt = now,
                UpdatedAt = now,
                RelatedEntityId = planId,
                IsDeleted = false
            };
        }

        public async Task<Payment> CreateOrUpdatePaymentRecordWithTxnRef(
            Guid planId,
            PaymentConfiguration paymentConfig,
            string userEmail,
            string userId,
            string txnRef)
        {
            var now = DateHelper.NowVietnamTime();
            var paymentCode = txnRef.Length > 20 ? txnRef[..20] : txnRef;

            var existingPayment = (await _unitOfWork.PaymentRepository.GetAllAsync(
                p => p.RelatedEntityId == planId
                  && p.PaymentPurpose == "PlanPosting"
                  && !p.IsDeleted
            )).FirstOrDefault();

            if (existingPayment != null)
            {
                if (existingPayment.PaymentStatus == "Success")
                    throw new InvalidOperationException("Kế hoạch đã thanh toán, không thể chỉnh sửa phí.");

                existingPayment.ConfigId = paymentConfig.ConfigId;
                existingPayment.PaymentAmount = (int)paymentConfig.Amount;
                existingPayment.PaymentCode = paymentCode;
                existingPayment.PaymentStatus = "Pending";
                existingPayment.PaymentTime = null;
                existingPayment.UpdatedAt = now;
                existingPayment.Email = userEmail;
                existingPayment.UserId = !string.IsNullOrEmpty(userId) ? Guid.Parse(userId) : null;

                await _unitOfWork.PaymentRepository.UpdateAsync(existingPayment);
                await _unitOfWork.SaveChangesAsync();
                return existingPayment;
            }
            else
            {
                var payment = new Payment
                {
                    PaymentId = Guid.NewGuid(),
                    Email = userEmail,
                    ConfigId = paymentConfig.ConfigId,
                    UserId = !string.IsNullOrEmpty(userId) ? Guid.Parse(userId) : null,
                    PaymentCode = paymentCode,
                    PaymentAmount = (int)paymentConfig.Amount,
                    PaymentMethod = "VNPay",
                    PaymentPurpose = "PlanPosting",
                    PaymentStatus = "Pending",
                    PaymentTime = null,
                    AdminVerified = false,
                    CreatedAt = now,
                    UpdatedAt = now,
                    RelatedEntityId = planId,
                    IsDeleted = false
                };

                await _unitOfWork.PaymentRepository.CreateAsync(payment);
                await _unitOfWork.SaveChangesAsync();
                return payment;
            }
        }

        public async Task<(string RspCode, string Message)> ProcessRealIpnAsync(Dictionary<string, string> vnpParams)
        {
            var vnp_TxnRef = vnpParams.GetValueOrDefault("vnp_TxnRef") ?? "";
            var vnp_ResponseCode = vnpParams.GetValueOrDefault("vnp_ResponseCode");
            var vnp_AmountStr = vnpParams.GetValueOrDefault("vnp_Amount");
            var vnp_OrderInfo = vnpParams.GetValueOrDefault("vnp_OrderInfo") ?? "";

            if (string.IsNullOrEmpty(vnp_TxnRef) || string.IsNullOrEmpty(vnp_AmountStr))
                return ("99", "Missing fields");

            if (!long.TryParse(vnp_AmountStr, out var vnpAmountX100))
                return ("04", "Invalid amount");

            var paymentCode = vnp_TxnRef.Length > 20 ? vnp_TxnRef[..20] : vnp_TxnRef;

            var payment = (await _unitOfWork.PaymentRepository
                .GetAllAsync(p => p.PaymentCode == paymentCode && !p.IsDeleted))
                .FirstOrDefault();

            if (payment == null)
                return ("01", "Order not found");

            if (!string.Equals(payment.PaymentStatus, "Pending", StringComparison.OrdinalIgnoreCase))
                return ("02", "Order already confirmed");

            var requiredAmountX100 = (long)payment.PaymentAmount * 100;
            if (vnpAmountX100 != requiredAmountX100)
                return ("04", "Invalid amount");

            var now = DateHelper.NowVietnamTime();
            var isSuccess = vnp_ResponseCode == "00";
            payment.PaymentStatus = isSuccess ? "Success" : "Failed";
            payment.PaymentTime = isSuccess ? now : null;
            payment.UpdatedAt = now;

            await _unitOfWork.PaymentRepository.UpdateAsync(payment);
            await _unitOfWork.SaveChangesAsync();

            if (!isSuccess)
                return ("00", "Confirm Success");

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

        public async Task SavePaymentAsync(Payment payment)
        {
            await _unitOfWork.PaymentRepository.CreateAsync(payment);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<ProcurementPlan?> ValidatePlanOwnership(Guid planId, string userId)
        {
            var plan = await _unitOfWork.ProcurementPlanRepository.GetByIdAsync(
                predicate: p => p.PlanId == planId && !p.IsDeleted
            );

            if (plan == null) return null;

            var user = await _unitOfWork.UserAccountRepository.GetByIdAsync(plan.CreatedBy);
            if (user?.UserId.ToString() != userId)
                return null;

            return plan;
        }

        public async Task CreateWalletTopupPaymentAsync(Guid walletId, int amount, string txnRef, string userEmail, string userId)
        {
            var cfg = (await _unitOfWork.PaymentConfigurationRepository.GetAllAsync()).FirstOrDefault();
            if (cfg == null) return;

            var now = DateHelper.NowVietnamTime();
            var payment = new Payment
            {
                PaymentId = Guid.NewGuid(),
                Email = userEmail,
                ConfigId = cfg.ConfigId,
                UserId = !string.IsNullOrEmpty(userId) ? Guid.Parse(userId) : null,
                PaymentCode = txnRef[..20],
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

        public async Task<(bool Success, string NewTxnRef, string Message)> RecreateWalletTopupPaymentAsync(Guid paymentId, string userEmail, string userId)
        {
            var payment = await _unitOfWork.PaymentRepository.GetByIdAsync(paymentId);
            if (payment == null)
                return (false, "", "Không tìm thấy payment.");

            if (payment.PaymentStatus != "Pending")
                return (false, "", $"Payment đã {payment.PaymentStatus}, không thể tiếp tục.");

            if (payment.PaymentPurpose != "WalletTopup")
                return (false, "", "Payment không phải WalletTopup.");

            var newTxnRef = PaymentHelper.GenerateWalletTxnRef();

            payment.PaymentCode = newTxnRef[..20];
            payment.UpdatedAt = DateHelper.NowVietnamTime();
            payment.Email = userEmail;
            if (!string.IsNullOrEmpty(userId) && Guid.TryParse(userId, out var uid))
                payment.UserId = uid;

            await _unitOfWork.PaymentRepository.UpdateAsync(payment);
            await _unitOfWork.SaveChangesAsync();

            return (true, newTxnRef, "Payment đã được cập nhật để tiếp tục thanh toán.");
        }

        public async Task<Wallet> GetOrCreateSystemWalletAsync()
        {
            var systemWallet = (await _unitOfWork.WalletRepository.GetAllAsync(
                predicate: w => w.UserId == null &&
                               w.WalletType == "System" &&
                               !w.IsDeleted,
                asNoTracking: false
            )).FirstOrDefault();

            if (systemWallet == null)
            {
                systemWallet = new Wallet
                {
                    WalletId = Guid.NewGuid(),
                    UserId = null,
                    WalletType = "System",
                    TotalBalance = 0,
                    LastUpdated = DateHelper.NowVietnamTime(),
                    IsDeleted = false
                };

                await _unitOfWork.WalletRepository.CreateAsync(systemWallet);
                await _unitOfWork.SaveChangesAsync();
            }

            return systemWallet;
        }

        public async Task AddToSystemWalletAsync(Guid paymentId, double amount, string description)
        {
            var systemWallet = await GetOrCreateSystemWalletAsync();
            systemWallet.TotalBalance += amount;
            systemWallet.LastUpdated = DateHelper.NowVietnamTime();

            await _unitOfWork.WalletRepository.UpdateAsync(systemWallet);

            var systemTransaction = new WalletTransaction
            {
                TransactionId = Guid.NewGuid(),
                WalletId = systemWallet.WalletId,
                PaymentId = paymentId,
                Amount = amount,
                TransactionType = "Fee",
                Description = description,
                CreatedAt = DateHelper.NowVietnamTime(),
                IsDeleted = false
            };

            await _unitOfWork.WalletTransactionRepository.CreateAsync(systemTransaction);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task CreatePlanPostingFeeTransactionsAsync(Guid paymentId, double amount, Guid userId, Guid planId, string description)
        {
            var now = DateHelper.NowVietnamTime();

            var user = await _unitOfWork.UserAccountRepository.GetUserAccountByIdAsync(userId);
            var userName = user?.Name ?? "Unknown";
            var userRole = user?.Role?.RoleName ?? "Unknown";

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
                TransactionType = "Fee",
                Description = $"Thu phí đăng ký kế hoạch thu mua từ VNPay - {userName} ({userRole}) - {description}",
                CreatedAt = now,
                IsDeleted = false
            };

            await _unitOfWork.WalletTransactionRepository.CreateAsync(systemTransaction);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task CreatePlanPostingFeeTransactionsFromWalletAsync(Guid paymentId, double amount, Guid userId, Guid planId, string description)
        {
            var now = DateHelper.NowVietnamTime();

            var user = await _unitOfWork.UserAccountRepository.GetUserAccountByIdAsync(userId);
            var userName = user?.Name ?? "Unknown";
            var userRole = user?.Role?.RoleName ?? "Unknown";

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
                TransactionType = "Fee",
                Description = $"Thu phí đăng ký kế hoạch thu mua từ ví - {userName} ({userRole}) - {description}",
                CreatedAt = now,
                IsDeleted = false
            };

            await _unitOfWork.WalletTransactionRepository.CreateAsync(systemTransaction);

            var userWallet = await GetOrCreateUserWalletAsync(userId);
            userWallet.TotalBalance -= amount;
            userWallet.LastUpdated = now;
            await _unitOfWork.WalletRepository.UpdateAsync(userWallet);

            var userTransaction = new WalletTransaction
            {
                TransactionId = Guid.NewGuid(),
                WalletId = userWallet.WalletId,
                PaymentId = paymentId,
                Amount = -amount,
                TransactionType = "Payment",
                Description = $"Thanh toán phí đăng ký kế hoạch thu mua từ ví - {userName} ({userRole}) - {description}",
                CreatedAt = now,
                IsDeleted = false
            };

            await _unitOfWork.WalletTransactionRepository.CreateAsync(userTransaction);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task CreatePlanPostingFeeTransactionsByMethodAsync(Guid paymentId, double amount, Guid userId, Guid planId, string description, string paymentMethod)
        {
            if (paymentMethod?.ToLower() == "vnpay")
                await CreatePlanPostingFeeTransactionsAsync(paymentId, amount, userId, planId, description);
            else
                await CreatePlanPostingFeeTransactionsFromWalletAsync(paymentId, amount, userId, planId, description);
        }

        public async Task<Wallet> GetOrCreateUserWalletAsync(Guid userId)
        {
            var userWallet = (await _unitOfWork.WalletRepository.GetAllAsync(
                predicate: w => w.UserId == userId && !w.IsDeleted,
                asNoTracking: false
            )).FirstOrDefault();

            if (userWallet == null)
            {
                userWallet = new Wallet
                {
                    WalletId = Guid.NewGuid(),
                    UserId = userId,
                    WalletType = "Personal",
                    TotalBalance = 0,
                    LastUpdated = DateHelper.NowVietnamTime(),
                    IsDeleted = false
                };

                await _unitOfWork.WalletRepository.CreateAsync(userWallet);
                await _unitOfWork.SaveChangesAsync();
            }

            return userWallet;
        }

        public async Task<(bool Success, string Message, string? TransactionId)> ProcessWalletPaymentAsync(Guid planId, double amount, Guid userId, string? description = null)
        {
            try
            {
                var plan = await _unitOfWork.ProcurementPlanRepository.GetByIdAsync(planId);
                if (plan == null)
                    return (false, "Kế hoạch thu mua không tồn tại", null);

                var userWallet = await GetOrCreateUserWalletAsync(userId);
                if (userWallet.TotalBalance < amount)
                    return (false, "Số dư ví không đủ để thanh toán", null);

                var userRoleId = GetCurrentUserRoleId();
                if (userRoleId == null)
                    return (false, "Không thể xác định vai trò của người dùng", null);

                var paymentConfig = await GetPaymentConfigurationByContext(userRoleId.Value, "PlanPosting", planId);
                if (paymentConfig == null)
                    return (false, "Không tìm thấy cấu hình thanh toán", null);

                var payment = await CreateOrUpdatePaymentRecordWithTxnRef(planId, paymentConfig, "", userId.ToString(), Guid.NewGuid().ToString());

                payment.PaymentMethod = "Wallet";
                payment.PaymentStatus = "Success";
                payment.PaymentTime = DateHelper.NowVietnamTime();
                payment.UpdatedAt = DateHelper.NowVietnamTime();

                await _unitOfWork.PaymentRepository.UpdateAsync(payment);
                await _unitOfWork.SaveChangesAsync();

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
                return (false, $"Lỗi xử lý thanh toán: {ex.Message}", null);
            }
        }

        public async Task<IEnumerable<Payment>> GetPaymentHistoryAsync(Guid userId)
        {
            var payments = await _unitOfWork.PaymentRepository.GetAllAsync(
                predicate: p => p.UserId == userId && !p.IsDeleted,
                orderBy: query => query.OrderByDescending(p => p.CreatedAt),
                asNoTracking: true
            );
            return payments;
        }
    }
}
