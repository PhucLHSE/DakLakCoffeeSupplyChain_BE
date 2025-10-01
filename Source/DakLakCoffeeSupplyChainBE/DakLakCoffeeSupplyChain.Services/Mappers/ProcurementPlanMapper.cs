using DakLakCoffeeSupplyChain.Common.DTOs.BusinessManagerDTOs.ProcurementPlanViews;
using DakLakCoffeeSupplyChain.Common.DTOs.FarmingCommitmentDTOs;
using DakLakCoffeeSupplyChain.Common.DTOs.ProcurementPlanDTOs;
using DakLakCoffeeSupplyChain.Common.DTOs.ProcurementPlanDTOs.CoffeeTypeDTOs;
using DakLakCoffeeSupplyChain.Common.DTOs.ProcurementPlanDTOs.ViewDetailsDtos;
using DakLakCoffeeSupplyChain.Common.Enum.FarmingCommitmentEnums;
using DakLakCoffeeSupplyChain.Common.Enum.ProcurementPlanEnums;
using DakLakCoffeeSupplyChain.Common.Helpers;
using DakLakCoffeeSupplyChain.Repositories.Models;

namespace DakLakCoffeeSupplyChain.Services.Mappers
{
    public static class ProcurementPlanMapper
    {
        // Mapper ProcurementPlanViewAllDto
        public static ProcurementPlanViewAllDto MapToProcurementPlanViewAllDto(this ProcurementPlan plan)
        {
            // Parse Status string to enum
            ProcurementPlanStatus status = Enum.TryParse<ProcurementPlanStatus>(plan.Status, true, out var parsedStatus)
                ? parsedStatus
                : ProcurementPlanStatus.Draft;

            return new ProcurementPlanViewAllDto
            {
                PlanId = plan.PlanId,
                PlanCode = plan.PlanCode ?? string.Empty,
                Title = plan.Title ?? string.Empty,
                Description = plan.Description ?? string.Empty,
                TotalQuantity = plan.TotalQuantity,
                CreatedBy = new BusinessManagerSummaryDto
                {
                    ManagerId = plan.CreatedByNavigation.ManagerId,
                    UserId = plan.CreatedByNavigation.UserId,
                    ManagerCode = plan.CreatedByNavigation.ManagerCode,
                    CompanyName = plan.CreatedByNavigation.CompanyName,
                    CompanyAddress = plan.CreatedByNavigation.CompanyAddress,
                    Website = plan.CreatedByNavigation.Website,
                    ContactEmail = plan.CreatedByNavigation.ContactEmail
                },
                StartDate = plan.StartDate,
                EndDate = plan.EndDate,
                Status = status,
                ProgressPercentage = plan.ProgressPercentage,
                Commitments = plan.FarmingCommitments?.Select(
                    p => new FarmingCommitmentViewAllDto
                    {
                        CommitmentId = p.CommitmentId,
                        CommitmentCode = p.CommitmentCode,
                        CommitmentName = p.CommitmentName,
                        FarmerName = p.Farmer.User.Name,
                        PlanTitle = p.Plan.Title,
                        TotalPrice = p.TotalPrice,
                        CommitmentDate = p.CommitmentDate,
                        Status = EnumHelper.ParseEnumFromString(p.Status, FarmingCommitmentStatus.Unknown)
                    }
                    ).ToList() ?? [],
                CreatedAt = plan.CreatedAt,
                UpdatedAt = plan.UpdatedAt
            };
        }

        // Map to available dto api
        public static ProcurementPlanViewAllDto MapToProcurementPlanViewAllAvailableDto(this ProcurementPlan plan)
        {
            return new ProcurementPlanViewAllDto
            {
                PlanId = plan.PlanId,
                PlanCode = plan.PlanCode ?? string.Empty,
                Title = plan.Title ?? string.Empty,
                Description = plan.Description ?? string.Empty,
                TotalQuantity = plan.TotalQuantity,
                CreatedBy = new BusinessManagerSummaryDto
                {
                    ManagerId = plan.CreatedByNavigation.ManagerId,
                    UserId = plan.CreatedByNavigation.UserId,
                    ManagerCode = plan.CreatedByNavigation.ManagerCode,
                    CompanyName = plan.CreatedByNavigation.CompanyName,
                    CompanyAddress = plan.CreatedByNavigation.CompanyAddress,
                    Website = plan.CreatedByNavigation.Website,
                    ContactEmail = plan.CreatedByNavigation.ContactEmail
                },
                StartDate = plan.StartDate,
                EndDate = plan.EndDate,
                Status = EnumHelper.ParseEnumFromString(plan.Status, ProcurementPlanStatus.Unknown),
                ProgressPercentage = plan.ProgressPercentage,
                CreatedAt = plan.CreatedAt,
                UpdatedAt = plan.UpdatedAt,
                ProcurementPlansDetails = plan.ProcurementPlansDetails.Select(p => new ProcurementPlanDetailsDto
                {
                    PlanDetailsId = p.PlanDetailsId,
                    PlanDetailCode = p.PlanDetailCode,
                    PlanId = p.PlanId,
                    CoffeeType = new CoffeeTypePlanDetailsViewDto
                    {
                        CoffeeTypeId = p.CoffeeTypeId,
                        TypeCode = p.CoffeeType.TypeCode,
                        TypeName = p.CoffeeType.TypeName,
                        BotanicalName = p.CoffeeType.BotanicalName,
                        Description = p.CoffeeType.Description,
                        TypicalRegion = p.CoffeeType.TypicalRegion,
                        SpecialtyLevel = p.CoffeeType.SpecialtyLevel
                    },
                    ProcessingMethodName = p.ProcessMethod?.Name,
                    TargetQuantity = p.TargetQuantity,
                    TargetRegion = p.TargetRegion,
                    MinimumRegistrationQuantity = p.MinimumRegistrationQuantity,
                    MinPriceRange = p.MinPriceRange,
                    MaxPriceRange = p.MaxPriceRange,
                    Note = p.Note,
                    ProgressPercentage = p.ProgressPercentage,
                    Status = EnumHelper.ParseEnumFromString(p.Status, ProcurementPlanDetailsStatus.Disable),
                    CreatedAt = p.CreatedAt,
                    UpdatedAt = p.UpdatedAt
                }).ToList() ?? []
            };
        }

        // Mapper ProcurementPlanViewDetailsDto
        public static ProcurementPlanViewDetailsSumaryDto MapToProcurementPlanViewDetailsDto(this ProcurementPlan plan)
        {
            return new ProcurementPlanViewDetailsSumaryDto
            {
                PlanId = plan.PlanId,
                PlanCode = plan.PlanCode ?? string.Empty,
                Title = plan.Title ?? string.Empty,
                Description = plan.Description ?? string.Empty,
                TotalQuantity = plan.TotalQuantity,
                CreatedById = plan.CreatedBy,
                CreatedBy = plan.CreatedByNavigation == null ? null : new BusinessManagerSummaryDto
                {
                    ManagerId = plan.CreatedByNavigation.ManagerId,
                    UserId = plan.CreatedByNavigation.UserId,
                    ManagerCode = plan.CreatedByNavigation.ManagerCode,
                    CompanyName = plan.CreatedByNavigation.CompanyName,
                    CompanyAddress = plan.CreatedByNavigation.CompanyAddress,
                    Website = plan.CreatedByNavigation.Website,
                    ContactEmail = plan.CreatedByNavigation.ContactEmail
                },
                StartDate = plan.StartDate,
                EndDate = plan.EndDate,
                Status = EnumHelper.ParseEnumFromString(plan.Status, ProcurementPlanStatus.Draft),
                ProgressPercentage = plan.ProgressPercentage,
                CreatedAt = plan.CreatedAt,
                UpdatedAt = plan.UpdatedAt,
                ProcurementPlansDetails = plan.ProcurementPlansDetails?
                .OrderBy(p => p.PlanDetailCode)
                .Select(p => new ProcurementPlanDetailsDto
                {
                    PlanDetailsId = p.PlanDetailsId,
                    PlanDetailCode = p.PlanDetailCode,
                    PlanId = p.PlanId,
                    CoffeeTypeId = p.CoffeeTypeId,
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
                    ProcessMethodId = p.ProcessMethodId,
                    ProcessingMethodName = p.ProcessMethod?.Name,
                    TargetQuantity = p.TargetQuantity,
                    TargetRegion = p.TargetRegion,
                    //TargetRegions = p.TargetRegion,
                    MinimumRegistrationQuantity = p.MinimumRegistrationQuantity,
                    MinPriceRange = p.MinPriceRange,
                    MaxPriceRange = p.MaxPriceRange,
                    ExpectedYieldPerHectare = p.ExpectedYieldPerHectare,
                    Note = p.Note,
                    ProgressPercentage = p.ProgressPercentage,
                    Status = EnumHelper.ParseEnumFromString(p.Status, ProcurementPlanDetailsStatus.Disable),
                    CreatedAt = p.CreatedAt,
                    UpdatedAt = p.UpdatedAt
                }).ToList() ?? []
            };
        }

        // Mapper ProcurementPlanCreateDto
        public static ProcurementPlan MapToProcurementPlanCreateDto(this ProcurementPlanCreateDto dto, string planCode, Guid bmId)
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
                CreatedBy = bmId,
                Status = ProcurementPlanStatus.Draft.ToString(),
                ProcurementPlansDetails = [.. dto.ProcurementPlansDetails
                .Select(detail => new ProcurementPlansDetail
                {
                    PlanDetailsId = Guid.NewGuid(),
                    CoffeeTypeId = detail.CoffeeTypeId,
                    ProcessMethodId = detail.ProcessMethodId,
                    TargetQuantity = detail.TargetQuantity,
                    //TargetRegion = detail.TargetRegion,
                    MinimumRegistrationQuantity = detail.MinimumRegistrationQuantity,
                    MinPriceRange = detail.MinPriceRange,
                    MaxPriceRange = detail.MaxPriceRange,
                    ExpectedYieldPerHectare = detail.ExpectedYieldPerHectare,
                    Note = detail.Note,
                    Status = ProcurementPlanDetailsStatus.Active.ToString(),
                    ContractItemId = detail.ContractItemId
                })]
            };
        }

        //Mapper ProcurementPlanUpdateDto
        public static void MapToProcurementPlanUpdate(this ProcurementPlanUpdateDto dto, ProcurementPlan p)
        {
            p.Title = dto.Title.HasValue() ? dto.Title : p.Title;
            p.Description = dto.Description.HasValue() ? dto.Description : p.Description;
            p.StartDate = dto.StartDate.HasValue ? dto.StartDate : p.StartDate;
            p.EndDate = dto.EndDate.HasValue ? dto.EndDate : p.EndDate;
            p.Status = dto.Status.ToString().HasValue() ? dto.Status.ToString() : p.Status;
            p.UpdatedAt = DateHelper.NowVietnamTime();
        }
    }
}
