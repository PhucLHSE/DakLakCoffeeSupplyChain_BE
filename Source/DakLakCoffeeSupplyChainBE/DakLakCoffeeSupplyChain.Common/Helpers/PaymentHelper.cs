using System.Security.Cryptography;
using System.Text;

namespace DakLakCoffeeSupplyChain.Common.Helpers
{
    public static class PaymentHelper
    {
        /// <summary>
        /// Tạo HMAC SHA512 hash cho VNPay
        /// </summary>
        /// <param name="key">Secret key</param>
        /// <param name="data">Data cần hash</param>
        /// <returns>Hash string</returns>
        public static string CreateHmac512(string key, string data)
        {
            using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(key));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            return Convert.ToHexString(hash).ToUpperInvariant();
        }

        /// <summary>
        /// Tạo VNPay URL với các tham số
        /// </summary>
        /// <param name="baseUrl">Base URL của VNPay</param>
        /// <param name="parameters">Dictionary chứa các tham số</param>
        /// <param name="secret">Secret key để tạo hash</param>
        /// <returns>VNPay URL hoàn chỉnh</returns>
        public static string CreateVnPayUrl(string baseUrl, Dictionary<string, string> parameters, string secret)
        {
            // Sắp xếp parameters theo key
            var sortedParams = new SortedDictionary<string, string>(parameters);
            
            // Tạo encoded string cho hash
            var encodedForHash = string.Join('&', sortedParams.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
            
            // Tạo hash
            var secureHash = CreateHmac512(secret, encodedForHash);
            
            // Tạo query string
            var query = string.Join('&', sortedParams.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
            
            // Tạo URL hoàn chỉnh
            return $"{baseUrl}?{query}&vnp_SecureHashType=HmacSHA512&vnp_SecureHash={secureHash}";
        }

        /// <summary>
        /// Tạo VNPay parameters cho thanh toán
        /// </summary>
        /// <param name="tmnCode">Terminal code</param>
        /// <param name="amount">Số tiền (đã nhân 100)</param>
        /// <param name="txnRef">Transaction reference</param>
        /// <param name="orderInfo">Thông tin đơn hàng</param>
        /// <param name="returnUrl">URL trả về</param>
        /// <param name="ipAddress">IP address</param>
        /// <param name="locale">Ngôn ngữ</param>
        /// <returns>Dictionary chứa các tham số VNPay</returns>
        public static Dictionary<string, string> CreateVnPayParameters(
            string tmnCode,
            long amount,
            string txnRef,
            string orderInfo,
            string returnUrl,
            string ipAddress,
            string locale = "vn")
        {
            return new Dictionary<string, string>
            {
                ["vnp_Version"] = "2.1.0",
                ["vnp_Command"] = "pay",
                ["vnp_TmnCode"] = tmnCode,
                ["vnp_Amount"] = amount.ToString(),
                ["vnp_CreateDate"] = DateTime.UtcNow.ToString("yyyyMMddHHmmss"),
                ["vnp_CurrCode"] = "VND",
                ["vnp_IpAddr"] = ipAddress,
                ["vnp_Locale"] = locale,
                ["vnp_OrderInfo"] = orderInfo,
                ["vnp_OrderType"] = "other",
                ["vnp_ReturnUrl"] = returnUrl,
                ["vnp_TxnRef"] = txnRef
            };
        }

        /// <summary>
        /// Tạo payment code duy nhất
        /// </summary>
        /// <returns>Payment code</returns>
        public static string GeneratePaymentCode()
        {
            return $"PAY-{DateTime.UtcNow:yyyyMMddHHmmss}";
        }

        /// <summary>
        /// Tạo transaction reference từ PlanId
        /// </summary>
        /// <param name="planId">Plan ID</param>
        /// <returns>Transaction reference</returns>
        public static string GenerateTxnRef(Guid planId)
        {
            // 17 ký tự thời gian (UTC tới mili giây) + 3 ký tự ngẫu nhiên + planId
            // => 20 ký tự đầu (timestamp + random) luôn khác nhau giữa các lần gọi
            var ts17 = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff"); // 17
            var rand3 = Guid.NewGuid().ToString("N").Substring(0, 3); // 3
            return $"{ts17}{rand3}{planId:N}"; // ví dụ: 20250202143015999abc439852bb9ddb4b50950f...
        }

        /// <summary>
        /// Tạo transaction reference cho wallet topup
        /// </summary>
        /// <returns>Transaction reference</returns>
        public static string GenerateWalletTxnRef()
        {
            return Guid.NewGuid().ToString("N"); // 32 chars
        }
    }
}
