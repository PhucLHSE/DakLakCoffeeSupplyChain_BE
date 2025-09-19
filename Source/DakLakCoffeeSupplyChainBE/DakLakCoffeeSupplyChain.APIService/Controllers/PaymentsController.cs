using DakLakCoffeeSupplyChain.Common.DTOs.PaymentDTOs;
using DakLakCoffeeSupplyChain.Common.Helpers;
using DakLakCoffeeSupplyChain.Repositories.UnitOfWork;
using DakLakCoffeeSupplyChain.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

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
        private readonly IPaymentService _paymentService;

        public PaymentsController(
            IConfiguration config, 
            IUnitOfWork unitOfWork, 
            ILogger<PaymentsController> logger, 
            IHostEnvironment env,
            IPaymentService paymentService)
        {
            _config = config;
            _unitOfWork = unitOfWork;
            _logger = logger;
            _env = env;
            _paymentService = paymentService;
        }


        /// <summary>
        /// Lấy thông tin phí thanh toán cho PlanPosting
        /// </summary>
        [HttpGet("plan-posting-fee")]
        [Authorize(Roles = "BusinessManager")]
        public async Task<IActionResult> GetPlanPostingFee()
        {
            var userRoleId = _paymentService.GetCurrentUserRoleId();
            if (userRoleId == null)
            {
                return BadRequest("Không thể xác định vai trò của người dùng.");
            }

            var paymentConfig = await _paymentService.GetPaymentConfigurationByContext(userRoleId.Value, "PlanPosting");
            if (paymentConfig == null)
            {
                return BadRequest("Không tìm thấy cấu hình phí cho việc đăng ký kế hoạch thu mua.");
            }

            return Ok(new PaymentAmountResponse
            {
                Amount = (int)paymentConfig.Amount,
                FeeType = paymentConfig.FeeType,
                Description = paymentConfig.Description ?? "Phí đăng ký kế hoạch thu mua"
            });
        }


        [HttpPost("vnpay/create-url")]
        [Authorize(Roles = "BusinessManager")]
        public async Task<IActionResult> CreateVnPayUrl([FromBody] VnPayCreateRequest req)
        {
            // Validate VNPay configuration
            var tmnCode = _config["VnPay:TmnCode"] ?? string.Empty;
            var secret = _config["VnPay:HashSecret"] ?? string.Empty;
            var baseUrl = _config["VnPay:BaseUrl"] ?? _config["VnPay:PaymentUrl"] ?? "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";
            var returnUrl = req.ReturnUrl ?? _config["VnPay:ReturnUrl"] ?? string.Empty;

            if (string.IsNullOrWhiteSpace(tmnCode) || string.IsNullOrWhiteSpace(secret) || string.IsNullOrWhiteSpace(returnUrl))
                return BadRequest("VNPay chưa cấu hình đầy đủ.");

            // Get user information
            var (userEmail, userId) = _paymentService.GetCurrentUserInfo();
            var userRoleId = _paymentService.GetCurrentUserRoleId();

            if (userRoleId == null)
            {
                return BadRequest("Không thể xác định vai trò của người dùng.");
            }

            // Get payment configuration
            var paymentConfig = await _paymentService.GetPaymentConfigurationByContext(userRoleId.Value, "PlanPosting");
            if (paymentConfig == null)
            {
                return BadRequest("Không tìm thấy cấu hình phí cho việc đăng ký kế hoạch thu mua.");
            }

            // Create VNPay parameters
            var paymentAmount = (int)paymentConfig.Amount;
            var amount = (long)paymentAmount * 100; // VNPay requires amount * 100
            var txnRef = PaymentHelper.GenerateTxnRef(req.PlanId);
            var ipAddress = _paymentService.GetClientIpAddress();

            var vnpParameters = PaymentHelper.CreateVnPayParameters(
                tmnCode, amount, txnRef, $"PlanPosting:{txnRef}", returnUrl, ipAddress, req.Locale ?? "vn");

            // Create VNPay URL
            var url = PaymentHelper.CreateVnPayUrl(baseUrl, vnpParameters, secret);

            // Create and save payment record
            var payment = _paymentService.CreatePaymentRecord(req.PlanId, paymentConfig, userEmail, userId);
            await _paymentService.SavePaymentAsync(payment);

            return Ok(new VnPayCreateResponse { Url = url });
        }




        // DEV-ONLY: mock IPN to test locally without public URL

        [HttpPost("vnpay/mock-ipn")]
        [AllowAnonymous]
        public async Task<IActionResult> MockIpn([FromBody] MockIpnRequest req)
        {
            // Only allow in Development or when explicitly enabled
            var allow = _env.IsDevelopment() || string.Equals(_config["VnPay:AllowMockIpn"], "true", StringComparison.OrdinalIgnoreCase);
            if (!allow) return Forbid();

            var txnRef = string.IsNullOrWhiteSpace(req.TxnRef) ? req.PlanId.ToString("N") : req.TxnRef!;

            // Lấy PaymentConfiguration cho PlanPosting (MockIpn thường dùng cho BusinessManager)
            var paymentConfig = await _paymentService.GetPaymentConfigurationByContext(2, "PlanPosting"); // RoleID = 2 cho BusinessManager
            if (paymentConfig == null)
            {
                return BadRequest("Không tìm thấy cấu hình phí cho việc đăng ký kế hoạch thu mua.");
            }

            // Process MockIpn using service
            await _paymentService.ProcessMockIpnAsync(req.PlanId, txnRef, paymentConfig);
            return Ok(new { message = "Mock IPN applied", txnRef });
        }


        [HttpPost("wallet-topup/vnpay/create-url")]
        [Authorize(Roles = "BusinessManager,BusinessStaff,Farmer")]
        public async Task<IActionResult> CreateWalletTopupVnPayUrl([FromBody] WalletTopupVnPayRequest req)
        {
            // Validate VNPay configuration
            var tmnCode = _config["VnPay:TmnCode"] ?? string.Empty;
            var secret = _config["VnPay:HashSecret"] ?? string.Empty;
            var baseUrl = _config["VnPay:BaseUrl"] ?? _config["VnPay:PaymentUrl"] ?? "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";
            var returnUrl = req.ReturnUrl ?? _config["VnPay:ReturnUrl"] ?? string.Empty;

            if (string.IsNullOrWhiteSpace(tmnCode) || string.IsNullOrWhiteSpace(secret) || string.IsNullOrWhiteSpace(returnUrl))
                return BadRequest("VNPay chưa cấu hình đầy đủ.");

            // Create VNPay parameters using helper
            var amount = (long)req.Amount * 100; // VNPay requires amount * 100
            var txnRef = PaymentHelper.GenerateWalletTxnRef(); // Use helper for transaction reference
            var ipAddress = _paymentService.GetClientIpAddress();

            var vnpParameters = PaymentHelper.CreateVnPayParameters(
                tmnCode, amount, txnRef, $"WalletTopup:{txnRef}", returnUrl, ipAddress, req.Locale ?? "vn");

            // Create VNPay URL using helper
            var url = PaymentHelper.CreateVnPayUrl(baseUrl, vnpParameters, secret);

            // Get user information using service
            var (userEmail, userId) = _paymentService.GetCurrentUserInfo();

            // Create and save payment record using service
            await _paymentService.CreateWalletTopupPaymentAsync(req.WalletId, req.Amount, txnRef, userEmail, userId);

            return Ok(new VnPayCreateResponse { Url = url });
        }
    }
}



