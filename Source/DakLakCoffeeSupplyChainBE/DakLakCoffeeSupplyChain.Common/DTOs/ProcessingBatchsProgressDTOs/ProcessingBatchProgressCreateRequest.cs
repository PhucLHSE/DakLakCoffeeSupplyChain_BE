using DakLakCoffeeSupplyChain.Common.DTOs.ProcessingBatchsProgressDTOs;
using Microsoft.AspNetCore.Http;
using System;

namespace DakLakCoffeeSupplyChain.Common.DTOs.ProcessingBatchsProgressDTOs
{
    public class ProcessingBatchProgressCreateRequest
    {
        public Guid ProcessingBatchId { get; set; }
        public int? StageId { get; set; } // Thêm StageId để validation
        public DateOnly? ProgressDate { get; set; }
        public double? OutputQuantity { get; set; }
        public string OutputUnit { get; set; }

        public List<IFormFile>? PhotoFiles { get; set; }
        public List<IFormFile>? VideoFiles { get; set; }
        
        // Single parameter fields
        public string? ParameterName { get; set; }
        public string? ParameterValue { get; set; }
        public string? Unit { get; set; }
        public DateTime? RecordedAt { get; set; }
    }
}