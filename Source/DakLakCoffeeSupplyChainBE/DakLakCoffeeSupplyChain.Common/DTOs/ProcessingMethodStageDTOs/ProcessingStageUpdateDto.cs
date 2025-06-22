using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.DTOs.ProcessingMethodStageDTOs
{
    public class ProcessingStageUpdateDto
    {
        public int StageId { get; set; }              
        public string StageCode { get; set; } = string.Empty;
        public string StageName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int OrderIndex { get; set; }
        public bool IsRequired { get; set; }
        public int MethodId { get; set; }            
    }
}
