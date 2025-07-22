using DakLakCoffeeSupplyChain.Common.Enum.ProcurementPlanEnums;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DakLakCoffeeSupplyChain.Common.DTOs.ProcurementPlanDTOs.ViewDetailsDtos
{
    public class ProcurementPlanDetailsUpdateDto
    {
        [Required(ErrorMessage = "ID Chi tiết kế hoạch không xác định")]
        public Guid PlanDetailsId { get; set; }

        //public string PlanDetailCode { get; set; }

        //public Guid PlanId { get; set; }
        public Guid CoffeeTypeId { get; set; }

        public int ProcessMethodId { get; set; }

        public double? TargetQuantity { get; set; }

        public string TargetRegion { get; set; } = string.Empty;

        public double? MinimumRegistrationQuantity { get; set; }

        public double? MinPriceRange { get; set; }

        public double? MaxPriceRange { get; set; }

        public string Note { get; set; } = string.Empty;

        public Guid? ContractItemId { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ProcurementPlanDetailsStatus Status { get; set; } = ProcurementPlanDetailsStatus.Unknown;

        //public DateTime CreatedAt { get; set; }

        //public DateTime UpdatedAt { get; set; }

        //public bool IsDeleted { get; set; }
    }
}
