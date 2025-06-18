using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.DTOs.BusinessManagerDTOs
{
    public class BusinessManagerDeleteDto
    {
        [Required]
        public Guid ManagerId { get; set; }
    }
}
