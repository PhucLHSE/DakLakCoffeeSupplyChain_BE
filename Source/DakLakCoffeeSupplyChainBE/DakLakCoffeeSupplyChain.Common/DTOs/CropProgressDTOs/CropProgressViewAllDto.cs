﻿namespace DakLakCoffeeSupplyChain.Common.DTOs.CropProgressDTOs
{
    public class CropProgressViewAllDto
    {
        public Guid ProgressId { get; set; }

        public string StageName { get; set; } = string.Empty;

        public DateOnly? ProgressDate { get; set; }

        public string Note { get; set; } = string.Empty;

        public string PhotoUrl { get; set; } = string.Empty;

        public string VideoUrl { get; set; } = string.Empty;
    }
}
