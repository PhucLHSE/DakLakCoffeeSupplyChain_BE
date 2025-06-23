using System.ComponentModel.DataAnnotations;

namespace DakLakCoffeeSupplyChain.Common.DTOs.AuthDTOs
{
    public class SignUpRequestDto : IValidatableObject
    {
        [Required(ErrorMessage = "Email là bắt buộc.")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mật khẩu là bắt buộc.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu phải từ 6 đến 100 ký tự.")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tên người đại diện là bắt buộc.")]
        [StringLength(100, ErrorMessage = "Tên không được vượt quá 100 ký tự.")]
        public string Name { get; set; } = string.Empty;
        [Required(ErrorMessage = "Vai trò của tài khoản không được để trống")]
        public int RoleId { get; set; }
        [Required(ErrorMessage = "Số điện thoại không được để trống")]
        public string Phone { get; set; } = string.Empty;

        // Những trường từ đây xuống dành cho business manager
        public string CompanyName { get; set; } = string.Empty;
        public string TaxId { get; set; } = string.Empty;
        public string BusinessLicenseURl { get; set; } = string.Empty;

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            const int BUSINESS_ROLE_ID = 2; // Hard code

            if (RoleId == BUSINESS_ROLE_ID)
            {
                if (string.IsNullOrWhiteSpace(CompanyName))
                    yield return new ValidationResult("Tên công ty là bắt buộc.", [nameof(CompanyName)]);

                if (string.IsNullOrWhiteSpace(TaxId))
                    yield return new ValidationResult("Mã số thuế là bắt buộc.", [nameof(TaxId)]);
                else if (!System.Text.RegularExpressions.Regex.IsMatch(TaxId, @"^\d{10,14}$"))
                    yield return new ValidationResult("Mã số thuế phải từ 10 đến 14 chữ số.", [nameof(TaxId)] );

                if (string.IsNullOrWhiteSpace(BusinessLicenseURl))
                    yield return new ValidationResult("Đường dẫn giấy phép kinh doanh là bắt buộc.", [nameof(BusinessLicenseURl)] );
            }
        }
    }
}
