namespace DakLakCoffeeSupplyChain.Common.DTOs.FarmerDTOs
{
    public class FarmerViewAllDto
    {
        public Guid FarmerId { get; set; }

        public string FarmerCode { get; set; } = string.Empty;

        public Guid UserId { get; set; }

        public string FarmLocation { get; set; } = string.Empty;
        public string FarmerName { get; set;} = string.Empty;
    }
}
