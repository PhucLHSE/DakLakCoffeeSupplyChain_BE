using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.DTOs.ProcessingWasteDisposalDTOs
{
    public class ProcessingWasteDisposalViewAllDto
    {
        public Guid DisposalId { get; set; }
        public string DisposalCode { get; set; } = string.Empty;
        public Guid WasteId { get; set; }
        public string DisposalMethod { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public bool IsSold { get; set; }
        public decimal? Revenue { get; set; }
        public DateTime? HandledAt { get; set; }
        public string HandledByName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
