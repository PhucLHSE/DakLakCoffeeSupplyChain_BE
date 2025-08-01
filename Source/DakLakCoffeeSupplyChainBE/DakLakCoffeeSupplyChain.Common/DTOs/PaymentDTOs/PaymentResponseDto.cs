using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.DTOs.PaymentDTOs
{
    public class PaymentResponseDto
    {
        public Guid PaymentID { get; set; }
        public string Email { get; set; }
        public string PaymentPurpose { get; set; }
        public string PaymentStatus { get; set; }
        public string PaymentCode { get; set; }
        public string CheckoutUrl { get; set; }
    }
}
