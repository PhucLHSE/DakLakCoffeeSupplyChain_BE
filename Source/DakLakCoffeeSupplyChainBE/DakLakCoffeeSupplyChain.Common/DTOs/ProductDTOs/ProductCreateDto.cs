using DakLakCoffeeSupplyChain.Common.Enum.ProductEnums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.DTOs.ProductDTOs
{
    public class ProductCreateDto : IValidatableObject
    {
        [Required(ErrorMessage = "Tên sản phẩm là bắt buộc.")]
        [StringLength(100, ErrorMessage = "Tên sản phẩm không được vượt quá 100 ký tự.")]
        public string ProductName { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Mô tả sản phẩm không được vượt quá 500 ký tự.")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Giá bán là bắt buộc.")]
        [Range(0, double.MaxValue, ErrorMessage = "Giá bán phải lớn hơn hoặc bằng 0.")]
        public double UnitPrice { get; set; }

        [Required(ErrorMessage = "Số lượng là bắt buộc.")]
        [Range(0, double.MaxValue, ErrorMessage = "Số lượng phải lớn hơn hoặc bằng 0.")]
        public double QuantityAvailable { get; set; }

        [Required(ErrorMessage = "Đơn vị tính là bắt buộc.")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        [EnumDataType(typeof(ProductUnit), ErrorMessage = "Đơn vị tính không hợp lệ (Kg, Ta, Tan).")]
        public ProductUnit Unit { get; set; }

        [Required(ErrorMessage = "Mã mẻ sơ chế là bắt buộc.")]
        public Guid BatchId { get; set; }

        [Required(ErrorMessage = "Mã kho là bắt buộc.")]
        public Guid InventoryId { get; set; }

        [Required(ErrorMessage = "Loại cà phê là bắt buộc.")]
        public Guid CoffeeTypeId { get; set; }

        [StringLength(100, ErrorMessage = "Vùng sản xuất không được vượt quá 100 ký tự.")]
        public string OriginRegion { get; set; } = string.Empty;

        [StringLength(200, ErrorMessage = "Vị trí nông trại không được vượt quá 200 ký tự.")]
        public string OriginFarmLocation { get; set; } = string.Empty;

        [StringLength(50, ErrorMessage = "Mã chỉ dẫn địa lý không được vượt quá 50 ký tự.")]
        public string GeographicalIndicationCode { get; set; } = string.Empty;

        [Url(ErrorMessage = "Đường dẫn chứng nhận không hợp lệ.")]
        public string? CertificationUrl { get; set; }

        [StringLength(50, ErrorMessage = "Chất lượng đánh giá không được vượt quá 50 ký tự.")]
        public string EvaluatedQuality { get; set; } = string.Empty;

        [Range(0, 100, ErrorMessage = "Điểm đánh giá phải trong khoảng từ 0 đến 100.")]
        public double? EvaluationScore { get; set; }

        [Required(ErrorMessage = "Trạng thái sản phẩm là bắt buộc.")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ProductStatus Status { get; set; } = ProductStatus.Pending;

        [StringLength(50, ErrorMessage = "Ghi chú duyệt không được vượt quá 50 ký tự.")]
        public string ApprovalNote { get; set; } = string.Empty;

        // Optional fields (do system control)
        [JsonIgnore]
        public Guid? ApprovedBy { get; set; }

        [JsonIgnore]
        public DateTime? ApprovedAt { get; set; }

        // Validation nghiệp vụ
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!string.IsNullOrEmpty(CertificationUrl) && 
                string.IsNullOrWhiteSpace(EvaluatedQuality))
            {
                yield return new ValidationResult(
                    "Nếu có chứng nhận, bạn cần cung cấp chất lượng đánh giá.",
                    new[] { nameof(EvaluatedQuality) }
                );
            }
        }
    }
}
