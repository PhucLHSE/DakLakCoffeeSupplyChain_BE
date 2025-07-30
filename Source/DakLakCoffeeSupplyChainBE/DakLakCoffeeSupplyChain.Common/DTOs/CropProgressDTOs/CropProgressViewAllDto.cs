namespace DakLakCoffeeSupplyChain.Common.DTOs.CropProgressDTOs
{
    public class CropProgressViewAllDto
    {
        public Guid ProgressId { get; set; }
        public Guid CropSeasonDetailId { get; set; }
        public int StageId { get; set; }
        public int? StepIndex { get; set; }
        public string StageName { get; set; } = string.Empty;
        public DateOnly? ProgressDate { get; set; }
        public string Note { get; set; } = string.Empty;
        public string PhotoUrl { get; set; } = string.Empty;
        public string VideoUrl { get; set; } = string.Empty;
    }

}
