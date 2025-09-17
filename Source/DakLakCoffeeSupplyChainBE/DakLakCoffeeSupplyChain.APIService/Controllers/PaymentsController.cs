using DakLakCoffeeSupplyChain.Common.Helpers;
using DakLakCoffeeSupplyChain.Repositories.UnitOfWork;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace DakLakCoffeeSupplyChain.APIService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentsController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<PaymentsController> _logger;
        private readonly IHostEnvironment _env;

        public PaymentsController(IConfiguration config, IUnitOfWork unitOfWork, ILogger<PaymentsController> logger, IHostEnvironment env)
        {
            _config = config;
            _unitOfWork = unitOfWork;
            _logger = logger;
            _env = env;
        }

        public class VnPayCreateRequest
        {
            public Guid PlanId { get; set; }
            public int Amount { get; set; } = 100000; // VND
            public string? ReturnUrl { get; set; }
            public string? Locale { get; set; } = "vn";
        }

        public class VnPayCreateResponse
        {
            public string Url { get; set; } = string.Empty;
        }

        [HttpPost("vnpay/create-url")]
        [Authorize(Roles = "BusinessManager")]
        public async Task<IActionResult> CreateVnPayUrl([FromBody] VnPayCreateRequest req)
        {
            var tmnCode = _config["VnPay:TmnCode"] ?? string.Empty;
            var secret = _config["VnPay:HashSecret"] ?? string.Empty;
            var baseUrl = _config["VnPay:BaseUrl"] ?? _config["VnPay:PaymentUrl"] ?? "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";
            var returnUrl = req.ReturnUrl ?? _config["VnPay:ReturnUrl"] ?? string.Empty;

            if (string.IsNullOrWhiteSpace(tmnCode) || string.IsNullOrWhiteSpace(secret) || string.IsNullOrWhiteSpace(returnUrl))
                return BadRequest("VNPay chưa cấu hình đầy đủ.");

            // VNPay yêu cầu amount * 100
            var amount = (long)req.Amount * 100;
            var txnRef = req.PlanId.ToString("N"); // 32 chars

            string ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

            var vnp = new SortedDictionary<string, string>
            {
                ["vnp_Version"] = "2.1.0",
                ["vnp_Command"] = "pay",
                ["vnp_TmnCode"] = tmnCode,
                ["vnp_Amount"] = amount.ToString(),
                ["vnp_CreateDate"] = DateTime.UtcNow.ToString("yyyyMMddHHmmss"),
                ["vnp_CurrCode"] = "VND",
                ["vnp_IpAddr"] = ip,
                ["vnp_Locale"] = req.Locale ?? "vn",
                ["vnp_OrderInfo"] = $"PlanPosting:{txnRef}",
                ["vnp_OrderType"] = "other",
                ["vnp_ReturnUrl"] = returnUrl,
                ["vnp_TxnRef"] = txnRef
            };

            // VNPay signature: build hash string with key=value (value URL-encoded as trong ví dụ VNPay)
            var encodedForHash = string.Join('&', vnp.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
            var secureHash = CreateHmac512(secret, encodedForHash);
            var query = string.Join('&', vnp.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
            var url = $"{baseUrl}?{query}&vnp_SecureHashType=HmacSHA512&vnp_SecureHash={secureHash}";

            // Lấy email của manager từ JWT token
            var userEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty;
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;

            // Lưu payment thành Success luôn (chỉ khi có PaymentConfiguration để tránh lỗi FK)
            var cfg = (await _unitOfWork.PaymentConfigurationRepository.GetAllAsync()).FirstOrDefault();
            if (cfg != null)
            {
                var now = DateTime.UtcNow;
                var paymentCode = $"PAY-{now:yyyyMMddHHmmss}"; // <= 18 chars, phù hợp cột PaymentCode
                var payment = new DakLakCoffeeSupplyChain.Repositories.Models.Payment
                {
                    PaymentId = Guid.NewGuid(),
                    Email = userEmail, // Lưu email của manager
                    ConfigId = cfg.ConfigId,
                    UserId = !string.IsNullOrEmpty(userId) ? Guid.Parse(userId) : null,
                    PaymentCode = paymentCode,
                    PaymentAmount = req.Amount,
                    PaymentMethod = "VNPay",
                    PaymentPurpose = "PlanPosting",
                    PaymentStatus = "Success", // Tự động thành Success
                    PaymentTime = now, // Tự động set thời gian thanh toán
                    AdminVerified = true, // Tự động xác thực
                    CreatedAt = now,
                    UpdatedAt = now,
                    RelatedEntityId = req.PlanId,
                    IsDeleted = false
                };
                await _unitOfWork.PaymentRepository.CreateAsync(payment);
                await _unitOfWork.SaveChangesAsync();
                
                // Tự động mở kế hoạch luôn
                var plan = (await _unitOfWork.ProcurementPlanRepository.GetAllAsync(p => p.PlanId == req.PlanId)).FirstOrDefault();
                if (plan != null)
                {
                    plan.Status = "Open";
                    plan.StartDate ??= DateOnly.FromDateTime(DateTime.UtcNow.Date);
                    plan.UpdatedAt = DateTime.UtcNow;
                    await _unitOfWork.ProcurementPlanRepository.UpdateAsync(plan);
                    await _unitOfWork.SaveChangesAsync();
                }
            }

            return Ok(new VnPayCreateResponse { Url = url });
        }



        private static string CreateHmac512(string key, string data)
        {
            using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(key));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            return Convert.ToHexString(hash).ToUpperInvariant();
        }

        // DEV-ONLY: mock IPN to test locally without public URL
        public class MockIpnRequest
        {
            public Guid PlanId { get; set; }
            public int Amount { get; set; } = 100000; // VND
            public string? TxnRef { get; set; } // if null, derive from PlanId (N)
        }

        [HttpPost("vnpay/mock-ipn")]
        [AllowAnonymous]
        public async Task<IActionResult> MockIpn([FromBody] MockIpnRequest req)
        {
            // Only allow in Development or when explicitly enabled
            var allow = _env.IsDevelopment() || string.Equals(_config["VnPay:AllowMockIpn"], "true", StringComparison.OrdinalIgnoreCase);
            if (!allow) return Forbid();

            var txnRef = string.IsNullOrWhiteSpace(req.TxnRef) ? req.PlanId.ToString("N") : req.TxnRef!;

            // Upsert payment as Paid
            var payment = (await _unitOfWork.PaymentRepository.GetAllAsync(p => p.PaymentCode == txnRef)).FirstOrDefault();
            var cfg = (await _unitOfWork.PaymentConfigurationRepository.GetAllAsync()).FirstOrDefault();
            var now = DateTime.UtcNow;
            if (payment == null && cfg != null)
            {
                payment = new DakLakCoffeeSupplyChain.Repositories.Models.Payment
                {
                    PaymentId = Guid.NewGuid(),
                    Email = string.Empty,
                    ConfigId = cfg.ConfigId,
                    UserId = null,
                    PaymentCode = txnRef,
                    PaymentAmount = req.Amount,
                    PaymentMethod = "VNPay",
                    PaymentPurpose = "PlanPosting",
                    PaymentStatus = "Paid",
                    PaymentTime = now,
                    CreatedAt = now,
                    UpdatedAt = now,
                    RelatedEntityId = req.PlanId,
                    IsDeleted = false
                };
                await _unitOfWork.PaymentRepository.CreateAsync(payment);
            }
            else if (payment != null)
            {
                payment.PaymentStatus = "Paid";
                payment.PaymentTime = now;
                payment.UpdatedAt = now;
                await _unitOfWork.PaymentRepository.UpdateAsync(payment);
            }

            // Open plan
            var plan = (await _unitOfWork.ProcurementPlanRepository.GetAllAsync(p => p.PlanId == req.PlanId)).FirstOrDefault();
            if (plan != null)
            {
                plan.Status = "Open";
                plan.StartDate ??= DateOnly.FromDateTime(DateTime.UtcNow.Date);
                plan.UpdatedAt = now;
                await _unitOfWork.ProcurementPlanRepository.UpdateAsync(plan);
            }

            await _unitOfWork.SaveChangesAsync();
            return Ok(new { message = "Mock IPN applied", txnRef });
        }

        public class WalletTopupVnPayRequest
        {
            public Guid WalletId { get; set; }
            public int Amount { get; set; } = 100000; // VND
            public string? ReturnUrl { get; set; }
            public string? Locale { get; set; } = "vn";
            public string? Description { get; set; }
        }

        [HttpPost("wallet-topup/vnpay/create-url")]
        [Authorize(Roles = "BusinessManager,BusinessStaff,Farmer")]
        public async Task<IActionResult> CreateWalletTopupVnPayUrl([FromBody] WalletTopupVnPayRequest req)
        {
            var tmnCode = _config["VnPay:TmnCode"] ?? string.Empty;
            var secret = _config["VnPay:HashSecret"] ?? string.Empty;
            var baseUrl = _config["VnPay:BaseUrl"] ?? _config["VnPay:PaymentUrl"] ?? "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";
            var returnUrl = req.ReturnUrl ?? _config["VnPay:ReturnUrl"] ?? string.Empty;

            if (string.IsNullOrWhiteSpace(tmnCode) || string.IsNullOrWhiteSpace(secret) || string.IsNullOrWhiteSpace(returnUrl))
                return BadRequest("VNPay chưa cấu hình đầy đủ.");

            // VNPay yêu cầu amount * 100
            var amount = (long)req.Amount * 100;
            var txnRef = Guid.NewGuid().ToString("N"); // 32 chars như endpoint cũ

            string ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

            var vnp = new SortedDictionary<string, string>
            {
                ["vnp_Version"] = "2.1.0",
                ["vnp_Command"] = "pay",
                ["vnp_TmnCode"] = tmnCode,
                ["vnp_Amount"] = amount.ToString(),
                ["vnp_CreateDate"] = DateTime.UtcNow.ToString("yyyyMMddHHmmss"),
                ["vnp_CurrCode"] = "VND",
                ["vnp_IpAddr"] = ip,
                ["vnp_Locale"] = req.Locale ?? "vn",
                ["vnp_OrderInfo"] = $"WalletTopup:{txnRef}",
                ["vnp_OrderType"] = "other",
                ["vnp_ReturnUrl"] = returnUrl,
                ["vnp_TxnRef"] = txnRef
            };

            // VNPay signature: build hash string với URL encoding như endpoint cũ
            var encodedForHash = string.Join('&', vnp.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
            var secureHash = CreateHmac512(secret, encodedForHash);
            var query = string.Join('&', vnp.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
            
            // Debug logging
            _logger.LogInformation($"VNPay Encoded For Hash: {encodedForHash}");
            _logger.LogInformation($"VNPay Secret: {secret}");
            _logger.LogInformation($"VNPay Hash: {secureHash}");
            
            // Tạo URL với hash
            var url = $"{baseUrl}?{query}&vnp_SecureHashType=HmacSHA512&vnp_SecureHash={secureHash}";

            // Lấy email của user từ JWT token
            var userEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty;
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;

            // Lưu payment record
            var cfg = (await _unitOfWork.PaymentConfigurationRepository.GetAllAsync()).FirstOrDefault();
            if (cfg != null)
            {
                var now = DateTime.UtcNow;
                var payment = new DakLakCoffeeSupplyChain.Repositories.Models.Payment
                {
                    PaymentId = Guid.NewGuid(),
                    Email = userEmail,
                    ConfigId = cfg.ConfigId,
                    UserId = !string.IsNullOrEmpty(userId) ? Guid.Parse(userId) : null,
                    PaymentCode = txnRef[..20], // Cắt 20 ký tự đầu để fit vào PaymentCode
                    PaymentAmount = req.Amount,
                    PaymentMethod = "VNPay",
                    PaymentPurpose = "WalletTopup",
                    PaymentStatus = "Pending",
                    PaymentTime = null,
                    AdminVerified = false,
                    CreatedAt = now,
                    UpdatedAt = now,
                    RelatedEntityId = req.WalletId,
                    IsDeleted = false
                };
                await _unitOfWork.PaymentRepository.CreateAsync(payment);
                await _unitOfWork.SaveChangesAsync();
            }

            return Ok(new VnPayCreateResponse { Url = url });
        }
    }
}



