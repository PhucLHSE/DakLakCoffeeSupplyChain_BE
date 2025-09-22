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
        [Authorize(Roles = "BusinessManager,Admin")]
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
        [Authorize(Roles = "BusinessManager,Admin")]
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

            // Create and save payment record with the same txnRef
            var payment = _paymentService.CreatePaymentRecordWithTxnRef(req.PlanId, paymentConfig, userEmail, userId, txnRef);
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

        /// <summary>
        /// VNPay IPN endpoint - xử lý thông báo thanh toán từ VNPay
        /// </summary>
        [HttpPost("vnpay/ipn")]
        [AllowAnonymous]
        public async Task<IActionResult> VnPayIpn()
        {
            try
            {
                // Lấy tất cả parameters từ VNPay
                var vnpParams = new Dictionary<string, string>();
                foreach (var key in Request.Form.Keys)
                {
                    vnpParams[key] = Request.Form[key];
                }

                // Lấy thông tin cần thiết
                var vnp_ResponseCode = vnpParams.GetValueOrDefault("vnp_ResponseCode");
                var vnp_TxnRef = vnpParams.GetValueOrDefault("vnp_TxnRef");
                var vnp_Amount = vnpParams.GetValueOrDefault("vnp_Amount");
                var vnp_OrderInfo = vnpParams.GetValueOrDefault("vnp_OrderInfo");

                // Kiểm tra thanh toán thành công
                if (vnp_ResponseCode == "00" && !string.IsNullOrEmpty(vnp_TxnRef))
                {
                    // Xác định loại thanh toán từ OrderInfo
                    if (vnp_OrderInfo?.StartsWith("PlanPosting:") == true)
                    {
                        // Xử lý thanh toán phí kế hoạch thu mua
                        var planIdStr = vnp_OrderInfo.Replace("PlanPosting:", "").Split(':')[0];
                        if (Guid.TryParse(planIdStr, out var planId))
                        {
                            // Lấy PaymentConfiguration cho PlanPosting
                            var paymentConfig = await _paymentService.GetPaymentConfigurationByContext(2, "PlanPosting"); // RoleID = 2 cho BusinessManager
                            if (paymentConfig != null)
                            {
                                await _paymentService.ProcessMockIpnAsync(planId, vnp_TxnRef, paymentConfig);
                                return Ok(new { RspCode = "00", Message = "Success" });
                            }
                        }
                    }
                    else if (vnp_OrderInfo?.StartsWith("WalletTopup:") == true)
                    {
                        // Xử lý nạp tiền ví - logic này đã có trong WalletService
                        // Không cần xử lý ở đây vì WalletService đã handle
                        return Ok(new { RspCode = "00", Message = "Success" });
                    }
                }

                return Ok(new { RspCode = "01", Message = "Order not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing VNPay IPN");
                return Ok(new { RspCode = "99", Message = "Unknown error" });
            }
        }

        /// <summary>
        /// Xử lý thanh toán thành công từ frontend
        /// </summary>
        [HttpPost("process-payment-success")]
        [Authorize(Roles = "BusinessManager,Admin")]
        public async Task<IActionResult> ProcessPaymentSuccess([FromBody] ProcessPaymentSuccessRequest req)
        {
            try
            {
                if (string.IsNullOrEmpty(req.TxnRef) || string.IsNullOrEmpty(req.OrderInfo))
                {
                    return BadRequest("Thiếu thông tin giao dịch");
                }

                // Xác định loại thanh toán từ OrderInfo
                if (req.OrderInfo.StartsWith("PlanPosting:"))
                {
                    // Xử lý thanh toán phí kế hoạch thu mua
                    var planIdStr = req.OrderInfo.Replace("PlanPosting:", "").Split(':')[0];
                    if (Guid.TryParse(planIdStr, out var planId))
                    {
                        // Lấy PaymentConfiguration cho PlanPosting
                        var userRoleId = _paymentService.GetCurrentUserRoleId();
                        if (userRoleId == null)
                        {
                            return BadRequest("Không thể xác định vai trò của người dùng.");
                        }

                        var paymentConfig = await _paymentService.GetPaymentConfigurationByContext(userRoleId.Value, "PlanPosting");
                        if (paymentConfig != null)
                        {
                            await _paymentService.ProcessMockIpnAsync(planId, req.TxnRef, paymentConfig);
                            return Ok(new { message = "Thanh toán đã được xử lý thành công", planId });
                        }
                    }
                }

                return BadRequest("Không thể xử lý loại thanh toán này");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing payment success");
                return StatusCode(500, "Có lỗi xảy ra khi xử lý thanh toán");
            }
        }

        /// <summary>
        /// Xử lý thanh toán qua ví nội bộ
        /// </summary>
        [HttpPost("wallet-payment")]
        [Authorize(Roles = "BusinessManager,Admin")]
        public async Task<IActionResult> ProcessWalletPayment([FromBody] WalletPaymentRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.PlanId) || request.Amount <= 0)
                {
                    return BadRequest(new { success = false, message = "Thông tin thanh toán không hợp lệ" });
                }

                // Lấy thông tin user hiện tại
                var (email, userId) = _paymentService.GetCurrentUserInfo();
                if (string.IsNullOrEmpty(userId))
                {
                    return BadRequest(new { success = false, message = "Không thể xác định người dùng" });
                }

                if (!Guid.TryParse(userId, out var userGuid))
                {
                    return BadRequest(new { success = false, message = "ID người dùng không hợp lệ" });
                }

                if (!Guid.TryParse(request.PlanId, out var planId))
                {
                    return BadRequest(new { success = false, message = "ID kế hoạch không hợp lệ" });
                }

                // Xử lý thanh toán qua ví
                var result = await _paymentService.ProcessWalletPaymentAsync(planId, request.Amount, userGuid, request.Description);
                
                if (result.Success)
                {
                    return Ok(new { 
                        success = true, 
                        message = "Thanh toán thành công", 
                        transactionId = result.TransactionId 
                    });
                }
                else
                {
                    return BadRequest(new { 
                        success = false, 
                        message = result.Message 
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing wallet payment");
                return StatusCode(500, new { 
                    success = false, 
                    message = "Có lỗi xảy ra khi xử lý thanh toán" 
                });
            }
        }

        /// <summary>
        /// Lấy thông tin ví System (Admin wallet)
        /// </summary>
        [HttpGet("system-wallet")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetSystemWallet()
        {
            try
            {
                var systemWallet = await _paymentService.GetOrCreateSystemWalletAsync();
                return Ok(new
                {
                    walletId = systemWallet.WalletId,
                    walletType = systemWallet.WalletType,
                    totalBalance = systemWallet.TotalBalance,
                    lastUpdated = systemWallet.LastUpdated
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi khi lấy thông tin ví System: {ex.Message}");
            }
        }


        [HttpPost("wallet-topup/vnpay/create-url")]
        [Authorize(Roles = "BusinessManager,BusinessStaff,Farmer,Admin")]
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



