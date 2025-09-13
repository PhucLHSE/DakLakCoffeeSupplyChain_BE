namespace DakLakCoffeeSupplyChain.Common.DTOs.RegionsDTOs
{
    public class WardDto
    {
        public string Name { get; set; } = string.Empty;
        public int Code { get; set; }
        public string? Division_type { get; set; }
        public string? Codename { get; set; }
        public int Province_code { get; set; }
    }
}
