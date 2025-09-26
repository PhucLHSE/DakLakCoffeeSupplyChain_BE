using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.DTOs.CropDTOs
{
    public class CropCreateDto
    {
        // CropCode sẽ được tự động tạo trong service, không cần trong DTO
        // public string CropCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Address là bắt buộc")]
        [StringLength(500, ErrorMessage = "Address không được vượt quá 500 ký tự")]
        public string Address { get; set; } = string.Empty;

        [Required(ErrorMessage = "FarmName là bắt buộc")]
        [StringLength(200, ErrorMessage = "FarmName không được vượt quá 200 ký tự")]
        public string FarmName { get; set; } = string.Empty;

        [Range(0.01, double.MaxValue, ErrorMessage = "CropArea phải lớn hơn 0")]
        public decimal? CropArea { get; set; }

        // Status auto set "Active" khi tạo mới
        // public string Status { get; set; } = "Active";
    }
}
