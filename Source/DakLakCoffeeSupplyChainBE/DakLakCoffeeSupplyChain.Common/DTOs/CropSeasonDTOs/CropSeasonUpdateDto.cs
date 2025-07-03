using System;
using System.ComponentModel.DataAnnotations;
using DakLakCoffeeSupplyChain.Common.Enum.CropSeasonEnums;

namespace DakLakCoffeeSupplyChain.Common.DTOs.CropSeasonDTOs
{
    public class CropSeasonUpdateDto
    {
        [Required]
        public Guid CropSeasonId { get; set; }

        //[Required]
        //public Guid FarmerId { get; set; }

        [Required]
        public Guid RegistrationId { get; set; }

        [Required]
        public Guid CommitmentId { get; set; }

        [Required]
        public string SeasonName { get; set; } = string.Empty;

        public double? Area { get; set; }

        [Required]
        public DateOnly StartDate { get; set; }

        [Required]
        public DateOnly EndDate { get; set; }

        public string? Note { get; set; }

        public CropSeasonStatus Status { get; set; } = CropSeasonStatus.Active;
    }
}
