using DakLakCoffeeSupplyChain.Common.Enum.FarmingCommitmentEnums;

namespace DakLakCoffeeSupplyChain.Common.DTOs.FarmingCommitmentDTOs
{
    public class FarmingCommitmentViewAllDto
    {
        public Guid CommitmentId { get; set; }

        public string CommitmentCode { get; set; } = string.Empty;

        public string CommitmentName { get; set; } = string.Empty;

        public string FarmerName { get; set; } = string.Empty;
        public string PlanTitle { get; set; } = string.Empty;
        public DateTime CommitmentDate { get; set; }
        public FarmingCommitmentStatus Status { get; set; } = FarmingCommitmentStatus.Unknown;
    }
}
