namespace DakLakCoffeeSupplyChain.Common.DTOs.PaymentDTOs
{
    /// <summary>
    /// Response DTO cho việc tạo VNPay URL
    /// </summary>
    public class VnPayCreateResponse
    {
        /// <summary>
        /// URL thanh toán VNPay
        /// </summary>
        public string Url { get; set; } = string.Empty;
    }
}
