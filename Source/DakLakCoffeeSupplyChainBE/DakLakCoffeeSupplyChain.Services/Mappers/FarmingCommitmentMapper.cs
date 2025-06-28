using DakLakCoffeeSupplyChain.Common.DTOs.FarmerDTOs;
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
                ConfirmedPrice = fm.ConfirmedPrice,
                CommittedQuantity = fm.CommittedQuantity,
                EstimatedDeliveryStart = fm.EstimatedDeliveryStart,
                EstimatedDeliveryEnd = fm.EstimatedDeliveryEnd,
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
                RegistrationDetailId = entity.RegistrationDetailId,
                PlanDetailId = entity.PlanDetailId,
                FarmerId = entity.FarmerId,
                FarmerName = entity.Farmer.User.Name,
                ConfirmedPrice = entity.ConfirmedPrice,
                CommittedQuantity = entity.CommittedQuantity,
                EstimatedDeliveryStart = entity.EstimatedDeliveryStart,
                EstimatedDeliveryEnd = entity.EstimatedDeliveryEnd,
                CommitmentDate = entity.CommitmentDate,
                ApprovedById = entity.ApprovedBy,
                ApprovedBy = entity.ApprovedByNavigation.User.Name,
                CompanyName = entity.ApprovedByNavigation.CompanyName,
                ApprovedAt = entity.ApprovedAt,
                Status = EnumHelper.ParseEnumFromString(entity.Status, FarmingCommitmentStatus.Unknown),
                RejectionReason = entity.RejectionReason,
                Note = entity.Note,
                ContractDeliveryItemId = entity.ContractDeliveryItemId,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt
            };
        }
    }
}
