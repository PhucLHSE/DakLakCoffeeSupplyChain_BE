using System;
using System.Text.Json.Serialization;
using DakLakCoffeeSupplyChain.Common.Enum.CropSeasonEnums;

namespace DakLakCoffeeSupplyChain.Common.DTOs.CropSeasonDetailDTOs
{
    public class CropSeasonDetailViewDto
    {
        public Guid FarmerId { get; set; }
        public string FarmerName { get; set; } = string.Empty;
        public Guid DetailId { get; set; }
        public Guid CoffeeTypeId { get; set; }
        public string TypeName { get; set; } = string.Empty;
        public double? AreaAllocated { get; set; }
        public DateOnly? ExpectedHarvestStart { get; set; }
        public DateOnly? ExpectedHarvestEnd { get; set; }
        public double? EstimatedYield { get; set; }
        public double? ActualYield { get; set; }
        public string PlannedQuality { get; set; } = string.Empty;
        public string QualityGrade { get; set; } = string.Empty;

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public CropDetailStatus Status { get; set; }
    }
}
