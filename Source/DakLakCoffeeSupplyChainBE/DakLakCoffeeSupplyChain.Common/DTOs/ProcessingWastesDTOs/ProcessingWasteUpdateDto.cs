using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.DTOs.ProcessingWastesDTOs
{
    public class ProcessingWasteUpdateDto
    {
        public Guid WasteId { get; set; }
        public string WasteType { get; set; } = string.Empty;
        public Guid ProgressId { get; set; }
        public Guid? RecordedBy { get; set; }
        public string Note { get; set; } 
        public string Unit { get; set; }
        public double Quantity { get; set; }
        public DateTime? RecordedAt { get; set; }
    }
}
