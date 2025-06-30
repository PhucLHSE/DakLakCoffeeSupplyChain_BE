using DakLakCoffeeSupplyChain.Common.DTOs.ProcessingParameterDTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.DTOs.ProcessingBatchsProgressDTOs
{
    public class ProcessingBatchProgressCreateDto
    {
        public Guid BatchId { get; set; }
        public int StepIndex { get; set; }
        public int StageId { get; set; }
        public DateOnly? ProgressDate { get; set; }
        public double? OutputQuantity { get; set; }
        public string OutputUnit { get; set; }
        public string PhotoUrl { get; set; }
        public string VideoUrl { get; set; }
    }
}
