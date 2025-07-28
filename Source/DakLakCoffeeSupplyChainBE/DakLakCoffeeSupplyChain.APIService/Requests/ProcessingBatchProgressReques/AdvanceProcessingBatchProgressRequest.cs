using Microsoft.AspNetCore.Http;
using System;

namespace DakLakCoffeeSupplyChain.APIService.Requests.ProcessingBatchProgressReques
{
    public class AdvanceProcessingBatchProgressRequest
    {
        public DateTime ProgressDate { get; set; }
        public double OutputQuantity { get; set; }
        public string OutputUnit { get; set; }

        public IFormFile? PhotoFile { get; set; }
        public IFormFile? VideoFile { get; set; }
    }
}
