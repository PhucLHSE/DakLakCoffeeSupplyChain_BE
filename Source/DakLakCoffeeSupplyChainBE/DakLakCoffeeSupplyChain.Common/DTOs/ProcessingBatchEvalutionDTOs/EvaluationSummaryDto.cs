using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.DTOs.ProcessingBatchEvalutionDTOs
{
    public class EvaluationSummaryDto
    {
        public Guid BatchId { get; set; }
        public int Total { get; set; }
        public int PassCount { get; set; }
        public int FailCount { get; set; }
        public string? LatestResult { get; set; }
        public DateTime? LatestAt { get; set; }
    }
}
