using DakLakCoffeeSupplyChain.Common.Enum.CropSeasonEnums;
using System;
using System.Collections.Generic;

namespace DakLakCoffeeSupplyChain.Common.DTOs.CropSeasonDTOs
{
    public class CropSeasonCreateDto
    {
        public Guid FarmerId { get; set; }
        public Guid RegistrationId { get; set; }
        public Guid CommitmentId { get; set; }
        public string SeasonName { get; set; } = string.Empty;
        public double? Area { get; set; }
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
        public string? Note { get; set; }

        public CropSeasonStatus Status { get; set; } = CropSeasonStatus.Active;

        public List<CropSeasonDetailCreateDto> Details { get; set; } = new();
    }

    public class CropSeasonDetailCreateDto
    {
        public Guid CoffeeTypeId { get; set; }
        public DateOnly? ExpectedHarvestStart { get; set; }
        public DateOnly? ExpectedHarvestEnd { get; set; }
        public double? EstimatedYield { get; set; }
        public double? AreaAllocated { get; set; }
        public string? PlannedQuality { get; set; }

        public CropDetailStatus Status { get; set; } = CropDetailStatus.Planned;
    }
}
