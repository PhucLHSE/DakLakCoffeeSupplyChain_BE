using DakLakCoffeeSupplyChain.Common.DTOs.ProcessingWastesDTOs;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;

namespace DakLakCoffeeSupplyChain.Common.DTOs.ProcessingBatchsProgressDTOs
{
    public class AdvanceProcessingBatchProgressRequest
    {
        public DateOnly? ProgressDate { get; set; }
        public double? OutputQuantity { get; set; }
        public string OutputUnit { get; set; }

        public List<IFormFile>? PhotoFiles { get; set; }
        public List<IFormFile>? VideoFiles { get; set; }
        
        // Stage selection fields - Sửa từ string thành int để nhất quán với model database
        public int? StageId { get; set; }        
        public int? CurrentStageId { get; set; } 
        public string? StageDescription { get; set; }
        
        // Single parameter fields
        public string? ParameterName { get; set; }
        public string? ParameterValue { get; set; }
        public string? Unit { get; set; }
        public DateTime? RecordedAt { get; set; }
        
        // Multiple parameters support
        public string? ParametersJson { get; set; } // JSON array of parameters
        
        // Thêm waste information
        public List<ProcessingWasteCreateDto>? Wastes { get; set; }
        
        // Thêm field để nhận waste data dưới dạng JSON string từ FormData
        public string? WastesJson { get; set; }
        
        // Thêm các field riêng biệt cho waste giống như parameter
        public string? WasteType { get; set; }
        public double? WasteQuantity { get; set; }
        public string? WasteUnit { get; set; }
        public string? WasteNote { get; set; }
        public DateTime? WasteRecordedAt { get; set; }
    }
}
