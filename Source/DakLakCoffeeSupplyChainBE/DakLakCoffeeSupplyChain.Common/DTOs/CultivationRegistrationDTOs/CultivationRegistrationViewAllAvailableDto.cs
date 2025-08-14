using DakLakCoffeeSupplyChain.Common.Enum.CultivationRegistrationEnums;
using DakLakCoffeeSupplyChain.Common.Enum.FarmingCommitmentEnums;
using System.Text.Json.Serialization;

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
        public string Note { get; set; } = string.Empty;

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public CultivationRegistrationStatus Status { get; set; } = CultivationRegistrationStatus.Unknown;

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public FarmingCommitmentStatus CommitmentStatus { get; set; } = FarmingCommitmentStatus.Unknown;

        public Guid? CommitmentId { get; set; }

        public ICollection<CultivationRegistrationViewDetailsDto> CultivationRegistrationDetails { get; set; } = [];
    }
}
