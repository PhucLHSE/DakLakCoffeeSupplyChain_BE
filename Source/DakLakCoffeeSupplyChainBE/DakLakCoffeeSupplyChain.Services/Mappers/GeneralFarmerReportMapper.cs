using DakLakCoffeeSupplyChain.Common.DTOs.GeneralFarmerReportDTOs;
using DakLakCoffeeSupplyChain.Repositories.Models;

namespace DakLakCoffeeSupplyChain.Services.Mappers
{
    public static class GeneralFarmerReportMapper
    {
        public static GeneralFarmerReportViewAllDto MapToGeneralFarmerReportViewAllDto(this GeneralFarmerReport entity)
        {
            return new GeneralFarmerReportViewAllDto
            {
                ReportId = entity.ReportId,
                Title = entity.Title,
                ReportedAt = entity.ReportedAt,
                ReportedByName = entity.ReportedByNavigation?.Name ?? string.Empty,
                IsResolved = entity.IsResolved
            };
        }

        public static GeneralFarmerReportViewDetailsDto MapToGeneralFarmerReportViewDetailsDto(this GeneralFarmerReport entity)
        {
            return new GeneralFarmerReportViewDetailsDto
            {
                ReportId = entity.ReportId,
                Title = entity.Title,
                Description = entity.Description,
                SeverityLevel = entity.SeverityLevel,
                ImageUrl = entity.ImageUrl,
                VideoUrl = entity.VideoUrl,
                IsResolved = entity.IsResolved,
                ReportedAt = entity.ReportedAt,
                UpdatedAt = entity.UpdatedAt,
                ResolvedAt = entity.ResolvedAt,
                ReportedByName = entity.ReportedByNavigation?.Name ?? string.Empty,
                CropStageName = entity.CropProgress?.Stage?.StageName ?? string.Empty,
                ProcessingBatchCode = entity.ProcessingProgress?.Batch?.BatchCode ?? string.Empty
            };
        }
    }
}
