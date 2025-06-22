using System;
using System.ComponentModel.DataAnnotations;

namespace DakLakCoffeeSupplyChain.Common.DTOs.CropProgressDTOs
{
    public class CropProgressCreateDto
    {
        [Required]
        public Guid CropSeasonDetailId { get; set; }

        [Required]
        public int StageId { get; set; }

        [StringLength(500)]
        public string? StageDescription { get; set; }

        public DateOnly? ProgressDate { get; set; }

        public string? PhotoUrl { get; set; }

        public string? VideoUrl { get; set; }

        public string? Note { get; set; }

        public int? StepIndex { get; set; }

        [Required]
        public Guid UpdatedBy { get; set; }
    }
}
