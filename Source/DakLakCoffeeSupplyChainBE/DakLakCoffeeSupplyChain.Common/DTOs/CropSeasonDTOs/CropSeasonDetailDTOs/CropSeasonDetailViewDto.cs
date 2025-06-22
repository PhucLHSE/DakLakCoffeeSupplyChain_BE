namespace DakLakCoffeeSupplyChain.Common.DTOs.CropSeasonDetailDTOs
{
    namespace DakLakCoffeeSupplyChain.Common.DTOs.CropSeasonDetailDTOs
    {
        public class CropSeasonDetailViewDto
        {
            public Guid DetailId { get; set; }
            public double Area { get; set; }
            public Guid CoffeeTypeId { get; set; }
            public string TypeName { get; set; } = string.Empty;
            public DateOnly? ExpectedHarvestStart { get; set; }
            public DateOnly? ExpectedHarvestEnd { get; set; }
            public double? EstimatedYield { get; set; }
            public string PlannedQuality { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;
        }

    }

}
