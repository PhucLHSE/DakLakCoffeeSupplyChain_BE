using DakLakCoffeeSupplyChain.Common.Enum.FarmingCommitmentEnums;
using System.Text.Json.Serialization;

namespace DakLakCoffeeSupplyChain.Common.DTOs.FarmingCommitmentDTOs
{
    public class FarmingCommitmentUpdateStatusDto
    {
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public FarmingCommitmentStatus Status { get; set; } = FarmingCommitmentStatus.Unknown;
        public string RejectReason { get; set; } = string.Empty;
    }
}
