using System.Text.Json.Serialization;

namespace DakLakCoffeeSupplyChain.Common.DTOs.ContractDTOs.SettlementDTOs
{
    public class SettlementRound
    {
        [JsonPropertyName("roundName")]
        public int RoundName { get; set; }
        [JsonPropertyName("settlementFileURL")]
        public string SettlementFileURL { get; set; } = string.Empty;
    }
}
