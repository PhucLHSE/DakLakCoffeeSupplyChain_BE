using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.DTOs.BusinessBuyerDTOs
{
    public class BusinessBuyerViewAllDto
    {
        public Guid BuyerId { get; set; }

        public string BuyerCode { get; set; } = string.Empty;

        public string CompanyName { get; set; } = string.Empty;

        public string ContactPerson { get; set; } = string.Empty;

        public string Position { get; set; } = string.Empty;

        public string CompanyAddress { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public string CreatedByName { get; set; } = string.Empty;
    }
}
