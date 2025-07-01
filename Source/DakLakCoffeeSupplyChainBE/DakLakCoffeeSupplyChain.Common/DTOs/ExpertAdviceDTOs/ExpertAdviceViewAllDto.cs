using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.DTOs.ExpertAdviceDTOs
{
    public class ExpertAdviceViewAllDto
    {
        public Guid AdviceId { get; set; }
        public string ExpertName { get; set; }
        public string ResponseType { get; set; }
        public string AdviceSource { get; set; }
        public DateTime CreatedAt { get; set; }
    }

}
