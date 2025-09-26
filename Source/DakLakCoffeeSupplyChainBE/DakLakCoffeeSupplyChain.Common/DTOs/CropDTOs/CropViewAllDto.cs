using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.DTOs.CropDTOs
{
    public class CropViewAllDto
    {
        public Guid CropId { get; set; }

        public string CropCode { get; set; } = string.Empty;

        public string Address { get; set; } = string.Empty;

        public string FarmName { get; set; } = string.Empty;

        public decimal? CropArea { get; set; }

        public string Status { get; set; } = "Active";
    }
}
