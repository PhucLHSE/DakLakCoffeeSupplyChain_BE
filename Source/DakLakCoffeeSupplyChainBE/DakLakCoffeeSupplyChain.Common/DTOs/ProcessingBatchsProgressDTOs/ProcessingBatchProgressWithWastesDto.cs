using DakLakCoffeeSupplyChain.Common.DTOs.ProcessingWasteDisposalDTOs;
using DakLakCoffeeSupplyChain.Common.DTOs.ProcessingWastesDTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.DTOs.ProcessingBatchsProgressDTOs
{
    public class ProcessingBatchProgressWithWastesDto
    {
        public Guid Id { get; set; }
        public DateTime ProgressDate { get; set; }
        public double OutputQuantity { get; set; }
        public string OutputUnit { get; set; } = string.Empty;
        public string? PhotoUrl { get; set; }
        public string? VideoUrl { get; set; }

        public List<ProcessingWasteViewAllDto> Wastes { get; set; }
    }
}
