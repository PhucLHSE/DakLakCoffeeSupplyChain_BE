using System;
using System.ComponentModel.DataAnnotations;

namespace DakLakCoffeeSupplyChain.Common.DTOs.AgriculturalExpertDTOs
{
    public class AgriculturalExpertCreateDto
    {
        [Required(ErrorMessage = "Lĩnh vực chuyên môn không được để trống.")]
        [StringLength(100, ErrorMessage = "Lĩnh vực chuyên môn không được vượt quá 100 ký tự.")]
        public string ExpertiseArea { get; set; } = string.Empty;

        [Required(ErrorMessage = "Bằng cấp/chứng chỉ không được để trống.")]
        [StringLength(200, ErrorMessage = "Bằng cấp/chứng chỉ không được vượt quá 200 ký tự.")]
        public string Qualifications { get; set; } = string.Empty;

        [Range(0, 50, ErrorMessage = "Số năm kinh nghiệm phải từ 0 đến 50 năm.")]
        public int? YearsOfExperience { get; set; }

        [Required(ErrorMessage = "Tổ chức liên kết không được để trống.")]
        [StringLength(200, ErrorMessage = "Tổ chức liên kết không được vượt quá 200 ký tự.")]
        public string AffiliatedOrganization { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "Tiểu sử không được vượt quá 1000 ký tự.")]
        public string? Bio { get; set; }

        [Range(0.0, 5.0, ErrorMessage = "Đánh giá phải từ 0.0 đến 5.0.")]
        public double? Rating { get; set; }
    }
}
