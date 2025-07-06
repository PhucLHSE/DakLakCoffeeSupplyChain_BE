using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.DTOs.OrderDTOs
{
    public class OrderDeleteDto
    {
        [Required]
        public Guid OrderId { get; set; }
    }
}
