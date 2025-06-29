using System;
using System.ComponentModel.DataAnnotations;

namespace DakLakCoffeeSupplyChain.Common.DTOs.BusinessStaffDTOs
{
    public class BusinessStaffCreateDto
    {
        // Tài khoản người dùng
        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; }

        [Required]
        [StringLength(100)]
        public string FullName { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 6)]
        public string Password { get; set; }

        [Phone]
        public string? PhoneNumber { get; set; }

        // Nhân viên
        //[Required]
        //public Guid SupervisorId { get; set; }

        [Required]
        [StringLength(50)]
        public string Position { get; set; }

        [StringLength(100)]
        public string Department { get; set; }

        public Guid? AssignedWarehouseId { get; set; }
    }
}
