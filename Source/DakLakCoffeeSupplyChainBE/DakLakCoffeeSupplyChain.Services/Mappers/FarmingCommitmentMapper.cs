using DakLakCoffeeSupplyChain.Common.DTOs.FarmingCommitmentDTOs;
using DakLakCoffeeSupplyChain.Common.Enum.FarmingCommitmentEnums;
using DakLakCoffeeSupplyChain.Common.Helpers;
using DakLakCoffeeSupplyChain.Repositories.Models;

namespace DakLakCoffeeSupplyChain.Services.Mappers
{
    public static class FarmingCommitmentMapper
    {
        // Mapper FarmingCommitmentViewAllDto
        public static FarmingCommitmentViewAllDto MapToFarmingCommitmentViewAllDto(this FarmingCommitment fm)
        {
            return new FarmingCommitmentViewAllDto
            {
                CommitmentId = fm.CommitmentId,
                CommitmentCode = fm.CommitmentCode,
                CommitmentName = fm.CommitmentName,
                FarmerName = fm.Farmer.User.Name,
                PlanTitle = fm.Plan.Title,
                CommitmentDate = fm.CommitmentDate,
                Status = EnumHelper.ParseEnumFromString(fm.Status, FarmingCommitmentStatus.Unknown)
            };
        }

        // Mapper FarmingCommitmentViewDetailsDto
        public static FarmingCommitmentViewDetailsDto MapToFarmingCommitmentViewDetailsDto(this FarmingCommitment entity)
        {
            return new FarmingCommitmentViewDetailsDto
            {
                CommitmentId = entity.CommitmentId,
                CommitmentCode = entity.CommitmentCode,
                CommitmentName = entity.CommitmentName,
                RegistrationId = entity.RegistrationId,
                PlanId = entity.PlanId,
                FarmerId = entity.FarmerId,
                FarmerName = entity.Farmer.User.Name,
                //ConfirmedPrice = entity.ConfirmedPrice,
                //CommittedQuantity = entity.CommittedQuantity,
                //EstimatedDeliveryStart = entity.EstimatedDeliveryStart,
                //EstimatedDeliveryEnd = entity.EstimatedDeliveryEnd,
                CommitmentDate = entity.CommitmentDate,
                ApprovedById = entity.ApprovedBy,
                ApprovedBy = entity.ApprovedByNavigation?.User?.Name ?? string.Empty,
                CompanyName = entity.Plan.CreatedByNavigation.CompanyName,
                ApprovedAt = entity.ApprovedAt,
                Status = EnumHelper.ParseEnumFromString(entity.Status, FarmingCommitmentStatus.Unknown),
                RejectionReason = entity.RejectionReason,
                Note = entity.Note,
                //ContractDeliveryItemId = entity.ContractDeliveryItemId,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt
            };
        }

        // Mapper FarmingCommitmentCreateDto
        public static FarmingCommitment MapToFarmingCommitment(this FarmingCommitmentCreateDto dto, string commitmentCode)
        {
            return new FarmingCommitment
            {
                CommitmentId = Guid.NewGuid(),
                CommitmentCode = commitmentCode,
                CommitmentName = dto.CommitmentName,
                //RegistrationDetailId = dto.RegistrationDetailId,
                //PlanDetailId = dto.PlanDetailId,
                //FarmerId = dto.FarmerId,
                //ConfirmedPrice = dto.ConfirmedPrice,
                //CommittedQuantity = dto.CommittedQuantity,
                //EstimatedDeliveryStart = dto.EstimatedDeliveryStart,
                //EstimatedDeliveryEnd = dto.EstimatedDeliveryEnd,
                Status = FarmingCommitmentStatus.Pending_farmer.ToString(),
                Note = dto.Note,
                //ContractDeliveryItemId = dto.ContractDeliveryItemId
            };
        }
    }
}
