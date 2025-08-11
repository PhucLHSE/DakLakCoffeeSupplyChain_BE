using DakLakCoffeeSupplyChain.Common.Enum.FarmingCommitmentEnums;
using System.Text.Json.Serialization;

namespace DakLakCoffeeSupplyChain.Common.DTOs.FarmingCommitmentDTOs.FarmingCommitmentsDetailsDTOs
{
    public class FarmingCommitmentsDetailsUpdateStatusDto
    {
        public string RejectReason { get; set; } = string.Empty;

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public FarmingCommitmentStatus Status { get; set; } = FarmingCommitmentStatus.Unknown;

    }
}
