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

        [Required(ErrorMessage = "CropCode là bắt buộc")]
        [StringLength(20, ErrorMessage = "CropCode không được vượt quá 20 ký tự")]
        public string CropCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Address là bắt buộc")]
        [StringLength(500, ErrorMessage = "Address không được vượt quá 500 ký tự")]
        public string Address { get; set; } = string.Empty;

        [Required(ErrorMessage = "FarmName là bắt buộc")]
        [StringLength(200, ErrorMessage = "FarmName không được vượt quá 200 ký tự")]
        public string FarmName { get; set; } = string.Empty;

        [Range(0.01, double.MaxValue, ErrorMessage = "CropArea phải lớn hơn 0")]
        public decimal? CropArea { get; set; }

        // Status auto transition theo workflow, không cho edit thủ công
        [Required(ErrorMessage = "Trạng thái là bắt buộc.")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public CropStatus Status { get; set; } = CropStatus.Inactive;

        [StringLength(1000, ErrorMessage = "Ghi chú không được vượt quá 1000 ký tự")]
        public string? Note { get; set; }
    }
}

