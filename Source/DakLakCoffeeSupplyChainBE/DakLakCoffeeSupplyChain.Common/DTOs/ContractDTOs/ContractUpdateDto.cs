using DakLakCoffeeSupplyChain.Common.DTOs.ContractDTOs.ContractItemDTOs;
using DakLakCoffeeSupplyChain.Common.Enum.ContractEnums;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;

namespace DakLakCoffeeSupplyChain.Common.DTOs.ContractDTOs
{
    public class ContractUpdateDto : IValidatableObject
    {
        [Required(ErrorMessage = "ContractId là bắt buộc.")]
        public Guid ContractId { get; set; }

        [Required(ErrorMessage = "BuyerId là bắt buộc.")]
        public Guid BuyerId { get; set; }

        [Required(ErrorMessage = "Số hợp đồng (ContractNumber) là bắt buộc.")]
        [MaxLength(100, ErrorMessage = "Số hợp đồng không được vượt quá 100 ký tự.")]
        public string ContractNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tiêu đề hợp đồng (ContractTitle) là bắt buộc.")]
        [MaxLength(255, ErrorMessage = "Tiêu đề không được vượt quá 255 ký tự.")]
        public string ContractTitle { get; set; } = string.Empty;

        [Display(Name = "File hợp đồng")]
        public IFormFile? ContractFile { get; set; }

        [StringLength(500, ErrorMessage = "URL file hợp đồng không được vượt quá 500 ký tự.")]
        public string? ContractFileUrl { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Số đợt giao hàng phải lớn hơn 0.")]
        public int? DeliveryRounds { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Tổng khối lượng phải lớn hơn hoặc bằng 0.")]
        public double? TotalQuantity { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Tổng giá trị phải lớn hơn hoặc bằng 0.")]
        public double? TotalValue { get; set; }

        [Required(ErrorMessage = "Ngày bắt đầu là bắt buộc.")]
        public DateOnly? StartDate { get; set; }

        [Required(ErrorMessage = "Ngày kết thúc là bắt buộc.")]
        public DateOnly? EndDate { get; set; }

        public DateTime? SignedAt { get; set; }

        [Required(ErrorMessage = "Trạng thái là bắt buộc.")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ContractStatus Status { get; set; } = ContractStatus.NotStarted;

        [MaxLength(1000, ErrorMessage = "Lý do hủy không được vượt quá 1000 ký tự.")]
        public string? CancelReason { get; set; }

        public List<ContractItemUpdateDto> ContractItems { get; set; } = new();

        public string ContractType { get; set; } = string.Empty;

        public Guid? ParentContractID { get; set; }

        public int PaymentRounds { get; set; }

        public ICollection<IFormFile>? SettlementFiles { get; set; }
        //[JsonPropertyName("settlementFiles")]
        //public Dictionary<int, IFormFile>? SettlementFiles { get; set; }
        public string? SettlementFileURL { get; set; }

        public string? SettlementFilesJson { get; set; }

        public string? SettlementNote { get; set; }

        // Validation nghiệp vụ
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (StartDate.HasValue && 
                EndDate.HasValue && 
                StartDate > EndDate)
            {
                yield return new ValidationResult(
                    "Ngày bắt đầu không được sau ngày kết thúc.",
                    new[] { nameof(StartDate), nameof(EndDate) }
                );
            }

            // Validation ngày ký hợp đồng phải ≤ ngày bắt đầu
            if (SignedAt.HasValue &&
                StartDate.HasValue)
            {
                var signedDate = DateOnly.FromDateTime(SignedAt.Value);

                if (signedDate > StartDate.Value)
                {
                    yield return new ValidationResult(
                        "Ngày ký hợp đồng không được sau ngày bắt đầu.",
                        new[] { nameof(SignedAt), nameof(StartDate) }
                    );
                }
            }

            if (TotalQuantity.HasValue && 
                TotalQuantity < 0)
            {
                yield return new ValidationResult(
                    "Tổng khối lượng không được âm.",
                    new[] { nameof(TotalQuantity) }
                );
            }

            if (TotalValue.HasValue && 
                TotalValue < 0)
            {
                yield return new ValidationResult(
                    "Tổng giá trị không được âm.",
                    new[] { nameof(TotalValue) }
                );
            }

            // Validation cho file upload
            if (ContractFile != null)
            {
                const long maxFileSize = 30 * 1024 * 1024; // 30MB
                if (ContractFile.Length > maxFileSize)
                {
                    yield return new ValidationResult(
                        "File hợp đồng không được vượt quá 30MB.",
                        new[] { nameof(ContractFile) }
                    );
                }

                var allowedExtensions = new[] { ".pdf", ".doc", ".docx", ".txt", ".rtf", ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".mp4", ".avi", ".mov", ".wmv", ".flv", ".webm" };
                var fileExtension = Path.GetExtension(ContractFile.FileName)?.ToLowerInvariant();
                
                if (string.IsNullOrEmpty(fileExtension) || !allowedExtensions.Contains(fileExtension))
                {
                    yield return new ValidationResult(
                        "File hợp đồng phải có định dạng: PDF, DOC, DOCX, TXT, RTF, JPG, JPEG, PNG, GIF, BMP, WEBP, MP4, AVI, MOV, WMV, FLV, WEBM.",
                        new[] { nameof(ContractFile) }
                    );
                }
            }

            if (ContractItems != null)
            {
                foreach (var item in ContractItems)
                {
                    var itemResults = item.Validate(validationContext);

                    foreach (var result in itemResults)
                    {
                        yield return result;
                    }
                }

                var duplicatedTypes = ContractItems
                    .GroupBy(ci => ci.CoffeeTypeId)
                    .Where(ci => ci.Count() > 1)
                    .Select(ci => ci.Key)
                    .ToList();

                if (duplicatedTypes.Any())
                {
                    yield return new ValidationResult(
                        "Không được có 2 dòng hợp đồng cùng loại cà phê.",
                        new[] { nameof(ContractItems) }
                    );
                }
            }
        }
    }
}
