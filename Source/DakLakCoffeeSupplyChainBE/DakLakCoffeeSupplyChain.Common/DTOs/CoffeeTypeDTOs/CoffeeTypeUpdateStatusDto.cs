using DakLakCoffeeSupplyChain.Common.Enum.CoffeeTypeEnums;
using System.Text.Json.Serialization;

namespace DakLakCoffeeSupplyChain.Common.DTOs.CoffeeTypeDTOs
{
    public class CoffeeTypeUpdateStatusDto
    {
        public Guid CoffeeTypeId { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public CoffeeTypeStatus Status { get; set; } = CoffeeTypeStatus.Unknown;
    }
}
