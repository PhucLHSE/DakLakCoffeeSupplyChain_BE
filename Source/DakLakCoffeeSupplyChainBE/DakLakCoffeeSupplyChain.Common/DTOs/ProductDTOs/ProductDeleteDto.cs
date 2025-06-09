using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.DTOs.ProductDTOs
{
    public class ProductDeleteDto
    {
        [Required]
        public Guid ProductId { get; set; }
    }
}
