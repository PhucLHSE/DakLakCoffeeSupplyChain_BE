using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.DTOs.ProcessingWastesDTOs
{
    public class ProcessingWasteCreateDto
    {
        public Guid ProgressId { get; set; }

        public string WasteType { get; set; } = string.Empty;

        public double Quantity { get; set; }

        public string Unit { get; set; } = string.Empty;

        public string Note { get; set; } = string.Empty;

        public DateTime? RecordedAt { get; set; }

       
    }

}
