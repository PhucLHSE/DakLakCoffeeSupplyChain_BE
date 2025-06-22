using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.DTOs.ContractDTOs.ContractItemDTOs
{
    public class ContractItemViewDto
    {
        public Guid ContractItemId { get; set; }

        public string ContractItemCode { get; set; } = string.Empty;

        public Guid CoffeeTypeId { get; set; }

        public string CoffeeTypeName { get; set; } = string.Empty;

        public double? Quantity { get; set; }

        public double? UnitPrice { get; set; }

        public double? DiscountAmount { get; set; }

        public string Note { get; set; } = string.Empty;
    }
}
