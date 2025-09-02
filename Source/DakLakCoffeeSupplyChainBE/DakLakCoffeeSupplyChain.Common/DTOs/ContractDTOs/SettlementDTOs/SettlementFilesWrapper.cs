using System.Text.Json.Serialization;

namespace DakLakCoffeeSupplyChain.Common.DTOs.ContractDTOs.SettlementDTOs
{
    public class SettlementFilesWrapper
    {
        [JsonPropertyName("settlementRounds")]
        public List<SettlementRound> SettlementRounds { get; set; } = new();
    }
}
