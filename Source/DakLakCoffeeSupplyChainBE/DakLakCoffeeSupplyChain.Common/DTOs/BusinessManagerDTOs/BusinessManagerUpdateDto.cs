using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.DTOs.BusinessManagerDTOs
{
    public class BusinessManagerUpdateDto
    {
        [Required(ErrorMessage = "Mã quản lý không được để trống.")]
        public Guid ManagerId { get; set; }

        [Required(ErrorMessage = "Tên công ty không được để trống.")]
        [StringLength(100, ErrorMessage = "Tên công ty không được vượt quá 100 ký tự.")]
        public string CompanyName { get; set; } = string.Empty;

        [StringLength(50, ErrorMessage = "Chức vụ không được vượt quá 50 ký tự.")]
        public string Position { get; set; } = string.Empty;

        [StringLength(100, ErrorMessage = "Phòng ban không được vượt quá 100 ký tự.")]
        public string Department { get; set; } = string.Empty;

        [Required(ErrorMessage = "Địa chỉ công ty không được để trống.")]
        [StringLength(255, ErrorMessage = "Địa chỉ công ty không được vượt quá 255 ký tự.")]
        public string CompanyAddress { get; set; } = string.Empty;

        [RegularExpression(@"^\d{10,14}$", ErrorMessage = "Mã số thuế phải từ 10 đến 14 chữ số.")]
        public string TaxId { get; set; } = string.Empty;

        [StringLength(255, ErrorMessage = "Website không được vượt quá 255 ký tự.")]
        [Url(ErrorMessage = "Website không hợp lệ.")]
        public string? Website { get; set; }

        [Required(ErrorMessage = "Email liên hệ không được để trống.")]
        [StringLength(100, ErrorMessage = "Email liên hệ không được vượt quá 100 ký tự.")]
        [EmailAddress(ErrorMessage = "Email liên hệ không hợp lệ.")]
        public string ContactEmail { get; set; } = string.Empty;

        [StringLength(255, ErrorMessage = "Đường dẫn giấy phép kinh doanh không được vượt quá 255 ký tự.")]
        [Url(ErrorMessage = "Đường dẫn giấy phép kinh doanh không hợp lệ.")]
        public string? BusinessLicenseUrl { get; set; }

        public bool? IsCompanyVerified { get; set; }
    }
}
