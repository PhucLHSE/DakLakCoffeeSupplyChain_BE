using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.DTOs.PaymentDTOs
{
   public class PayOSWebhookDto
    {
        public string OrderCode { get; set; }
        public int Status { get; set; }
        public string PaymentMethod { get; set; }
        public decimal Amount { get; set; }
        public DateTime PaidTime { get; set; }
    }
}
