using DakLakCoffeeSupplyChain.Common.Enum.CropEnums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.DTOs.CropDTOs
{
    public class CropUpdateDto
    {
        [Required(ErrorMessage = "CropId là bắt buộc")]
        public Guid CropId { get; set; }

        // CropCode không cho phép edit, chỉ để hiển thị
        [StringLength(20, ErrorMessage = "CropCode không được vượt quá 20 ký tự")]
        public string CropCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Địa chỉ trang trại là bắt buộc")]
        [StringLength(200, MinimumLength = 10, ErrorMessage = "Địa chỉ phải từ 10-200 ký tự")]
        public string Address { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tên trang trại là bắt buộc")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "Tên trang trại phải từ 3-100 ký tự")]
        [RegularExpression(@"^[a-zA-ZÀ-ỹ0-9\s\-_.,()]+$", ErrorMessage = "Tên trang trại chỉ được chứa chữ cái, số, dấu cách và các ký tự: - _ . , ( )")]
        public string FarmName { get; set; } = string.Empty;

        [Range(0.01, 10000, ErrorMessage = "Diện tích phải từ 0.01-10,000 ha")]
        public decimal? CropArea { get; set; }

        // Status auto transition theo workflow, không cho edit thủ công
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public CropStatus Status { get; set; } = CropStatus.Inactive;

        [StringLength(1000, ErrorMessage = "Ghi chú không được vượt quá 1000 ký tự")]
        public string? Note { get; set; }
        public bool? IsApproved { get; set; }
        public string? RejectReason { get; set; }
    }
}

