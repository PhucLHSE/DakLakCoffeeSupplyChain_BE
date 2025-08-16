using System.ComponentModel.DataAnnotations;

namespace DakLakCoffeeSupplyChain.Common.DTOs.ProcurementPlanDTOs.ViewDetailsDtos
{
    public class ProcurementPlanDetailsCreateDto : IValidatableObject
    {
        [Required(ErrorMessage = "Loại cà phê không được để trống.")]
        public Guid CoffeeTypeId { get; set; }

        public int? ProcessMethodId { get; set; }

        [Required(ErrorMessage = "Sản lượng mong muốn không được để trống.")]
        public double TargetQuantity { get; set; }
        public string? TargetRegion { get; set; }

        [Required(ErrorMessage = "Sản lượng tối thiểu không được để trống.")]
        [Range(100, double.MaxValue, ErrorMessage = "Sản lượng tối thiểu phải từ 100kg trở lên.")]
        public double MinimumRegistrationQuantity { get; set; }

        [Required(ErrorMessage = "Giá thương lượng tối thiểu không được để trống.")]
        public double MinPriceRange { get; set; }

        [Required(ErrorMessage = "Giá thương lượng tối đa không được để trống.")]
        public double MaxPriceRange { get; set; }
        public double? ExpectedYieldPerHectare { get; set; }

        public string Note { get; set; } = string.Empty;

        //[JsonConverter(typeof(JsonStringEnumConverter))]
        //public ProcurementPlanDetailsStatus Status { get; set; } = ProcurementPlanDetailsStatus.Disable;
        public Guid? ContractItemId { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (MinimumRegistrationQuantity > TargetQuantity)
                yield return new ValidationResult(
                        "Sản lượng tối thiểu không thể lớn hơn sản lượng mong muốn",
                        [nameof(MinimumRegistrationQuantity)]);

            if (MinPriceRange > MaxPriceRange)
                yield return new ValidationResult(
                        "Số tiền thương lượng tối thiểu không thể lớn hơn số tiền thương lượng tối đa",
                        [nameof(MinPriceRange)]);
        }
    }
}
