using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.DTOs.CropStageDTOs
{
    public class CropStageCreateDto
    {
        [Required(ErrorMessage = "Mã giai đoạn là bắt buộc.")]
        [StringLength(50, ErrorMessage = "Mã giai đoạn không vượt quá 50 ký tự.")]
        public string StageCode { get; set; } = null!;

        [Required(ErrorMessage = "Tên giai đoạn là bắt buộc.")]
        [StringLength(100, ErrorMessage = "Tên giai đoạn không quá 100 ký tự.")]
        public string StageName { get; set; } = null!;

        [StringLength(250, ErrorMessage = "Mô tả không quá 250 ký tự.")]
        public string? Description { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Thứ tự giai đoạn phải từ 1 trở lên.")]
        public int? OrderIndex { get; set; }
    }
}
