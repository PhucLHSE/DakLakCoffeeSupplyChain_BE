using System;
using System.ComponentModel.DataAnnotations;

namespace DakLakCoffeeSupplyChain.Common.DTOs.CropSeasonDTOs
{
    public class CropSeasonDeleteDto
    {
        [Required(ErrorMessage = "ID mùa vụ là bắt buộc.")]
        public Guid CropSeasonId { get; set; }
    }
}
