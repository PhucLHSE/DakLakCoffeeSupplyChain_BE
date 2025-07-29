using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.DTOs.CropStageDTOs
{
    public class CropStageViewAllDto
    {
        public int StageId { get; set; }
        public string StageCode { get; set; } = string.Empty;
        public string StageName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int? OrderIndex { get; set; }
    }

}
