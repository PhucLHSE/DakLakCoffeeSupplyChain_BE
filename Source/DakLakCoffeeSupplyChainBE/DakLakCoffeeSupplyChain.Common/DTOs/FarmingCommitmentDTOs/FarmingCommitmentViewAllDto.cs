using DakLakCoffeeSupplyChain.Common.DTOs.FarmingCommitmentDTOs.FarmingCommitmentsDetailsDTOs;
using DakLakCoffeeSupplyChain.Common.Enum.FarmingCommitmentEnums;
using System.Text.Json.Serialization;

namespace DakLakCoffeeSupplyChain.Common.DTOs.FarmingCommitmentDTOs
{
    public class FarmingCommitmentViewAllDto
    {
        public Guid CommitmentId { get; set; }

        public string CommitmentCode { get; set; } = string.Empty;
        public string CommitmentName { get; set; } = string.Empty;
        public Guid FarmerId { get; set; } // Thêm FarmerId
        public string FarmerName { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string PlanTitle { get; set; } = string.Empty;
        public double? TotalPrice { get; set; }
        public double? ProgressPercentage { get; set; }
        public DateTime CommitmentDate { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public FarmingCommitmentStatus Status { get; set; } = FarmingCommitmentStatus.Unknown;
        public ICollection<FarmingCommitmentsDetailsViewAllDto> farmingCommitmentDetails { get; set; } = [];
    }
}
