using DakLakCoffeeSupplyChain.Common.Enum.CoffeeTypeEnums;
using System.Text.Json.Serialization;

namespace DakLakCoffeeSupplyChain.Common.DTOs.CoffeeTypeDTOs
{
    public class CoffeeTypeViewAllDto
    {
        public Guid CoffeeTypeId { get; set; }

        public string TypeCode { get; set; } = string.Empty;

        public string TypeName { get; set; } = string.Empty;

        public string BotanicalName { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string TypicalRegion { get; set; } = string.Empty;

        public string SpecialtyLevel { get; set; } = string.Empty;

        public string CoffeeTypeCategory { get; set; } = string.Empty;

        public Guid? CoffeeTypeParentId { get; set; }
        public string? CoffeeTypeParentName { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public CoffeeTypeStatus Status { get; set; } = CoffeeTypeStatus.Unknown;
    }
}
