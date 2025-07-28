using DakLakCoffeeSupplyChain.Common.DTOs.BusinessManagerDTOs.ProcurementPlanViews;
using DakLakCoffeeSupplyChain.Common.DTOs.FarmingCommitmentDTOs;
using DakLakCoffeeSupplyChain.Common.Enum.ProcurementPlanEnums;
using System.Text.Json.Serialization;

namespace DakLakCoffeeSupplyChain.Common.DTOs.ProcurementPlanDTOs
{
    public class ProcurementPlanViewAllDto
    {
        public Guid PlanId { get; set; }
        public string PlanCode { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public double? TotalQuantity { get; set; }
        public BusinessManagerSummaryDto? CreatedBy { get; set; }
        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ProcurementPlanStatus Status { get; set; } = ProcurementPlanStatus.Draft;
        public double? ProgressPercentage { get; set; }
        public ICollection<FarmingCommitmentViewAllDto>? Commitments { get; set; } = [];
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
