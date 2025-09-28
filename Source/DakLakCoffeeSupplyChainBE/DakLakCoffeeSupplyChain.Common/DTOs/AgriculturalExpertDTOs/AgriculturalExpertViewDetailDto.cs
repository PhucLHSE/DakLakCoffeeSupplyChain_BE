using System;

namespace DakLakCoffeeSupplyChain.Common.DTOs.AgriculturalExpertDTOs
{
    public class AgriculturalExpertViewDetailDto
    {
        public Guid ExpertId { get; set; }

        public string ExpertCode { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string PhoneNumber { get; set; } = string.Empty;

        public string ExpertiseArea { get; set; } = string.Empty;

        public string Qualifications { get; set; } = string.Empty;

        public int? YearsOfExperience { get; set; }

        public string AffiliatedOrganization { get; set; } = string.Empty;

        public string Bio { get; set; } = string.Empty;

        public double? Rating { get; set; }

        public bool? IsVerified { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }
    }
}
