using System.Text.Json.Serialization;

namespace DakLakCoffeeSupplyChain.Common.DTOs.RegionsDTOs
{
    public class TargetRegionWrapper
    {
        [JsonPropertyName("targetRegions")]
        public List<string> TargetRegions { get; set; } = [];
    }
}
