using DakLakCoffeeSupplyChain.Common.Enum.RoleEnums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.DTOs.RoleDTOs
{
    public class RoleUpdateDto
    {
        [Required(ErrorMessage = "ID vai trò là bắt buộc.")]
        public int RoleId { get; set; }

        [Required(ErrorMessage = "Role name is required.")]
        [StringLength(100, ErrorMessage = "Role name must be at most 100 characters.")]
        public string RoleName { get; set; } = string.Empty;

        [StringLength(250, ErrorMessage = "Description must be at most 250 characters.")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Status is required.")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public RoleStatus Status { get; set; } = RoleStatus.Inactive;
    }
}
