using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.DTOs.BusinessBuyerDTOs
{
    public class BusinessBuyerCreateDto
    {
        [Required(ErrorMessage = "Tên công ty không được để trống.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Tên công ty phải từ 2 đến 100 ký tự.")]
        public string CompanyName { get; set; } = string.Empty;

        [StringLength(100, ErrorMessage = "Tên người đại diện không được vượt quá 100 ký tự.")]
        [RegularExpression(@"^[\p{L}\s'.-]+$", ErrorMessage = "Tên người đại diện không hợp lệ.")]
        public string ContactPerson { get; set; } = string.Empty;

        [StringLength(50, ErrorMessage = "Chức vụ không được vượt quá 50 ký tự.")]
        public string Position { get; set; } = string.Empty;

        [StringLength(255, ErrorMessage = "Địa chỉ công ty không được vượt quá 255 ký tự.")]
        public string CompanyAddress { get; set; } = string.Empty;

        [RegularExpression(@"^\d{10,14}$", ErrorMessage = "Mã số thuế phải từ 10 đến 14 chữ số.")]
        public string TaxId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email không được để trống.")]
        [EmailAddress(ErrorMessage = "Địa chỉ email không hợp lệ.")]
        [MaxLength(255, ErrorMessage = "Email không được vượt quá 255 ký tự.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Số điện thoại không được để trống.")]
        [RegularExpression(@"^\+?[0-9]{10,15}$", ErrorMessage = "Số điện thoại phải từ 10 đến 15 chữ số.")]
        public string PhoneNumber { get; set; } = string.Empty;

        [StringLength(255, ErrorMessage = "Website không được vượt quá 255 ký tự.")]
        [Url(ErrorMessage = "Website không hợp lệ.")]
        public string? Website { get; set; }
    }
}
