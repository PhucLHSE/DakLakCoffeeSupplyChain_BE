using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.DTOs.ProcessingWasteDisposalDTOs
{
    public class ProcessingWasteDisposalCreateDto
    {
        [Required]
        public Guid WasteId { get; set; }

        [Required]
        [StringLength(200)]
        public string DisposalMethod { get; set; }
        [Required]
        public DateTime HandledAt { get; set; }
        [StringLength(500)]
        public string Notes { get; set; }

        public bool IsSold { get; set; }

        public decimal? Revenue { get; set; }
    }
}
