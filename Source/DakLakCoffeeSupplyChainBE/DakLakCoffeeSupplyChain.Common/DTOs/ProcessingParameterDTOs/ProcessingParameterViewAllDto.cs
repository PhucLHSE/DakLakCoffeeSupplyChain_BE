using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.DTOs.ProcessingParameterDTOs
{
    public class ProcessingParameterViewAllDto
    {
        public Guid ParameterId { get; set; }
        public Guid ProgressId { get; set; }
        public string ParameterName { get; set; } = default!;
        public string ParameterValue { get; set; } = default!;
        public string Unit { get; set; } = default!;
        public DateTime? RecordedAt { get; set; }
    }
}
