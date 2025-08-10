using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.DTOs.ProcessingBatchEvalutionDTOs
{
    public class EvaluationCreateDto
    {
        public Guid BatchId { get; set; }
        public string EvaluationResult { get; set; } = default!;
        public string? Comments { get; set; }
        public DateTime? EvaluatedAt { get; set; }
    }
}
