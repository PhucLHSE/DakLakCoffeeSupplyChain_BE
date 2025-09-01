using DakLakCoffeeSupplyChain.Common.Enum.CultivationRegistrationEnums;
using System.Text.Json.Serialization;

namespace DakLakCoffeeSupplyChain.Common.DTOs.CultivationRegistrationDTOs
{
    public class CultivationRegistrationViewSumaryDto
    {
        public Guid RegistrationId { get; set; }

        public string RegistrationCode { get; set; } = string.Empty;

        public Guid PlanId { get; set; }

        public Guid FarmerId { get; set; }
        public string FarmerName { get; set; } = string.Empty;

        public double? RegisteredArea { get; set; }

        public DateTime RegisteredAt { get; set; }

        public double? TotalWantedPrice { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public CultivationRegistrationStatus Status { get; set; }

        public string Note { get; set; } = string.Empty;
        public ICollection<CultivationRegistrationViewDetailsDto>? CultivationRegistrationDetails { get; set; }
    }
}
