using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.DTOs.ProcessingWastesDTOs
{
    public class ProcessingWasteViewAllDto
    {
        public Guid WasteId { get; set; }
        public string WasteCode { get; set; } = string.Empty;
        public Guid ProgressId { get; set; }
        public string WasteType { get; set; } = string.Empty;
        public double Quantity { get; set; }
        public string Unit { get; set; } = string.Empty;
        public string Note { get; set; } = string.Empty;
        public DateTime? RecordedAt { get; set; }
        public string RecordedBy { get; set; } = string.Empty;
        public bool IsDisposed { get; set; }
        public DateTime? DisposedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
