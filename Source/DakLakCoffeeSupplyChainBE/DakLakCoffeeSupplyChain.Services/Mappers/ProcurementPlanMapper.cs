using DakLakCoffeeSupplyChain.Common.DTOs.BusinessManagerDTOs.ProcurementPlanViews;
using DakLakCoffeeSupplyChain.Common.DTOs.ProcurementPlanDTOs;
using DakLakCoffeeSupplyChain.Common.DTOs.ProcurementPlanDTOs.ViewDetailsDtos;
using DakLakCoffeeSupplyChain.Common.DTOs.UserAccountDTOs;
using DakLakCoffeeSupplyChain.Common.Enum.ProcurementPlanEnums;
using DakLakCoffeeSupplyChain.Common.Enum.UserAccountEnums;
using DakLakCoffeeSupplyChain.Common.Helpers;
using DakLakCoffeeSupplyChain.Repositories.Models;

namespace DakLakCoffeeSupplyChain.Services.Mappers
{
    public static class ProcurementPlanMapper
    {
        // Mapper ProcurementPlanViewAllDto
        public static ProcurementPlanViewAllDto MapToProcurementPlanViewAllDto(this ProcurementPlan entity)
        {
            // Parse Status string to enum
            ProcurementPlanStatus status = Enum.TryParse<ProcurementPlanStatus>(entity.Status, true, out var parsedStatus)
                ? parsedStatus
                : ProcurementPlanStatus.Draft;

            return new ProcurementPlanViewAllDto
            {
                PlanId = entity.PlanId,
                PlanCode = entity.PlanCode ?? string.Empty,
                Title = entity.Title ?? string.Empty,
                Description = entity.Description ?? string.Empty,
                TotalQuantity = entity.TotalQuantity,
                CreatedBy = entity.CreatedByNavigation == null ? null : new BusinessManagerSummaryDto
                {
                    ManagerId = entity.CreatedByNavigation.ManagerId,
                    UserId = entity.CreatedByNavigation.UserId,
                    ManagerCode = entity.CreatedByNavigation.ManagerCode,
                    CompanyName = entity.CreatedByNavigation.CompanyName,
                    CompanyAddress = entity.CreatedByNavigation.CompanyAddress,
                    Website = entity.CreatedByNavigation.Website,
                    ContactEmail = entity.CreatedByNavigation.ContactEmail
                },
                StartDate = entity.StartDate,
                EndDate = entity.EndDate,
                Status = status,
                ProgressPercentage = entity.ProgressPercentage,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt
            };
        }
        // Mapper ProcurementPlanViewDetailsDto
        public static ProcurementPlanViewDetailsSumaryDto MapToProcurementPlanViewDetailsDto(this ProcurementPlan entity)
        {
            return new ProcurementPlanViewDetailsSumaryDto
            {
                PlanId = entity.PlanId,
                PlanCode = entity.PlanCode ?? string.Empty,
                Title = entity.Title ?? string.Empty,
                Description = entity.Description ?? string.Empty,
                TotalQuantity = entity.TotalQuantity,
                CreatedBy = entity.CreatedByNavigation == null ? null : new BusinessManagerSummaryDto
                {
                    ManagerId = entity.CreatedByNavigation.ManagerId,
                    UserId = entity.CreatedByNavigation.UserId,
                    ManagerCode = entity.CreatedByNavigation.ManagerCode,
                    CompanyName = entity.CreatedByNavigation.CompanyName,
                    CompanyAddress = entity.CreatedByNavigation.CompanyAddress,
                    Website = entity.CreatedByNavigation.Website,
                    ContactEmail = entity.CreatedByNavigation.ContactEmail
                },
                StartDate = entity.StartDate,
                EndDate = entity.EndDate,
                Status = EnumHelper.ParseEnumFromString(entity.Status, ProcurementPlanStatus.Draft),
                ProgressPercentage = entity.ProgressPercentage,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt,
                ProcurementPlansDetails = entity.ProcurementPlansDetails?
                .OrderBy(p => p.PlanDetailCode)
                .Select(p => new ProcurementPlanDetailsDto
                {
                    PlanDetailsId = p.PlanDetailsId,
                    PlanDetailCode = p.PlanDetailCode,
                    PlanId = p.PlanId,
                    CoffeeType = p.CoffeeType == null ? null : new Common.DTOs.ProcurementPlanDTOs.CoffeeTypeDTOs.CoffeeTypePlanDetailsViewDto
                    {
                        CoffeeTypeId = p.CoffeeTypeId,
                        TypeCode = p.CoffeeType.TypeCode,
                        TypeName = p.CoffeeType.TypeName,
                        BotanicalName = p.CoffeeType.BotanicalName,
                        Description = p.CoffeeType.Description,
                        TypicalRegion = p.CoffeeType.TypicalRegion,
                        SpecialtyLevel = p.CoffeeType.SpecialtyLevel
                    },
                    //CropType = p.CropType, //Có khả năng field này bị thừa
                    TargetQuantity = p.TargetQuantity,
                    TargetRegion = p.TargetRegion,
                    MinimumRegistrationQuantity = p.MinimumRegistrationQuantity,
                    BeanSize = p.BeanSize,
                    BeanColor = p.BeanColor,
                    MoistureContent = p.MoistureContent,
                    DefectRate = p.DefectRate,
                    MinPriceRange = p.MinPriceRange,
                    MaxPriceRange = p.MaxPriceRange,
                    Note = p.Note,
                    BeanColorImageUrl = p.BeanColorImageUrl,
                    ProgressPercentage = p.ProgressPercentage,
                    Status = EnumHelper.ParseEnumFromString(p.Status, ProcurementPlanDetailsStatus.Disable),
                    CreatedAt = p.CreatedAt,
                    UpdatedAt = p.UpdatedAt
                }).ToList() ?? []
            };
        }

        // Mapper ProcurementPlanViewAllDto
        public static ProcurementPlan MapToProcurementPlanCreateDto(this ProcurementPlanCreateDto dto, Guid BusinessManager)
        {
            return new ProcurementPlan
            {
                PlanId = Guid.NewGuid(), // Assuming PlanID is generated here
                Title = dto.Title,
                Description = dto.Description,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                CreatedBy = BusinessManager,
                Status = dto.Status.ToString(),
            };
        }
    }
}
