using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.DTOs.ExpertAdviceDTOs
{
    public class ExpertAdviceUpdateDto
    {
        public string ResponseType { get; set; }
        public string AdviceSource { get; set; }
        public string AdviceText { get; set; }
        public string AttachedFileUrl { get; set; }
    }
}
