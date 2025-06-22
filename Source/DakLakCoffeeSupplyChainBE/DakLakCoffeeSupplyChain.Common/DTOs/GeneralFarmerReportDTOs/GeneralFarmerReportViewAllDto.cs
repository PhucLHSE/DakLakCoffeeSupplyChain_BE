using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.DTOs.GeneralFarmerReportDTOs
{
    public class GeneralFarmerReportViewAllDto
    {
        public Guid ReportId { get; set; }
        public string Title { get; set; }
        public DateTime ReportedAt { get; set; }
        public string ReportedByName { get; set; }
        public bool? IsResolved { get; set; }
    }

}
