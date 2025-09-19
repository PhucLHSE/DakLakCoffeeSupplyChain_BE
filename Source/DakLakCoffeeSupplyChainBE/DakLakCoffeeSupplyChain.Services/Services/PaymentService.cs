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
            var paymentCode = PaymentHelper.GeneratePaymentCode();

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
                PaymentStatus = "Success", // Tự động thành Success
                PaymentTime = now,
                AdminVerified = true, // Tự động xác thực
                CreatedAt = now,
                UpdatedAt = now,
                RelatedEntityId = planId,
                IsDeleted = false
            };
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
        /// Xử lý MockIpn - tạo hoặc cập nhật payment record
        /// </summary>
        /// <param name="planId">Plan ID</param>
        /// <param name="txnRef">Transaction reference</param>
        /// <param name="paymentConfig">Payment configuration</param>
        /// <returns>Task</returns>
        public async Task ProcessMockIpnAsync(Guid planId, string txnRef, PaymentConfiguration paymentConfig)
        {
            var payment = (await _unitOfWork.PaymentRepository.GetAllAsync(p => p.PaymentCode == txnRef)).FirstOrDefault();
            var now = DateTime.UtcNow;

            if (payment == null)
            {
                // Create new payment
                payment = new DakLakCoffeeSupplyChain.Repositories.Models.Payment
                {
                    PaymentId = Guid.NewGuid(),
                    Email = string.Empty,
                    ConfigId = paymentConfig.ConfigId,
                    UserId = null,
                    PaymentCode = txnRef,
                    PaymentAmount = (int)paymentConfig.Amount,
                    PaymentMethod = "VNPay",
                    PaymentPurpose = "PlanPosting",
                    PaymentStatus = "Paid",
                    PaymentTime = now,
                    CreatedAt = now,
                    UpdatedAt = now,
                    RelatedEntityId = planId,
                    IsDeleted = false
                };
                await _unitOfWork.PaymentRepository.CreateAsync(payment);
            }
            else
            {
                // Update existing payment
                payment.PaymentStatus = "Paid";
                payment.PaymentTime = now;
                payment.UpdatedAt = now;
                await _unitOfWork.PaymentRepository.UpdateAsync(payment);
            }

            await _unitOfWork.SaveChangesAsync();
        }
    }
}
