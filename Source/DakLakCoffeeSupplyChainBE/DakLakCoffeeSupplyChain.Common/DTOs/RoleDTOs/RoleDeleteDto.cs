using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.DTOs.RoleDTOs
{
    public class RoleDeleteDto
    {
        [Required]
        public int RoleId { get; set; }
    }
}
