namespace DakLakCoffeeSupplyChain.Common.DTOs.FarmingCommitmentDTOs.FarmingCommitmentsDetailsDTOs
{
    public class FarmingCommitmentsDetailsViewAllDto
    {
        public Guid CommitmentDetailId { get; set; }
        public string CommitmentDetailCode { get; set; } = string.Empty;
        public Guid CommitmentId { get; set; }
        public Guid RegistrationDetailId { get; set; }
        public Guid PlanDetailId { get; set; }
        public double? ConfirmedPrice { get; set; }
        public double? CommittedQuantity { get; set; }
        public DateOnly? EstimatedDeliveryStart { get; set; }
        public DateOnly? EstimatedDeliveryEnd { get; set; }
        public string Note { get; set; } = string.Empty;
        public Guid? ContractDeliveryItemId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

    }
}
