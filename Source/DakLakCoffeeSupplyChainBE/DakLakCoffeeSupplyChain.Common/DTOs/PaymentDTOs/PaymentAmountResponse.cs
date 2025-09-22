namespace DakLakCoffeeSupplyChain.Common.DTOs.PaymentDTOs
{
    /// <summary>
    /// Response DTO cho thông tin phí thanh toán
    /// </summary>
    public class PaymentAmountResponse
    {
        /// <summary>
        /// Số tiền phí
        /// </summary>
        public int Amount { get; set; }

        /// <summary>
        /// Loại phí
        /// </summary>
        public string FeeType { get; set; } = string.Empty;

        /// <summary>
        /// Mô tả phí
        /// </summary>
        public string Description { get; set; } = string.Empty;
    }
}

