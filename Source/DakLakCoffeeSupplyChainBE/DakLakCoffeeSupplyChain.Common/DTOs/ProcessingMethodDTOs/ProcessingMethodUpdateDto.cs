using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.DTOs.ProcessingMethodDTOs
{
    public class ProcessingMethodUpdateDto
    {
        public int MethodId { get; set; }
        
        [Required(ErrorMessage = "Mã phương pháp không được để trống")]
        [StringLength(50, ErrorMessage = "Mã phương pháp không được vượt quá 50 ký tự")]
        public string MethodCode { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Tên phương pháp không được để trống")]
        [StringLength(200, ErrorMessage = "Tên phương pháp không được vượt quá 200 ký tự")]
        public string Name { get; set; } = string.Empty;
        
        [StringLength(500, ErrorMessage = "Mô tả không được vượt quá 500 ký tự")]
        public string? Description { get; set; }
    }
}
