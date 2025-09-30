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

            // Lấy FrontendUrl từ cấu hình
            var frontendUrl = _config["AppSettings:FrontendUrl"];

            var returnUrl = $"{frontendUrl}/dashboard/manager/procurement-plans/payment-result?planId={req.PlanId}";


            if (string.IsNullOrWhiteSpace(tmnCode) || string.IsNullOrWhiteSpace(secret) || string.IsNullOrWhiteSpace(frontendUrl))
                return BadRequest("VNPay hoặc AppSettings:FrontendUrl chưa cấu hình đầy đủ.");

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

            return Ok(new VnPayCreateResponse { Url = url, PaymentId = payment.PaymentId.ToString() });
        }




        [HttpGet("vnpay/ipn")]
        [AllowAnonymous]
        public async Task<IActionResult> VnPayIpn()
        {
            try
            {
                var secret = _config["VnPay:HashSecret"] ?? string.Empty;

                // CHỈ LẤY THAM SỐ BẮT ĐẦU BẰNG "vnp_"
                var all = Request.Query.ToDictionary(k => k.Key, v => v.Value.ToString());
                var vnpParams = all
                    .Where(kv => kv.Key.StartsWith("vnp_", StringComparison.OrdinalIgnoreCase))
                    .ToDictionary(k => k.Key, v => v.Value);

                var receivedHash = vnpParams.GetValueOrDefault("vnp_SecureHash");
                if (string.IsNullOrEmpty(receivedHash))
                    return Ok(new { RspCode = "97", Message = "Invalid Signature" });

                vnpParams.Remove("vnp_SecureHash");
                vnpParams.Remove("vnp_SecureHashType");

                var sorted = new SortedDictionary<string, string>(vnpParams, StringComparer.Ordinal);
                var dataToHash = string.Join('&', sorted.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
                var calc = PaymentHelper.CreateHmac512(secret, dataToHash);

                if (!string.Equals(receivedHash, calc, StringComparison.InvariantCultureIgnoreCase))
                    return Ok(new { RspCode = "97", Message = "Invalid Signature" });

                var (rspCode, message) = await _paymentService.ProcessRealIpnAsync(vnpParams);
                return Ok(new { RspCode = rspCode, Message = message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing VNPay IPN");
                return Ok(new { RspCode = "99", Message = "Unknown error" });
            }
        }
       
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
            var tmnCode = _config["VnPay:TmnCode"] ?? string.Empty;
            var secret = _config["VnPay:HashSecret"] ?? string.Empty;
            var baseUrl = _config["VnPay:BaseUrl"] ?? _config["VnPay:PaymentUrl"] ?? "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";

            // Lấy FrontendUrl từ cấu hình
            var frontendUrl = _config["AppSettings:FrontendUrl"];

            // <<< DÒNG CŨ (SAI)
            // var returnUrl = req.ReturnUrl ?? _config["VnPay:WalletReturnUrl"] ?? _config["VnPay:ReturnUrl"] ?? string.Empty;

            // ✅ <<< DÒNG MỚI (ĐÚNG)
            // Trỏ về trang /wallet-topup-success của FE.
            var returnUrl = $"{frontendUrl}/dashboard/wallet/topup/success";

            if (string.IsNullOrWhiteSpace(tmnCode) || string.IsNullOrWhiteSpace(secret) || string.IsNullOrWhiteSpace(frontendUrl))
                return BadRequest("VNPay hoặc AppSettings:FrontendUrl chưa cấu hình đầy đủ.");

            var amountX100 = (long)req.Amount * 100;
            var txnRef = PaymentHelper.GenerateWalletTxnRef();
            var ipAddress = _paymentService.GetClientIpAddress();

            var vnpParameters = PaymentHelper.CreateVnPayParameters(
                tmnCode, amountX100, txnRef, $"WalletTopup:{txnRef}", returnUrl, ipAddress, req.Locale ?? "vn");

            var url = PaymentHelper.CreateVnPayUrl(baseUrl, vnpParameters, secret);

            var (userEmail, userId) = _paymentService.GetCurrentUserInfo();
            await _paymentService.CreateWalletTopupPaymentAsync(req.WalletId, req.Amount, txnRef, userEmail, userId);

            return Ok(new VnPayCreateResponse { Url = url, PaymentId = txnRef });
        }

        [HttpGet("{planId}/payment-status")]
        [Authorize(Roles = "BusinessManager,Admin")]
        public async Task<IActionResult> GetPaymentStatus(Guid planId)
        {
            // Tìm bản ghi thanh toán tương ứng trong database
            var payment = (await _unitOfWork.PaymentRepository.GetAllAsync(p =>
                p.RelatedEntityId == planId &&
                p.PaymentPurpose == "PlanPosting"
            )).FirstOrDefault();

            if (payment == null)
            {
                // Nếu không tìm thấy, tức là chưa có thanh toán nào được tạo
                return Ok(new
                {
                    hasPayment = false,
                    paymentStatus = "Not Found",
                    message = "Chưa có thanh toán nào cho kế hoạch này."
                });
            }

            // Nếu tìm thấy, trả về trạng thái của thanh toán đó
            return Ok(new
            {
                hasPayment = true,
                paymentStatus = payment.PaymentStatus, // "Pending", "Success", hoặc "Failed"
                message = "Đã tìm thấy thông tin thanh toán.",
                paymentTime = payment.PaymentTime
            });
        }
        [HttpGet("vnpay/return")]
        [AllowAnonymous]
        public async Task<IActionResult> VnPayReturn()
        {
            try
            {
                var secret = _config["VnPay:HashSecret"] ?? string.Empty;

                // CHỈ LẤY THAM SỐ vnp_
                var all = Request.Query.ToDictionary(k => k.Key, v => v.Value.ToString());
                var vnpParams = all
                    .Where(kv => kv.Key.StartsWith("vnp_", StringComparison.OrdinalIgnoreCase))
                    .ToDictionary(k => k.Key, v => v.Value);

                var receivedHash = vnpParams.GetValueOrDefault("vnp_SecureHash");
                if (string.IsNullOrEmpty(receivedHash))
                    return BadRequest(new { code = "97", message = "Invalid Signature" });

                vnpParams.Remove("vnp_SecureHash");
                vnpParams.Remove("vnp_SecureHashType");

                var sorted = new SortedDictionary<string, string>(vnpParams, StringComparer.Ordinal);
                var dataToHash = string.Join('&', sorted.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
                var calc = PaymentHelper.CreateHmac512(secret, dataToHash);

                if (!string.Equals(receivedHash, calc, StringComparison.InvariantCultureIgnoreCase))
                    return BadRequest(new { code = "97", message = "Invalid Signature" });

                var (rspCode, message) = await _paymentService.ProcessRealIpnAsync(vnpParams);
                return Ok(new { code = rspCode, message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing VNPay return");
                return StatusCode(500, new { code = "99", message = "Unknown error" });
            }
        }

    }
}



