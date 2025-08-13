using DakLakCoffeeSupplyChain.Common.DTOs.FarmingCommitmentDTOs;
using DakLakCoffeeSupplyChain.Common.DTOs.FarmingCommitmentDTOs.FarmingCommitmentsDetailsDTOs;
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
                CompanyName = fm.Plan.CreatedByNavigation.CompanyName,
                PlanTitle = fm.Plan.Title,
                TotalPrice = fm.TotalPrice,
                CommitmentDate = fm.CommitmentDate,
                ProgressPercentage = fm.ProgressPercentage,
                Status = EnumHelper.ParseEnumFromString(fm.Status, FarmingCommitmentStatus.Unknown),
                farmingCommitmentDetails = fm.FarmingCommitmentsDetails
                    ?.Select(detail => new FarmingCommitmentsDetailsViewAllDto
                    {
                        CommitmentDetailId = detail.CommitmentDetailId,
                        CommitmentDetailCode = detail.CommitmentDetailCode,
                        CommitmentId = detail.CommitmentId,
                        RegistrationDetailId = detail.RegistrationDetailId,
                        PlanDetailId = detail.PlanDetailId,
                        CoffeeTypeName = detail.PlanDetail.CoffeeType.TypeName,
                        ConfirmedPrice = detail.ConfirmedPrice,
                        CommittedQuantity = detail.CommittedQuantity,
                        EstimatedDeliveryStart = detail.EstimatedDeliveryStart,
                        EstimatedDeliveryEnd = detail.EstimatedDeliveryEnd,
                        Note = detail.Note,
                        ContractDeliveryItemId = detail.ContractDeliveryItemId,
                        CreatedAt = detail.CreatedAt,
                        UpdatedAt = detail.UpdatedAt
                    }).ToList() ?? [],
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
                PlanTitle = entity.Plan.Title,
                TotalPrice = entity.TotalPrice,
                TotalAdvancePayment = entity.TotalAdvancePayment,
                TotalTaxPrice = entity.TotalTaxPrice,
                CommitmentDate = entity.CommitmentDate,
                ProgressPercentage = entity.ProgressPercentage,
                ApprovedById = entity.ApprovedBy,
                ApprovedBy = entity.ApprovedByNavigation?.User?.Name ?? string.Empty,
                CompanyName = entity.Plan.CreatedByNavigation.CompanyName,
                ApprovedAt = entity.ApprovedAt,
                Status = EnumHelper.ParseEnumFromString(entity.Status, FarmingCommitmentStatus.Unknown),
                RejectionReason = entity.RejectionReason,
                Note = entity.Note,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt,
                FarmingCommitmentDetails = entity.FarmingCommitmentsDetails
                    ?.Select(detail => new FarmingCommitmentsDetailsViewAllDto
                    {
                        CommitmentDetailId = detail.CommitmentDetailId,
                        CommitmentDetailCode = detail.CommitmentDetailCode,
                        CommitmentId = detail.CommitmentId,
                        RegistrationDetailId = detail.RegistrationDetailId,
                        PlanDetailId = detail.PlanDetailId,
                        CoffeeTypeName = detail.PlanDetail.CoffeeType.TypeName,
                        ConfirmedPrice = detail.ConfirmedPrice,
                        AdvancePayment = detail.AdvancePayment,
                        TaxPrice = detail.TaxPrice,
                        DeliveriedQuantity = detail.DeliveriedQuantity,
                        CommittedQuantity = detail.CommittedQuantity,
                        ProgressPercentage = detail.ProgressPercentage,
                        EstimatedDeliveryStart = detail.EstimatedDeliveryStart,
                        EstimatedDeliveryEnd = detail.EstimatedDeliveryEnd,
                        Note = detail.Note,
                        Status = EnumHelper.ParseEnumFromString(detail.Status, FarmingCommitmentStatus.Unknown),
                        ContractDeliveryItemId = detail.ContractDeliveryItemId,
                        CreatedAt = detail.CreatedAt,
                        UpdatedAt = detail.UpdatedAt
                    }).ToList() ?? [],
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
                RegistrationId = dto.RegistrationId,
                TotalPrice = 0,
                TotalAdvancePayment = 0,
                TotalTaxPrice = 0,
                Status = FarmingCommitmentStatus.Pending.ToString(),
                Note = dto.Note,
                FarmingCommitmentsDetails = [.. dto.FarmingCommitmentsDetailsCreateDtos
                .Select(detail => new FarmingCommitmentsDetail {
                    CommitmentDetailId = Guid.NewGuid(),
                    RegistrationDetailId = detail.RegistrationDetailId,
                    ConfirmedPrice = detail?.ConfirmedPrice,
                    AdvancePayment = detail?.AdvancePayment,
                    TaxPrice = 0,
                    CommittedQuantity = detail?.CommittedQuantity,
                    EstimatedDeliveryStart = detail?.EstimatedDeliveryStart,
                    EstimatedDeliveryEnd = detail?.EstimatedDeliveryEnd,
                    Note = detail.Note,
                    ContractDeliveryItemId = detail?.ContractDeliveryItemId,
                })]
            };
        }
    }
}
