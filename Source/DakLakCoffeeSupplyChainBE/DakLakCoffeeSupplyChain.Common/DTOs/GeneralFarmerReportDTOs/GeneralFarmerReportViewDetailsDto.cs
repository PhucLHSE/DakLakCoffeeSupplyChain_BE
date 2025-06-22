using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.DTOs.GeneralFarmerReportDTOs
{
    public class GeneralFarmerReportViewDetailsDto
    {
        public Guid ReportId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public int? SeverityLevel { get; set; }
        public string ImageUrl { get; set; }
        public string VideoUrl { get; set; }
        public bool? IsResolved { get; set; }

        public DateTime ReportedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }

        public string ReportedByName { get; set; }
        public string CropStageName { get; set; }
        public string ProcessingBatchCode { get; set; }
    }

}
