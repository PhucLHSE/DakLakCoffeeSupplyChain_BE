using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.DTOs.CropStageDTOs
{
    public class CropStageCreateDto
    {
        public string StageCode { get; set; } = null!;
        public string StageName { get; set; } = null!;
        public string? Description { get; set; }
        public int? OrderIndex { get; set; }
    }


}
