using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.DTOs.ContractDeliveryBatchDTOs
{
    public class ContractDeliveryBatchDeleteDto
    {
        [Required]
        public Guid DeliveryBatchId { get; set; }
    }
}
