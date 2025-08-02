using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.DTOs.PaymentDTOs
{
    public class PaymentCreateDto
{
    public string Email { get; set; }
    public Guid UserID { get; set; }
    public string PaymentPurpose { get; set; }
    public string ReturnUrl { get; set; }
    public string CancelUrl { get; set; }
}
}
