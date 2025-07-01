using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.DTOs.ProcessingBatchsProgressDTOs
{
    public class ProcessingBatchProgressUpdateDto
    {
        public int StepIndex { get; set; }
        public DateTime ProgressDate { get; set; }
        public double? OutputQuantity { get; set; }
        public string? OutputUnit { get; set; }
        public string? PhotoUrl { get; set; }
        public string? VideoUrl { get; set; }
    }
}
