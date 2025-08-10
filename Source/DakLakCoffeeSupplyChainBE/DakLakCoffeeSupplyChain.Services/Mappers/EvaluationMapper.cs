using DakLakCoffeeSupplyChain.Common.DTOs.ProcessingBatchEvalutionDTOs;
using DakLakCoffeeSupplyChain.Repositories.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Services.Mappers
{
    public static class EvaluationMapper
    {
        public static EvaluationViewDto MapToViewDto(this ProcessingBatchEvaluation e) => new()
        {
            EvaluationId = e.EvaluationId,
            EvaluationCode = e.EvaluationCode,
            BatchId = e.BatchId,
            EvaluatedBy = e.EvaluatedBy,
            EvaluationResult = e.EvaluationResult,
            Comments = e.Comments,
            EvaluatedAt = e.EvaluatedAt,
            CreatedAt = e.CreatedAt,
            UpdatedAt = e.UpdatedAt
        };
    }
}
