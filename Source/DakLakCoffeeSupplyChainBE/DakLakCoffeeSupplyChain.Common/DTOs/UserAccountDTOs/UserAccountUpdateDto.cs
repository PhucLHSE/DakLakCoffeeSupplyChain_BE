using DakLakCoffeeSupplyChain.Common.Enum.UserAccountEnums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.DTOs.UserAccountDTOs
{
    public class UserAccountUpdateDto
    {
        [Required(ErrorMessage = "ID người dùng là bắt buộc.")]
        public Guid UserId { get; set; }

        [Required(ErrorMessage = "Email không được để trống.")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
        [MaxLength(255, ErrorMessage = "Email không được vượt quá 255 ký tự.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Số điện thoại không được để trống.")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
        [StringLength(15, MinimumLength = 10, ErrorMessage = "Số điện thoại phải từ 10 đến 15 ký tự.")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tên không được để trống.")]
        [MaxLength(255, ErrorMessage = "Tên không được vượt quá 255 ký tự.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Giới tính không được để trống.")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public Gender Gender { get; set; }

        public DateOnly? DateOfBirth { get; set; }

        [MaxLength(255)]
        public string Address { get; set; } = string.Empty;

        [Url(ErrorMessage = "Link ảnh không hợp lệ.")]
        public string? ProfilePictureUrl { get; set; }

        [Required(ErrorMessage = "Trạng thái tài khoản không được để trống.")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public UserAccountStatus Status { get; set; }

        [Required(ErrorMessage = "Tên vai trò không được để trống.")]
        public string RoleName { get; set; } = string.Empty;
    }
}
