using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.DTOs.ProcessingBatchsProgressDTOs
{
    public class AdvanceProcessingBatchProgressDto
    {
        public DateTime ProgressDate { get; set; }
        public double OutputQuantity { get; set; }
        public string OutputUnit { get; set; } = null!;
        public string? PhotoUrl { get; set; }
        public string? VideoUrl { get; set; }
        public List<ProcessingParameterInProgressDto>? Parameters { get; set; }
    }
}
