using DakLakCoffeeSupplyChain.Common.Enum.FarmingCommitmentEnums;

namespace DakLakCoffeeSupplyChain.Common.DTOs.FarmingCommitmentDTOs
{
    public class FarmingCommitmentViewDetailsDto
    {
        public Guid CommitmentId { get; set; }

        public string CommitmentCode { get; set; } = string.Empty;

        public string CommitmentName { get; set; } = string.Empty;

        public Guid PlanId { get; set; }
        public Guid RegistrationId { get; set; }

        public Guid FarmerId { get; set; }
        public string FarmerName { get; set; } = string.Empty;

        //public double? ConfirmedPrice { get; set; }

        //public double? CommittedQuantity { get; set; }

        //public DateOnly? EstimatedDeliveryStart { get; set; }

        //public DateOnly? EstimatedDeliveryEnd { get; set; }

        public DateTime CommitmentDate { get; set; }

        public Guid? ApprovedById { get; set; }
        public string ApprovedBy { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;

        public DateTime? ApprovedAt { get; set; }

        public FarmingCommitmentStatus Status { get; set; } = FarmingCommitmentStatus.Unknown;

        public string RejectionReason { get; set; } = string.Empty;

        public string Note { get; set; } = string.Empty;

        public Guid? ContractDeliveryItemId { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }
    }
}
