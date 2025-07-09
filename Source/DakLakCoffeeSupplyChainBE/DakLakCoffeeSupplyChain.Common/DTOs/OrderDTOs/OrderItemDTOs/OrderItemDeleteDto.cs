using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.DTOs.OrderDTOs.OrderItemDTOs
{
    public class OrderItemDeleteDto
    {
        [Required]
        public Guid OrderItemId { get; set; }
    }
}
