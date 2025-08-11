using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.DTOs.ExpertAdviceDTOs
{
    public class ExpertAdviceViewAllDto
    {
        public Guid ReportId { get; set; } 

        public Guid AdviceId { get; set; }
        public string ExpertName { get; set; } = string.Empty;
        public string ResponseType { get; set; } = string.Empty;
        public string AdviceSource { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

}
