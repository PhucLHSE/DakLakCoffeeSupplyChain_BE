using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.DTOs.BusinessStaffDTOs
{
    public class BusinessStaffUpdateDto
    {
        [Required]
        public Guid StaffId { get; set; }

        [Required]
        [StringLength(50)]
        public string Position { get; set; }

        [Required]
        [StringLength(100)]
        public string Department { get; set; }

        public Guid? AssignedWarehouseId { get; set; }

        public bool? IsActive { get; set; }
    }
}
