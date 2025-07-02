using DakLakCoffeeSupplyChain.Common.DTOs.AgriculturalExpertDTOs;
using DakLakCoffeeSupplyChain.Repositories.Models;

namespace DakLakCoffeeSupplyChain.Services.Mappers
{
    public static class AgriculturalExpertMapper
    {
        // Mapper cho danh sách chuyên gia (ViewAll)
        public static AgriculturalExpertViewAllDto MapToViewAllDto(this AgriculturalExpert expert)
        {
            return new AgriculturalExpertViewAllDto
            {
                ExpertId = expert.ExpertId,
                ExpertCode = expert.ExpertCode,
                FullName = expert.User?.Name ?? "N/A",
                Email = expert.User?.Email ?? string.Empty,
                ExpertiseArea = expert.ExpertiseArea,
                YearsOfExperience = expert.YearsOfExperience,
                AffiliatedOrganization = expert.AffiliatedOrganization,
                Rating = expert.Rating,
                IsVerified = expert.IsVerified
            };
        }

        // Mapper cho xem chi tiết chuyên gia (ViewDetail)
        public static AgriculturalExpertViewDetailDto MapToViewDetailDto(this AgriculturalExpert expert)
        {
            return new AgriculturalExpertViewDetailDto
            {
                ExpertId = expert.ExpertId,
                ExpertCode = expert.ExpertCode,
                FullName = expert.User?.Name ?? "N/A",
                Email = expert.User?.Email ?? string.Empty,
                PhoneNumber = expert.User?.PhoneNumber ?? string.Empty,
                ExpertiseArea = expert.ExpertiseArea,
                Qualifications = expert.Qualifications,
                YearsOfExperience = expert.YearsOfExperience,
                AffiliatedOrganization = expert.AffiliatedOrganization,
                Bio = expert.Bio,
                Rating = expert.Rating,
                IsVerified = expert.IsVerified,
                CreatedAt = expert.CreatedAt,
                UpdatedAt = expert.UpdatedAt
            };
        }
    }
}
