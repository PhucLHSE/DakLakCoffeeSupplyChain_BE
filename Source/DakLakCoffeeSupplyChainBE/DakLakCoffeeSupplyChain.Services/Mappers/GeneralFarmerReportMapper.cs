using DakLakCoffeeSupplyChain.Common.DTOs.GeneralFarmerReportDTOs;
using DakLakCoffeeSupplyChain.Common.Enum.GeneralReportEnums;
using DakLakCoffeeSupplyChain.Common.Helpers;
using DakLakCoffeeSupplyChain.Repositories.Models;
using DakLakCoffeeSupplyChain.Services.Generators;

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

        public static GeneralFarmerReport MapToNewGeneralFarmerReportAsync(
        this GeneralFarmerReportCreateDto dto,
        string reportCode,
        Guid reportedBy)
        {
            return new GeneralFarmerReport
            {
                ReportId = Guid.NewGuid(),
                ReportCode = reportCode,
                ReportType = dto.ReportType.ToString(), 
                CropProgressId = dto.CropProgressId,
                ProcessingProgressId = dto.ProcessingProgressId,
                ReportedBy = reportedBy, // ✅ KHÔNG lấy từ dto
                Title = dto.Title,
                Description = dto.Description,
                SeverityLevel = (int)dto.SeverityLevel, // ✅ Enum → int
                ImageUrl = dto.ImageUrl ?? string.Empty,
                VideoUrl = dto.VideoUrl ?? string.Empty,
                IsResolved = false,
                ReportedAt = DateHelper.NowVietnamTime(),
                UpdatedAt = DateHelper.NowVietnamTime(),
                IsDeleted = false
            };
        }

    }
}
