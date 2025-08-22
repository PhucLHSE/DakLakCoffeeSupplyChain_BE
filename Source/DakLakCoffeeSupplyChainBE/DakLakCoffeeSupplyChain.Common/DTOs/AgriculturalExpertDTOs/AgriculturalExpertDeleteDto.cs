using System;
using System.ComponentModel.DataAnnotations;

namespace DakLakCoffeeSupplyChain.Common.DTOs.AgriculturalExpertDTOs
{
    public class AgriculturalExpertDeleteDto
    {
        [Required]
        public Guid ExpertId { get; set; }
    }
}
