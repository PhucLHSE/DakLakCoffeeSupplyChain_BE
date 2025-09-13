namespace DakLakCoffeeSupplyChain.Common.DTOs.RegionsDTOs
{
    public class RegionDto
    {
        public string Name { get; set; } = string.Empty;
        public int Code { get; set; }
        public List<WardDto>? Wards { get; set; }
    }
}
