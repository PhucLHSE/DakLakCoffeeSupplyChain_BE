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
            TotalScore = e.TotalScore, // 🔧 MỚI: Map điểm số đánh giá
            Comments = e.Comments,
            EvaluatedAt = e.EvaluatedAt,
            CreatedAt = e.CreatedAt,
            UpdatedAt = e.UpdatedAt,
            DetailedFeedback = ParseDetailedFeedback(e.Comments),
            ProblematicSteps = ParseProblematicSteps(e.Comments),
            Recommendations = ParseRecommendations(e.Comments),
            
            // Thông tin batch để hiển thị trong bảng
            BatchCode = e.Batch?.BatchCode,
            FarmerName = e.Batch?.Farmer?.User?.Name,
            MethodName = e.Batch?.Method?.Name,
            InputQuantity = e.Batch?.InputQuantity,
            InputUnit = e.Batch?.InputUnit,
            BatchStatus = e.Batch?.Status,
            
            // Thông tin expert (người đánh giá) - sẽ được set từ service
            ExpertName = null // Sẽ được set trong service khi cần
        };

        private static string? ParseDetailedFeedback(string? comments)
        {
            if (string.IsNullOrEmpty(comments)) return null;
            
            var lines = comments.Split('\n');
            foreach (var line in lines)
            {
                if (line.Trim().StartsWith("Chi tiết vấn đề:"))
                {
                    return line.Replace("Chi tiết vấn đề:", "").Trim();
                }
            }
            return null;
        }

        private static List<string>? ParseProblematicSteps(string? comments)
        {
            if (string.IsNullOrEmpty(comments)) return null;
            
            var lines = comments.Split('\n');
            foreach (var line in lines)
            {
                if (line.Trim().StartsWith("Tiến trình có vấn đề:"))
                {
                    var stepsText = line.Replace("Tiến trình có vấn đề:", "").Trim();
                    return stepsText.Split(',').Select(s => s.Trim()).ToList();
                }
            }
            return null;
        }

        private static string? ParseRecommendations(string? comments)
        {
            if (string.IsNullOrEmpty(comments)) return null;
            
            var lines = comments.Split('\n');
            foreach (var line in lines)
            {
                if (line.Trim().StartsWith("Khuyến nghị:"))
                {
                    return line.Replace("Khuyến nghị:", "").Trim();
                }
            }
            return null;
        }
    }
}
