using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.DTOs.ProcessingMethodStageDTOs
{
    public class ProcessingStageUpdateDto
    {
        public int StageId { get; set; }              
        
        [Required(ErrorMessage = "Mã giai đoạn không được để trống")]
        [StringLength(50, ErrorMessage = "Mã giai đoạn không được vượt quá 50 ký tự")]
        public string StageCode { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Tên giai đoạn không được để trống")]
        [StringLength(200, ErrorMessage = "Tên giai đoạn không được vượt quá 200 ký tự")]
        public string StageName { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Mô tả không được để trống")]
        [StringLength(500, ErrorMessage = "Mô tả không được vượt quá 500 ký tự")]
        public string Description { get; set; } = string.Empty;
        
        public int OrderIndex { get; set; }
        public bool IsRequired { get; set; }
        public int MethodId { get; set; }            
    }
}
