using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.DTOs.UserAccountDTOs
{
    public class UserAccountDeleteDto
    {
        [Required]
        public Guid UserId { get; set; }
    }
}
