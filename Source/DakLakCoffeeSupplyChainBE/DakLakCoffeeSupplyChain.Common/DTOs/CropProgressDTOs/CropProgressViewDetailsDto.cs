﻿namespace DakLakCoffeeSupplyChain.Common.DTOs.CropProgressDTOs
{
    public class CropProgressViewDetailsDto
    {
        public Guid ProgressId { get; set; }

        public Guid CropSeasonDetailId { get; set; }

        public Guid? UpdatedBy { get; set; } 
        public int StageId { get; set; }

        public string StageName { get; set; } = string.Empty;
        public string StageCode { get; set; } = string.Empty;

        public string StageDescription { get; set; } = string.Empty;

        public double? ActualYield { get; set; }
        public DateOnly? ProgressDate { get; set; }

        public string Note { get; set; } = string.Empty;

        public string PhotoUrl { get; set; } = string.Empty;

        public string VideoUrl { get; set; } = string.Empty;

        public string UpdatedByName { get; set; } = string.Empty;

        public int? StepIndex { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }
    }
}
