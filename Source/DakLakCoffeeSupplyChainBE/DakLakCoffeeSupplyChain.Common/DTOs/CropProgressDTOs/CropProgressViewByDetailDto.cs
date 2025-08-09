namespace DakLakCoffeeSupplyChain.Common.DTOs.CropProgressDTOs
{
    public class CropProgressViewByDetailDto
    {
        public Guid CropSeasonDetailId { get; set; }
        public List<CropProgressViewAllDto> Progresses { get; set; } = new();
    }
}