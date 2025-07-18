using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.DTOs.ProcessingWasteDisposalDTOs
{
    public class ProcessingWasteDisposalDto
    {
        public Guid DisposalId { get; set; }
        public string DisposalCode { get; set; }
        public Guid WasteId { get; set; }
        public string WasteName { get; set; }
        public string DisposalMethod { get; set; }
        public Guid HandledBy { get; set; }
        public string HandledByName { get; set; }
        public DateTime HandledAt { get; set; }
        public string Notes { get; set; }
        public bool IsSold { get; set; }
        public decimal? Revenue { get; set; }
        public DateTime CreatedAt { get; set; }
    }

}
