using DakLakCoffeeSupplyChain.Common.Enum.CropSeasonEnums;
using System;

namespace DakLakCoffeeSupplyChain.Common.DTOs.CropSeasonDTOs
{
    public class CropSeasonCreateDto
    {
        //public Guid FarmerId { get; set; }
        //public Guid RegistrationId { get; set; }
        public Guid CommitmentId { get; set; }
        public string SeasonName { get; set; } = string.Empty;
        public double? Area { get; set; }
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
        public string? Note { get; set; }

        public CropSeasonStatus Status { get; set; } = CropSeasonStatus.Active;
    }
}
