using DakLakCoffeeSupplyChain.Common.DTOs.CultivationRegistrationDTOs;
using DakLakCoffeeSupplyChain.Common.Enum.CultivationRegistrationEnums;
using DakLakCoffeeSupplyChain.Common.Helpers;
using DakLakCoffeeSupplyChain.Repositories.Models;

namespace DakLakCoffeeSupplyChain.Services.Mappers
{
    public static class CultivationRegistrationMapper
    {
        // Mapper CultivationRegistrationViewAllDto
        public static CultivationRegistrationViewAllDto MapToCultivationRegistrationViewAllDto(this CultivationRegistration cultivation)
        {
            return new CultivationRegistrationViewAllDto
            {
                RegistrationId = cultivation.RegistrationId,
                RegistrationCode = cultivation.RegistrationCode,
                PlanId = cultivation.PlanId,
                FarmerId = cultivation.FarmerId,
                FarmerName = cultivation.Farmer.User.Name,
                RegisteredArea = cultivation.RegisteredArea,
                RegisteredAt = cultivation.RegisteredAt,
                TotalWantedPrice = cultivation.TotalWantedPrice,
            };
        }
        public static CultivationRegistrationViewAllAvailableDto MapToCultivationRegistrationViewAllAvailableDto(this CultivationRegistration cultivation)
        {
            return new CultivationRegistrationViewAllAvailableDto
            {
                RegistrationId = cultivation.RegistrationId,
                RegistrationCode = cultivation.RegistrationCode,
                PlanId = cultivation.PlanId,
                FarmerId = cultivation.FarmerId,
                FarmerName = cultivation.Farmer.User.Name,
                FarmerAvatarURL = cultivation.Farmer.User.ProfilePictureUrl,
                FarmerLicencesURL = cultivation.Farmer.CertificationUrl,
                FarmerLocation = cultivation.Farmer.FarmLocation,
                RegisteredArea = cultivation.RegisteredArea,
                RegisteredAt = cultivation.RegisteredAt,
                TotalWantedPrice = cultivation.TotalWantedPrice,
                CultivationRegistrationViewDetailsDtos = [.. cultivation.CultivationRegistrationsDetails.Select(detail => new CultivationRegistrationViewDetailsDto
                {
                    CultivationRegistrationDetailId = detail.CultivationRegistrationDetailId,
                    RegistrationId = detail.RegistrationId,
                    PlanDetailId = detail.PlanDetailId,
                    CoffeeType = detail.PlanDetail.CoffeeType.TypeName,
                    EstimatedYield = detail.EstimatedYield,
                    WantedPrice = detail.WantedPrice,
                    ExpectedHarvestStart = detail.ExpectedHarvestStart,
                    ExpectedHarvestEnd = detail.ExpectedHarvestEnd,
                    Status = EnumHelper.ParseEnumFromString(detail.Status, CultivationRegistrationStatus.Unknown),
                    Note = detail.Note
                })]
            };
        }

        // Mapper CultivationRegistrationViewSumaryDto
        public static CultivationRegistrationViewSumaryDto MapToCultivationRegistrationViewSumaryDto(this CultivationRegistration entity)
        {
            return new CultivationRegistrationViewSumaryDto
            {
                RegistrationId = entity.RegistrationId,
                RegistrationCode = entity.RegistrationCode,
                PlanId = entity.PlanId,
                FarmerId = entity.FarmerId,
                FarmerName = entity.Farmer.User.Name,
                RegisteredArea = entity.RegisteredArea,
                RegisteredAt = entity.RegisteredAt,
                TotalWantedPrice = entity.TotalWantedPrice,
                Status = EnumHelper.ParseEnumFromString(entity.Status, CultivationRegistrationStatus.Unknown),
                Note = entity.Note,
                CultivationRegistrationDetails = entity.CultivationRegistrationsDetails?
                .OrderBy(c => c.CreatedAt)
                .Select(c => new CultivationRegistrationViewDetailsDto
                {
                    CultivationRegistrationDetailId = c.CultivationRegistrationDetailId,
                    RegistrationId = c.RegistrationId,
                    PlanDetailId = c.PlanDetailId,
                    CoffeeType = c.PlanDetail.CoffeeType.TypeName,
                    EstimatedYield = c.EstimatedYield,
                    WantedPrice = c.WantedPrice,
                    ExpectedHarvestStart = c.ExpectedHarvestStart,
                    ExpectedHarvestEnd = c.ExpectedHarvestEnd,
                    Status = EnumHelper.ParseEnumFromString(c.Status, CultivationRegistrationStatus.Unknown),
                    Note = c.Note
                }).ToList() ?? []
            };
        }

        // Mapper CultivationRegistrationCreateDto
        public static CultivationRegistration MapToCultivationRegistrationCreateDto(this CultivationRegistrationCreateViewDto dto, string registrationCode, Guid farmerId)
        {
            return new CultivationRegistration
            {
                RegistrationId = Guid.NewGuid(),
                RegistrationCode = registrationCode,
                PlanId = dto.PlanId,
                FarmerId = farmerId,
                RegisteredArea = dto.RegisteredArea,
                TotalWantedPrice = 0, // Vì sql không có khai báo mặc định nên buộc phải khai báo ở đây
                Status = CultivationRegistrationStatus.Pending.ToString(), // Mặc định gán vào luôn, farmer không có lựa chọn
                Note = dto.Note,
                CultivationRegistrationsDetails = [.. dto.CultivationRegistrationDetailsCreateViewDto
                .Select(detail => new CultivationRegistrationsDetail
                {
                    CultivationRegistrationDetailId = Guid.NewGuid(),
                    PlanDetailId = detail.PlanDetailId,
                    EstimatedYield = detail.EstimatedYield,
                    WantedPrice = detail.WantedPrice,
                    ExpectedHarvestStart = detail.ExpectedHarvestStart,
                    ExpectedHarvestEnd = detail.ExpectedHarvestEnd,
                    Note = detail.Note,
                    Status = CultivationRegistrationStatus.Pending.ToString()
                })]
            };
        }
    }
}
