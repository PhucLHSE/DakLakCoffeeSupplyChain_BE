using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.DTOs.BusinessStaffDTOs
{
    public class BusinessStaffDetailDto
    {
        public Guid StaffId { get; set; }

        public string StaffCode { get; set; } = null!;

        public string FullName { get; set; } = null!;

        public string Email { get; set; } = null!;

        public string? PhoneNumber { get; set; }

        public string Department { get; set; } = null!;

        public string Position { get; set; } = null!;

        public Guid? AssignedWarehouseId { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
