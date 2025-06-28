namespace DakLakCoffeeSupplyChain.Common.DTOs.FarmerDTOs
{
    public class FarmerViewDetailsDto
    {
        public Guid FarmerId { get; set; }

        public string FarmerCode { get; set; } = string.Empty;

        public Guid UserId { get; set; }
        public string FarmerName { get; set; } = string.Empty;

        public string FarmLocation { get; set; } = string.Empty;

        public double? FarmSize { get; set; }

        public string CertificationStatus { get; set; } = string.Empty;

        public string CertificationUrl { get; set; } = string.Empty;

        public bool? IsVerified { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        public bool IsDeleted { get; set; }
    }
}
