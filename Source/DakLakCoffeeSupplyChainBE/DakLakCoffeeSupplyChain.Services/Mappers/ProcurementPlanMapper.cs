using DakLakCoffeeSupplyChain.Common.DTOs.BusinessManagerDTOs.ProcurementPlanViews;
using DakLakCoffeeSupplyChain.Common.DTOs.ProcurementPlanDTOs;
using DakLakCoffeeSupplyChain.Common.DTOs.ProcurementPlanDTOs.CoffeeTypeDTOs;
using DakLakCoffeeSupplyChain.Common.DTOs.ProcurementPlanDTOs.ViewDetailsDtos;
using DakLakCoffeeSupplyChain.Common.Enum.ProcurementPlanEnums;
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
                CreatedById = entity.CreatedBy,
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
                    CoffeeType = p.CoffeeType == null ? null : new CoffeeTypePlanDetailsViewDto
                    {
                        CoffeeTypeId = p.CoffeeTypeId,
                        TypeCode = p.CoffeeType.TypeCode,
                        TypeName = p.CoffeeType.TypeName,
                        BotanicalName = p.CoffeeType.BotanicalName,
                        Description = p.CoffeeType.Description,
                        TypicalRegion = p.CoffeeType.TypicalRegion,
                        SpecialtyLevel = p.CoffeeType.SpecialtyLevel
                    },
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

        // Mapper ProcurementPlanCreateDto
        public static ProcurementPlan MapToProcurementPlanCreateDto(this ProcurementPlanCreateDto dto, string planCode, string planDetailCode)
        {
            return new ProcurementPlan
            {
                PlanId = Guid.NewGuid(),
                PlanCode = planCode,
                Title = dto.Title,
                Description = dto.Description,
                TotalQuantity = 0, // Cái này chưa có default 0 trong db
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                CreatedBy = dto.CreatedById,
                Status = dto.Status.ToString(),
                ProcurementPlansDetails = [.. dto.ProcurementPlansDetails
                .Select(detail => new ProcurementPlansDetail
                {
                    PlanDetailsId = Guid.NewGuid(),
                    PlanDetailCode = planDetailCode,
                    CoffeeTypeId = detail.CoffeeType,
                    CropType = "Arabica", //Set mặc định cho CropType, trường này sẽ được loại bỏ trong tương lai
                    TargetQuantity = detail.TargetQuantity,
                    TargetRegion = detail.TargetRegion,
                    MinimumRegistrationQuantity = detail.MinimumRegistrationQuantity,
                    MinPriceRange = detail.MinPriceRange,
                    MaxPriceRange = detail.MaxPriceRange,
                    Note = detail.Note,
                    Status = detail.Status.ToString()
                })]
            };
        }
    }
}
