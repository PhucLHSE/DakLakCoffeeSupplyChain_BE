using System;
using System.ComponentModel.DataAnnotations;
using DakLakCoffeeSupplyChain.Common.Enum.CropSeasonEnums;

namespace DakLakCoffeeSupplyChain.Common.DTOs.CropSeasonDetailDTOs
{
    public class CropSeasonDetailUpdateDto
    {
        [Required]
        public Guid DetailId { get; set; }

        [Required]
        public Guid CoffeeTypeId { get; set; }

        public DateOnly? ExpectedHarvestStart { get; set; }
        public DateOnly? ExpectedHarvestEnd { get; set; }
        public double? AreaAllocated { get; set; }
        public string? PlannedQuality { get; set; }
    }
}
