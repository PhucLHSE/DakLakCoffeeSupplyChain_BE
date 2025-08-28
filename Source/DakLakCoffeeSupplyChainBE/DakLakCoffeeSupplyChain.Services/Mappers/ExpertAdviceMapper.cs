using CloudinaryDotNet.Actions;
using DakLakCoffeeSupplyChain.Common.DTOs.ExpertAdviceDTOs;
using DakLakCoffeeSupplyChain.Repositories.Models;

public static class ExpertAdviceMapper
{
    public static ExpertAdviceViewAllDto MapToViewAllDto(this ExpertAdvice entity)
    {
        return new ExpertAdviceViewAllDto
        {
            AdviceId = entity.AdviceId,
            ReportId = entity.ReportId,
            ExpertName = entity.Expert?.User?.Name ?? "N/A",
            ResponseType = entity.ResponseType ?? string.Empty,
            AdviceSource = entity.AdviceSource ?? string.Empty,
            CreatedAt = entity.CreatedAt
        };
    }

    public static ExpertAdviceViewDetailDto MapToViewDetailDto(this ExpertAdvice entity)
    {
        return new ExpertAdviceViewDetailDto
        {
            AdviceId = entity.AdviceId,
            ReportId = entity.ReportId,
            ExpertId = entity.ExpertId,
            ExpertName = entity.Expert?.User?.Name ?? "N/A",
            ResponseType = entity.ResponseType ?? string.Empty,
            AdviceSource = entity.AdviceSource ?? string.Empty,
            AdviceText = entity.AdviceText ?? string.Empty,
            AttachedFileUrl = entity.AttachedFileUrl,
            CreatedAt = entity.CreatedAt
        };
    }
}
