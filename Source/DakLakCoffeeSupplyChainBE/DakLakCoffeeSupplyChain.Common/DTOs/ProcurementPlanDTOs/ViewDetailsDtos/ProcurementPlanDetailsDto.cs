using DakLakCoffeeSupplyChain.Common.DTOs.ProcurementPlanDTOs.CoffeeTypeDTOs;
using DakLakCoffeeSupplyChain.Common.Enum.ProcurementPlanEnums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.DTOs.ProcurementPlanDTOs.ViewDetailsDtos
{
    public class ProcurementPlanDetailsDto
    {
        public Guid PlanDetailsId { get; set; }

        public string PlanDetailCode { get; set; } = string.Empty;

        public Guid PlanId { get; set; }

        public CoffeeTypePlanDetailsViewDto? CoffeeType { get; set; }

        //public string CropType { get; set; } = string.Empty; Có khả năng field này bị thừa, cần loại bỏ

        public double? TargetQuantity { get; set; }

        public string TargetRegion { get; set; } = string.Empty;

        public double? MinimumRegistrationQuantity { get; set; }

        public string BeanSize { get; set; } = string.Empty;

        public string BeanColor { get; set; } = string.Empty;

        public double? MoistureContent { get; set; }

        public double? DefectRate { get; set; }

        public double? MinPriceRange { get; set; }

        public double? MaxPriceRange { get; set; }

        public string Note { get; set; } = string.Empty;

        public string BeanColorImageUrl { get; set; } = string.Empty;

        public double? ProgressPercentage { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ProcurementPlanDetailsStatus Status { get; set; } = ProcurementPlanDetailsStatus.Disable;

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }
    }
}
