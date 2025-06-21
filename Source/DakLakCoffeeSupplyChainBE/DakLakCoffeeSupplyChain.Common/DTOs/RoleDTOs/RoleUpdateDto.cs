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

        [Required(ErrorMessage = "Tên vai trò không được để trống.")]
        [StringLength(100, ErrorMessage = "Tên vai trò không được vượt quá 100 ký tự.")]
        public string RoleName { get; set; } = string.Empty;

        [StringLength(250, ErrorMessage = "Mô tả không được vượt quá 250 ký tự.")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Trạng thái là bắt buộc.")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public RoleStatus Status { get; set; } = RoleStatus.Inactive;
    }
}
