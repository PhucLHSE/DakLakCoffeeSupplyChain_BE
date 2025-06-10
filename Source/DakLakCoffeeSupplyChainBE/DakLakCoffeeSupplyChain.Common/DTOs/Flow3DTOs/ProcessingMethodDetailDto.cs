using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.DTOs.Flow3DTOs
{
    public class ProcessingMethodDetailDto
    {
        public int  MethodId { get; set; }
        public string MethodCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int StageCount { get; set; }
    }

}
