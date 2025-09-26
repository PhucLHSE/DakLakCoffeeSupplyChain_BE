using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.DTOs.CropDTOs
{
    public class CropDeleteDto
    {
        [Required(ErrorMessage = "CropId là bắt buộc")]
        public Guid CropId { get; set; }
    }
}

