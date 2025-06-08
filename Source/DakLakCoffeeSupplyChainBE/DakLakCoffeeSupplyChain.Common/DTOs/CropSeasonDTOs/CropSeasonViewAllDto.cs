// File: Common/DTOs/CropSeasonDTOs/CropSeasonViewAllDto.cs
using System;

namespace DakLakCoffeeSupplyChain.Common.DTOs.CropSeasonDTOs
{
    public class CropSeasonViewAllDto
    {
        public Guid CropSeasonId { get; set; }
        public string SeasonName { get; set; } = string.Empty;
        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public double? Area { get; set; }
        public string FarmerName { get; set; } = string.Empty;
    }
}
