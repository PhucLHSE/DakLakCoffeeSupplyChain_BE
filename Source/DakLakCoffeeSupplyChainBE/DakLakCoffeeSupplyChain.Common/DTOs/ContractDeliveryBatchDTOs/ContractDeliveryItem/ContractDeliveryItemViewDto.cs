using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.DTOs.ContractDeliveryBatchDTOs.ContractDeliveryItem
{
    public class ContractDeliveryItemViewDto
    {
        public Guid DeliveryItemId { get; set; }

        public string DeliveryItemCode { get; set; } = string.Empty;

        public Guid ContractItemId { get; set; }

        public string CoffeeTypeName { get; set; } = string.Empty;

        public double PlannedQuantity { get; set; }

        public double? FulfilledQuantity { get; set; }

        public string Note { get; set; } = string.Empty;
    }
}
