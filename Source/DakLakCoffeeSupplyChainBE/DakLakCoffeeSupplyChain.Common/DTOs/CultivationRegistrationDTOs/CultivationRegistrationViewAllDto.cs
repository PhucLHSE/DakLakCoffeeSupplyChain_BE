namespace DakLakCoffeeSupplyChain.Common.DTOs.CultivationRegistrationDTOs
{
    public class CultivationRegistrationViewAllDto
    {
        public Guid RegistrationId { get; set; }

        public string RegistrationCode { get; set; } = string.Empty;

        public Guid PlanId { get; set; }

        public Guid FarmerId { get; set; }
        public string FarmerName { get; set; } = string.Empty;

        public double? RegisteredArea { get; set; }

        public DateTime RegisteredAt { get; set; }

        public double? WantedPrice { get; set; }

    }
}
