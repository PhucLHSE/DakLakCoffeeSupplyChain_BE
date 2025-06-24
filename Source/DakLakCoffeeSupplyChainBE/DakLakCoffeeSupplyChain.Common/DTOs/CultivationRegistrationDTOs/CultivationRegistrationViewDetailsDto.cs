using DakLakCoffeeSupplyChain.Common.Enum.CultivationRegistrationEnums;

namespace DakLakCoffeeSupplyChain.Common.DTOs.CultivationRegistrationDTOs
{
    public class CultivationRegistrationViewDetailsDto
    {
        public Guid CultivationRegistrationDetailId { get; set; }

        public Guid RegistrationId { get; set; }

        public Guid PlanDetailId { get; set; }

        public double? EstimatedYield { get; set; }

        public DateOnly? ExpectedHarvestStart { get; set; }

        public DateOnly? ExpectedHarvestEnd { get; set; }

        public CultivationRegistrationStatus Status { get; set; }

        public string Note { get; set; } = string.Empty;
    }
}
