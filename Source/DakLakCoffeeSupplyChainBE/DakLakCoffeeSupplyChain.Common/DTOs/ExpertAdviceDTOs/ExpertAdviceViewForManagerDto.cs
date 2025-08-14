using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.DTOs.ExpertAdviceDTOs
{
    public class ExpertAdviceViewForManagerDto
    {
        public Guid AdviceId { get; set; }
        public Guid ReportId { get; set; }
        public string ReportTitle { get; set; }
        public string ReportCode { get; set; }
        public Guid ExpertId { get; set; }
        public string ExpertName { get; set; }
        public string ExpertEmail { get; set; }
        public string ResponseType { get; set; }
        public string AdviceSource { get; set; }
        public string? AdviceText { get; set; }
        public string? AttachedFileUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public string ReportedByName { get; set; }
        public string ReportedByEmail { get; set; }
    }
}
