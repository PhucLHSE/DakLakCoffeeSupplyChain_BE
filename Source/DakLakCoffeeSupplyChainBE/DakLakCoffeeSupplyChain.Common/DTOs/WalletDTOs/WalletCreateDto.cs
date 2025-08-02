using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.DTOs.WalletDTOs
{
    public class WalletCreateDto
    {
        [Required(ErrorMessage = "UserId is required.")]
        public Guid UserId { get; set; }

        [Required(ErrorMessage = "WalletType is required.")]
        [StringLength(50)]
        public string WalletType { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "TotalBalance must be >= 0.")]
        public double TotalBalance { get; set; }
    }
}
