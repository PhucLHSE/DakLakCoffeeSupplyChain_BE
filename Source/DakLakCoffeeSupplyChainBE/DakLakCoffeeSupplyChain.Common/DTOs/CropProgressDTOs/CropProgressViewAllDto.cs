namespace DakLakCoffeeSupplyChain.Common.DTOs.CropProgressDTOs
{
    public class CropProgressViewAllDto
    {
        public Guid ProgressId { get; set; }
        public Guid CropSeasonDetailId { get; set; }
        public int StageId { get; set; }
        public int? StepIndex { get; set; }
        public string StageName { get; set; } = string.Empty;
        public string StageCode { get; set; } = string.Empty;
        public string StageDescription { get; set; } = string.Empty; // Thêm StageDescription
        public DateOnly? ProgressDate { get; set; }
        public string Note { get; set; } = string.Empty;
        public string PhotoUrl { get; set; } = string.Empty;
        public string VideoUrl { get; set; } = string.Empty;
        public double? ActualYield { get; set; }
        
        // Thêm thông tin về người tạo/cập nhật
        public Guid? UpdatedBy { get; set; }
        public string UpdatedByName { get; set; } = string.Empty;
        
        // Thêm thông tin thời gian
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        
        // Thêm thông tin về vùng trồng
        public string CropSeasonName { get; set; } = string.Empty;
        public string CropSeasonDetailName { get; set; } = string.Empty;
    }
}