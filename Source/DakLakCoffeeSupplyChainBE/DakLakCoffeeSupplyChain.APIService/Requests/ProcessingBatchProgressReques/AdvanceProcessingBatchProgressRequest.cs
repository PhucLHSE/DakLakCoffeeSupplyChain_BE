using DakLakCoffeeSupplyChain.Common.DTOs.ProcessingBatchsProgressDTOs;
using Microsoft.AspNetCore.Http;
using System;

namespace DakLakCoffeeSupplyChain.APIService.Requests.ProcessingBatchProgressReques
{
    public class AdvanceProcessingBatchProgressRequest
    {
        public DateTime ProgressDate { get; set; }
        public double? OutputQuantity { get; set; }
        public string OutputUnit { get; set; }

        public List<IFormFile>? PhotoFiles { get; set; }
        public List<IFormFile>? VideoFiles { get; set; }
        
        // Stage selection fields
        public string? StageId { get; set; }
        public string? CurrentStageId { get; set; }
        public string? StageDescription { get; set; }
        
        // Single parameter fields
        public string? ParameterName { get; set; }
        public string? ParameterValue { get; set; }
        public string? Unit { get; set; }
        public DateTime? RecordedAt { get; set; }
        
        // Multiple parameters support
        public string? ParametersJson { get; set; } // JSON array of parameters
    }
}
