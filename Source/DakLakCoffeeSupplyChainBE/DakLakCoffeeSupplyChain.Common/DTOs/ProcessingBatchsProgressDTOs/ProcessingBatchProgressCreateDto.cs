using DakLakCoffeeSupplyChain.Common.DTOs.ProcessingParameterDTOs;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.DTOs.ProcessingBatchsProgressDTOs
{
    public class ProcessingBatchProgressCreateDto
    {
        public int? StageId { get; set; } // Thêm StageId để validation
        public DateOnly? ProgressDate { get; set; }
        public double? OutputQuantity { get; set; }
        public string OutputUnit { get; set; }
        public string PhotoUrl { get; set; }
        public string VideoUrl { get; set; }
        public List<IFormFile>? ImageFiles { get; set; }
        public List<IFormFile>? VideoFiles { get; set; }
        public List<ProcessingParameterInProgressDto>? Parameters { get; set; }
    }
}
