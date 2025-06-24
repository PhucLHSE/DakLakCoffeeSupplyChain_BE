using DakLakCoffeeSupplyChain.Common.DTOs.ContractDTOs.ContractItemDTOs;
using DakLakCoffeeSupplyChain.Common.Enum.ContractEnums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.DTOs.ContractDTOs
{
    public class ContractCreateDto : IValidatableObject
    {
        [Required(ErrorMessage = "BuyerId là bắt buộc.")]
        public Guid BuyerId { get; set; }

        [Required(ErrorMessage = "Số hợp đồng (ContractNumber) là bắt buộc.")]
        [MaxLength(100, ErrorMessage = "Số hợp đồng không được vượt quá 100 ký tự.")]
        public string ContractNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tiêu đề hợp đồng (ContractTitle) là bắt buộc.")]
        [MaxLength(255, ErrorMessage = "Tiêu đề không được vượt quá 255 ký tự.")]
        public string ContractTitle { get; set; } = string.Empty;

        [MaxLength(255, ErrorMessage = "URL file hợp đồng không được vượt quá 255 ký tự.")]
        [Url(ErrorMessage = "ContractFileUrl phải là một URL hợp lệ.")]
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
        public ContractStatus Status { get; set; } = ContractStatus.NotStarted;

        [MaxLength(1000, ErrorMessage = "Lý do hủy không được vượt quá 1000 ký tự.")]
        public string CancelReason { get; set; } = string.Empty;

        public List<ContractItemCreateDto> ContractItems { get; set; } = new();

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (StartDate.HasValue && EndDate.HasValue && StartDate > EndDate)
            {
                yield return new ValidationResult(
                    "Ngày bắt đầu không được sau ngày kết thúc.",
                    new[] { nameof(StartDate), nameof(EndDate) }
                );
            }

            if (TotalQuantity.HasValue && TotalQuantity < 0)
            {
                yield return new ValidationResult(
                    "Tổng khối lượng không được âm.",
                    new[] { nameof(TotalQuantity) }
                );
            }

            if (TotalValue.HasValue && TotalValue < 0)
            {
                yield return new ValidationResult(
                    "Tổng giá trị không được âm.",
                    new[] { nameof(TotalValue) }
                );
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

                // Kiểm tra trùng CoffeeType trong danh sách ContractItems
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
