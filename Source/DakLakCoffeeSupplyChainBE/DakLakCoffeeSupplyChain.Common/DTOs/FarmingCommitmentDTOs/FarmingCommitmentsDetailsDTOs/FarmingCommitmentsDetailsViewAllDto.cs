using DakLakCoffeeSupplyChain.Common.Enum.FarmingCommitmentEnums;
using System.Text.Json.Serialization;

namespace DakLakCoffeeSupplyChain.Common.DTOs.FarmingCommitmentDTOs.FarmingCommitmentsDetailsDTOs
{
    public class FarmingCommitmentsDetailsViewAllDto
    {
        public Guid CommitmentDetailId { get; set; }
        public string CommitmentDetailCode { get; set; } = string.Empty;
        public Guid CommitmentId { get; set; }
        public Guid RegistrationDetailId { get; set; }
        public Guid PlanDetailId { get; set; }
        public string CoffeeTypeName { get; set; } = string.Empty;
        public double? ConfirmedPrice { get; set; }
        public double? AdvancePayment { get; set; }
        public double? TaxPrice { get; set; }
        public double? CommittedQuantity { get; set; }
        public double? DeliveriedQuantity { get; set; }
        public double? ProgressPercentage { get; set; }
        public DateOnly? EstimatedDeliveryStart { get; set; }
        public DateOnly? EstimatedDeliveryEnd { get; set; }
        
        // Thông tin thời gian thu hoạch từ đăng ký canh tác
        public DateOnly? ExpectedHarvestStart { get; set; }
        public DateOnly? ExpectedHarvestEnd { get; set; }
        
        public string Note { get; set; } = string.Empty;

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public FarmingCommitmentStatus Status { get; set; } = FarmingCommitmentStatus.Unknown;
        public Guid? ContractDeliveryItemId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

    }
}
