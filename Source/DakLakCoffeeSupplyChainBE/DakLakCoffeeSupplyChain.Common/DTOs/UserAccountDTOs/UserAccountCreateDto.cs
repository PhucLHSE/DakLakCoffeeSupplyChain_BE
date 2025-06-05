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
    public class UserAccountCreateDto
    {
        [Required]
        [EmailAddress]
        [MaxLength(255)]
        public string Email { get; set; } = string.Empty;

        [Phone]
        [StringLength(15, MinimumLength = 10, ErrorMessage = "Số điện thoại phải từ 10 đến 15 ký tự.")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public Gender Gender { get; set; } = Gender.Unknown;

        public DateOnly? DateOfBirth { get; set; }

        [MaxLength(255)]
        public string Address { get; set; } = string.Empty;

        [Url]
        public string? ProfilePictureUrl { get; set; }

        [Required]
        [StringLength(255, MinimumLength = 10, ErrorMessage = "Mật khẩu phải từ 10 đến 255 ký tự.")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&\-_#])[A-Za-z\d@$!%*?&\-_#]{10,}$",
            ErrorMessage = "Mật khẩu phải có ít nhất 1 chữ thường, 1 chữ hoa, 1 chữ số, 1 ký tự đặc biệt và tối thiểu 10 ký tự.")]
        public string Password { get; set; } = string.Empty;

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public LoginType LoginType { get; set; } = LoginType.System;

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public UserAccountStatus Status { get; set; } = UserAccountStatus.Unknown;

        [Required]
        public string RoleName { get; set; } = string.Empty;
    }
}
