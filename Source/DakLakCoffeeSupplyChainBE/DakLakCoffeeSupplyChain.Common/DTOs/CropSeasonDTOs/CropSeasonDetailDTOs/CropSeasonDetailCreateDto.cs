using DakLakCoffeeSupplyChain.Common.Enum.CropSeasonEnums;
using System;
using System.Text.Json.Serialization;

namespace DakLakCoffeeSupplyChain.Common.DTOs.CropSeasonDetailDTOs
{
    public class CropSeasonDetailCreateDto
    {
        public Guid CropSeasonId { get; set; }
        public Guid CommitmentDetailId { get; set; }
        public DateOnly? ExpectedHarvestStart { get; set; }
        public DateOnly? ExpectedHarvestEnd { get; set; }

        public double? EstimatedYield { get; set; }
        public double? AreaAllocated { get; set; }
        public string? PlannedQuality { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public CropDetailStatus Status { get; set; }
        public Guid? CoffeeTypeId { get; set; }

    }
}
