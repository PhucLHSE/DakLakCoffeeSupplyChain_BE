using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.DTOs.ExpertAdviceDTOs
{
    public class ExpertAdviceViewDetailDto
    {
        public Guid AdviceId { get; set; }
        public Guid ReportId { get; set; }
        public Guid ExpertId { get; set; }
        public string ExpertName { get; set; } = string.Empty;
        public string ResponseType { get; set; } = string.Empty;
        public string AdviceSource { get; set; } = string.Empty;
        public string AdviceText { get; set; } = string.Empty;
        public string? AttachedFileUrl { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
