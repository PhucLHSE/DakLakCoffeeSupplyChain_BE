namespace DakLakCoffeeSupplyChain.Common.DTOs.CultivationRegistrationDTOs
{
    public class CultivationRegistrationViewAllAvailableDto
    {
        public Guid RegistrationId { get; set; }

        public string RegistrationCode { get; set; } = string.Empty;

        public Guid PlanId { get; set; }

        public Guid FarmerId { get; set; }
        public string FarmerName { get; set; } = string.Empty;
        public string FarmerAvatarURL { get; set; } = string.Empty;
        public string FarmerLicencesURL { get; set; } = string.Empty;
        public string FarmerLocation { get; set; } = string.Empty;

        public double? RegisteredArea { get; set; }

        public DateTime RegisteredAt { get; set; }

        public double? TotalWantedPrice { get; set; }

        public ICollection<CultivationRegistrationViewDetailsDto> CultivationRegistrationDetails { get; set; } = [];
    }
}
