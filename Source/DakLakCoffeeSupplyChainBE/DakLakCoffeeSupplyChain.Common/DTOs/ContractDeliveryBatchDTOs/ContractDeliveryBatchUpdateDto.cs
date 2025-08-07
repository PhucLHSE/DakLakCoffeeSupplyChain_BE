using DakLakCoffeeSupplyChain.Common.DTOs.ContractDeliveryBatchDTOs.ContractDeliveryItem;
using DakLakCoffeeSupplyChain.Common.Enum.ContractDeliveryBatchEnums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.DTOs.ContractDeliveryBatchDTOs
{
    public class ContractDeliveryBatchUpdateDto : IValidatableObject
    {
        [Required(ErrorMessage = "DeliveryBatchId là bắt buộc.")]
        public Guid DeliveryBatchId { get; set; }

        [Required(ErrorMessage = "ContractId là bắt buộc.")]
        public Guid ContractId { get; set; }

        [Required(ErrorMessage = "DeliveryRound là bắt buộc.")]
        [Range(1, int.MaxValue, ErrorMessage = "DeliveryRound phải lớn hơn hoặc bằng 1.")]
        public int DeliveryRound { get; set; }

        [Required(ErrorMessage = "ExpectedDeliveryDate là bắt buộc.")]
        public DateOnly ExpectedDeliveryDate { get; set; }

        [Required(ErrorMessage = "Tổng sản lượng dự kiến là bắt buộc.")]
        [Range(1, double.MaxValue, ErrorMessage = "TotalPlannedQuantity phải lớn hơn 0.")]
        public double TotalPlannedQuantity { get; set; }

        [Required(ErrorMessage = "Trạng thái là bắt buộc.")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ContractDeliveryBatchStatus Status { get; set; }

        public List<ContractDeliveryItemUpdateDto> ContractDeliveryItems { get; set; } = new();

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (ExpectedDeliveryDate < DateOnly.FromDateTime(DateTime.Now.Date))
            {
                yield return new ValidationResult(
                    "Ngày giao dự kiến không được ở quá khứ.",
                    new[] { nameof(ExpectedDeliveryDate) }
                );
            }

            if (TotalPlannedQuantity <= 0)
            {
                yield return new ValidationResult(
                    "Sản lượng dự kiến phải lớn hơn 0.",
                    new[] { nameof(TotalPlannedQuantity) }
                );
            }

            if (ContractDeliveryItems == null ||
                ContractDeliveryItems.Count == 0)
            {
                yield return new ValidationResult(
                    "Phải có ít nhất 1 dòng sản phẩm giao hàng.",
                    new[] { nameof(ContractDeliveryItems) }
                );
            }
        }
    }
}
