using System.ComponentModel.DataAnnotations;

namespace DakLakCoffeeSupplyChain.Common.DTOs.WalletDTOs
{
    public class WalletTopupRequestDto
    {
        [Required(ErrorMessage = "WalletId is required.")]
        public Guid WalletId { get; set; }

        [Required(ErrorMessage = "Amount is required.")]
        [Range(1000, double.MaxValue, ErrorMessage = "Amount must be at least 1,000 VND.")]
        public double Amount { get; set; }

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
        public string? Description { get; set; }

        public string? ReturnUrl { get; set; }
        public string? Locale { get; set; } = "vn";
    }
}
