using System.ComponentModel.DataAnnotations;

namespace DakLakCoffeeSupplyChain.Common.DTOs.CoffeeTypeDTOs
{
    public class CoffeeTypeUpdateDto
    {
        [Required(ErrorMessage = "Id CoffeeType không được để trống")]
        public Guid CoffeeTypeId { get; set; }
        [Required(ErrorMessage = "Tên loại cà phê là bắt buộc.")]
        [StringLength(100, ErrorMessage = "Tên loại cà phê không được vượt quá 100 ký tự.")]
        public string TypeName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tên khoa học là bắt buộc.")]
        [StringLength(150, ErrorMessage = "Tên khoa học không được vượt quá 150 ký tự.")]
        public string BotanicalName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string TypicalRegion { get; set; } = string.Empty;
        public string SpecialtyLevel { get; set; } = string.Empty;
        public string CoffeeTypeCategory { get; set; } = string.Empty;
        public Guid? CoffeeTypeParentId { get; set; }
    }
}
