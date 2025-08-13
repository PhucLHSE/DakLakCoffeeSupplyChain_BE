using System;
using System.ComponentModel.DataAnnotations;

namespace DakLakCoffeeSupplyChain.Common.DTOs.WalletDTOs
{
    public class WalletUpdateDto
    {
        [StringLength(50)]
        public string? WalletType { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "TotalBalance must be >= 0.")]
        public double? TotalBalance { get; set; }
    }
}
