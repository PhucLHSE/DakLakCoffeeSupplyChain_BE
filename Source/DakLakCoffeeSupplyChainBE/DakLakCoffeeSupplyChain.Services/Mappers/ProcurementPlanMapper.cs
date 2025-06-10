using DakLakCoffeeSupplyChain.Common.DTOs.ProcurementPlanDTOs;
using DakLakCoffeeSupplyChain.Common.DTOs.UserAccountDTOs;
using DakLakCoffeeSupplyChain.Common.Enum.ProcurementPlanEnums;
using DakLakCoffeeSupplyChain.Common.Enum.UserAccountEnums;
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
                CreatedBy = entity.CreatedBy,
                StartDate = entity.StartDate,
                EndDate = entity.EndDate,
                Status = status,
                ProgressPercentage = entity.ProgressPercentage,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt
            };
        }
        // Mapper ProcurementPlanViewDetailsDto
        public static ProcurementPlanViewDetailsDto MapToProcurementPlanViewDetailsDto(this ProcurementPlan entity, ICollection<Guid> detailList)
        {

            // Parse Status string to enum
            ProcurementPlanStatus status = Enum.TryParse<ProcurementPlanStatus>(entity.Status, true, out var parsedStatus)
                ? parsedStatus
                : ProcurementPlanStatus.Draft;

            return new ProcurementPlanViewDetailsDto
            {
                PlanId = entity.PlanId,
                PlanCode = entity.PlanCode ?? string.Empty,
                Title = entity.Title ?? string.Empty,
                Description = entity.Description ?? string.Empty,
                TotalQuantity = entity.TotalQuantity,
                CreatedBy = entity.CreatedBy,
                StartDate = entity.StartDate,
                EndDate = entity.EndDate,
                Status = status,
                ProgressPercentage = entity.ProgressPercentage,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt,
                ProcurementPlansDetails = detailList
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
