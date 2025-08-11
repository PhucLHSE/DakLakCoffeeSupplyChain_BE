using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace DakLakCoffeeSupplyChain.Common.DTOs.CropProgressDTOs
{
    public class CropProgressCreateWithMediaRequest
    {
        [Required]
        public Guid CropSeasonDetailId { get; set; }

        [Required]
        public int StageId { get; set; }

        [Required]
        public DateOnly ProgressDate { get; set; }

        public double? ActualYield { get; set; }

        public string? Notes { get; set; }

        public List<IFormFile>? MediaFiles { get; set; }
    }
} 