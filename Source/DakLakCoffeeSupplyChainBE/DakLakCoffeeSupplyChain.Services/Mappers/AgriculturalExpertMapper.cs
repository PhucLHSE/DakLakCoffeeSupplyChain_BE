using DakLakCoffeeSupplyChain.Common.DTOs.AgriculturalExpertDTOs;
using DakLakCoffeeSupplyChain.Repositories.Models;
using DakLakCoffeeSupplyChain.Common.Helpers;

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

        // Mapper cho tạo mới chuyên gia
        public static AgriculturalExpert MapToNewAgriculturalExpert(
            this AgriculturalExpertCreateDto dto, 
            Guid userId, 
            string expertCode)
        {
            return new AgriculturalExpert
            {
                ExpertId = Guid.NewGuid(),
                UserId = userId,
                ExpertCode = expertCode,
                ExpertiseArea = dto.ExpertiseArea,
                Qualifications = dto.Qualifications,
                YearsOfExperience = dto.YearsOfExperience,
                AffiliatedOrganization = dto.AffiliatedOrganization,
                Bio = dto.Bio,
                Rating = dto.Rating,
                IsVerified = false,                    // Mặc định khi đăng ký là false
                CreatedAt = DateHelper.NowVietnamTime(),
                UpdatedAt = DateHelper.NowVietnamTime(),
                IsDeleted = false
            };
        }

        // Mapper cho cập nhật chuyên gia
        public static void MapToUpdateAgriculturalExpert(
            this AgriculturalExpertUpdateDto dto, 
            AgriculturalExpert agriculturalExpert)
        {
            agriculturalExpert.ExpertiseArea = dto.ExpertiseArea;
            agriculturalExpert.Qualifications = dto.Qualifications;
            agriculturalExpert.YearsOfExperience = dto.YearsOfExperience;
            agriculturalExpert.AffiliatedOrganization = dto.AffiliatedOrganization;
            agriculturalExpert.Bio = dto.Bio;
            agriculturalExpert.Rating = dto.Rating;
            agriculturalExpert.IsVerified = dto.IsVerified;
            agriculturalExpert.UpdatedAt = DateHelper.NowVietnamTime();
        }
    }
}
