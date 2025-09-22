using System.ComponentModel.DataAnnotations;

namespace DakLakCoffeeSupplyChain.Common.DTOs.PaymentDTOs
{
    public class ProcessPaymentSuccessRequest
    {
        [Required(ErrorMessage = "TxnRef is required")]
        public string TxnRef { get; set; } = string.Empty;

        [Required(ErrorMessage = "OrderInfo is required")]
        public string OrderInfo { get; set; } = string.Empty;

        public string? ResponseCode { get; set; }
        public string? Amount { get; set; }
    }
}
