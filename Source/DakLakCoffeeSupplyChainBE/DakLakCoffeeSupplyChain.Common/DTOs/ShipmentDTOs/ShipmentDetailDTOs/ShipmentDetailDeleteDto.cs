using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.DTOs.ShipmentDTOs.ShipmentDetailDTOs
{
    public class ShipmentDetailDeleteDto
    {
        [Required]
        public Guid ShipmentDetailId { get; set; }
    }
}
