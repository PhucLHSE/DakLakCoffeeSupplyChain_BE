using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.DTOs.ContractDTOs.ContractItemDTOs
{
    public class ContractItemDeleteDto
    {
        [Required]
        public Guid ContractItemId { get; set; }
    }
}
