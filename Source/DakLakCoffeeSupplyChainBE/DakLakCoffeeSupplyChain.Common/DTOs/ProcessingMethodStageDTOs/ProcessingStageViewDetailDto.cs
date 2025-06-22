using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.DTOs.ProcessingMethodStageDTOs
{
    public class ProcessingStageViewDetailDto
    {
        public int StageId { get; set; }
        public string StageCode { get; set; } = string.Empty;
        public string StageName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int OrderIndex { get; set; }
        public bool IsRequired { get; set; }
        public int MethodId { get; set; }
        public string MethodName { get; set; } = string.Empty;
        public string MethodCode { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool IsDeleted { get; set; }
    }
}
