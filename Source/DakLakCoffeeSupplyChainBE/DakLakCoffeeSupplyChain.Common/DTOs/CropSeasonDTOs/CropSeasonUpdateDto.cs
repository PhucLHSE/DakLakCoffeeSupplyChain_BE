using System;
using System.ComponentModel.DataAnnotations;
using DakLakCoffeeSupplyChain.Common.Enum.CropSeasonEnums;

namespace DakLakCoffeeSupplyChain.Common.DTOs.CropSeasonDTOs
{
    public class CropSeasonUpdateDto
    {
        [Required]
        public Guid CropSeasonId { get; set; }
        [Required]
        public string SeasonName { get; set; } = string.Empty;

        [Required]
        public DateOnly StartDate { get; set; }

        [Required]
        public DateOnly EndDate { get; set; }

        public string? Note { get; set; }

    }
}
