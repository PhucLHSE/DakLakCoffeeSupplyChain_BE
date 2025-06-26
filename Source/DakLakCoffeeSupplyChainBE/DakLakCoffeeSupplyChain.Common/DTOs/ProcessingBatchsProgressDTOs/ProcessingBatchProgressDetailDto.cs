using DakLakCoffeeSupplyChain.Common.DTOs.ProcessingParameterDTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.DTOs.ProcessingBatchsProgressDTOs
{
    public class ProcessingBatchProgressDetailDto
    {
        public Guid ProgressId { get; set; }
        public Guid BatchId { get; set; }
        public string BatchCode { get; set; }

        public int StepIndex { get; set; }
        public int StageId { get; set; }
        public string StageName { get; set; }
        public string StageDescription { get; set; }

        public DateOnly? ProgressDate { get; set; }
        public double? OutputQuantity { get; set; }
        public string OutputUnit { get; set; }

        public string PhotoUrl { get; set; }
        public string VideoUrl { get; set; }

        public string UpdatedByName { get; set; }
        public DateTime UpdatedAt { get; set; }
        public List<ProcessingParameterViewAllDto> Parameters { get; set; }

    }    
}
