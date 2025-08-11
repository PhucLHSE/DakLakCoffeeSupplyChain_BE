using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DakLakCoffeeSupplyChain.Common.DTOs.ProcessingParameterDTOs;

namespace DakLakCoffeeSupplyChain.Common.DTOs.ProcessingBatchsProgressDTOs
{
    public class ProcessingBatchProgressMediaResponse
    {
        public string Message { get; set; } = string.Empty;
        public Guid ProgressId { get; set; }
        public string? PhotoUrl { get; set; }
        public string? VideoUrl { get; set; }
        public int MediaCount { get; set; }
        public List<string> AllPhotoUrls { get; set; } = new List<string>();
        public List<string> AllVideoUrls { get; set; } = new List<string>();
        public List<ProcessingParameterViewAllDto> Parameters { get; set; } = new List<ProcessingParameterViewAllDto>();
    }
}
