using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.DTOs.GeneralFarmerReportDTOs
{
    public class GeneralFarmerReportViewForManagerDto
    {
        public Guid ReportId { get; set; }
        public string ReportCode { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string ReportType { get; set; }
        public int? SeverityLevel { get; set; }
        public DateTime ReportedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public bool? IsResolved { get; set; }
        public string ReportedByName { get; set; }
        public string ReportedByEmail { get; set; }
        public string ReportedByPhone { get; set; }
        public string? ImageUrl { get; set; }
        public string? VideoUrl { get; set; }
        public int ExpertAdviceCount { get; set; }
    }
}
