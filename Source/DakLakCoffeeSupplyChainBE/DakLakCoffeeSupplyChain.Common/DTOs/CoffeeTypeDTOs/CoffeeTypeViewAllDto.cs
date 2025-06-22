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
    }
}
