using Microsoft.AspNetCore.Http;
using System;

namespace DakLakCoffeeSupplyChain.Common.DTOs.ProcessingBatchsProgressDTOs
{
    public class ProcessingBatchProgressCreateRequest
    {
        public Guid ProcessingBatchId { get; set; }
        public DateOnly? ProgressDate { get; set; }
        public double? OutputQuantity { get; set; }
        public string OutputUnit { get; set; }

        public List<IFormFile> MediaFiles { get; set; }
    }
}