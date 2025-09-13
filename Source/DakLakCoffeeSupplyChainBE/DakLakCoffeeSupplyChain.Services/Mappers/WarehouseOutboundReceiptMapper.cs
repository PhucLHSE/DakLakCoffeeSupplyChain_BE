using DakLakCoffeeSupplyChain.Common.DTOs.WarehouseOutboundReceiptDTOs;
using DakLakCoffeeSupplyChain.Common.Enum.InventoryLogEnums;
using DakLakCoffeeSupplyChain.Common.Enum.WarehouseOutboundRequestEnums;
using DakLakCoffeeSupplyChain.Repositories.Models;
using System;
using System.Linq;

namespace DakLakCoffeeSupplyChain.Services.Mappers
{
    public static class WarehouseOutboundReceiptMapper
    {
        public static WarehouseOutboundReceipt MapFromCreateDto(
            this WarehouseOutboundReceiptCreateDto dto,
            Guid outboundReceiptId,
            string receiptCode,
            Guid staffId,
            Guid batchId)
        {
            return new WarehouseOutboundReceipt
            {
                OutboundReceiptId = outboundReceiptId,
                OutboundReceiptCode = receiptCode,
                OutboundRequestId = dto.OutboundRequestId,
                WarehouseId = dto.WarehouseId,
                InventoryId = dto.InventoryId,
                BatchId = batchId,
                Quantity = dto.ExportedQuantity,                 // SL ghi nhận cho phiếu (draft)
                ExportedBy = staffId,
                ExportedAt = DateTime.UtcNow,
                Note = (dto.Note ?? "") + " [WAITING_FOR_PICKUP]", // Trạng thái "Chờ lấy hàng" ngay từ đầu
                DestinationNote = dto.Destination ?? "",          // Map từ Destination
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            };
        }

        // LƯU Ý: Không ghi đè Quantity nữa để hỗ trợ partial; chỉ append tag xác nhận
        public static void UpdateAfterConfirm(
            this WarehouseOutboundReceipt receipt,
            double confirmedQuantity,
            string? destinationNote)
        {
            // Append tag xác nhận cho lần này
            receipt.Note = (receipt.Note ?? "") + $" [CONFIRMED:{confirmedQuantity}]";
            if (!string.IsNullOrWhiteSpace(destinationNote))
                receipt.DestinationNote = destinationNote!;
            receipt.UpdatedAt = DateTime.UtcNow;
        }

        public static void MarkAsCompleted(this WarehouseOutboundRequest request)
        {
            request.Status = WarehouseOutboundRequestStatus.Completed.ToString();
            request.UpdatedAt = DateTime.UtcNow;
        }

        public static InventoryLog ToInventoryLogFromOutbound(
            this WarehouseOutboundReceipt receipt,
            Guid inventoryId,
            double confirmedQuantity)
        {
            return new InventoryLog
            {
                LogId = Guid.NewGuid(),
                InventoryId = inventoryId,
                ActionType = InventoryLogActionType.decrease.ToString(),
                QuantityChanged = -confirmedQuantity,
                Note = $"Xuất kho từ phiếu {receipt.OutboundReceiptCode}",
                UpdatedBy = receipt.ExportedBy, // ✅ Track staff thực hiện xuất kho
                TriggeredBySystem = false, // ✅ Không phải hệ thống tự động
                LoggedAt = DateTime.UtcNow,
                IsDeleted = false
            };
        }

        public static WarehouseOutboundReceiptListItemDto ToListItemDto(this WarehouseOutboundReceipt r)
        {
            return new WarehouseOutboundReceiptListItemDto
            {
                OutboundReceiptId = r.OutboundReceiptId,
                OutboundReceiptCode = r.OutboundReceiptCode,
                WarehouseName = r.Warehouse?.Name ?? "N/A",
                BatchCode = r.Batch?.BatchCode ?? "N/A",
                Quantity = r.Quantity,
                ExportedAt = r.ExportedAt,
                StaffName = r.ExportedByNavigation?.User?.Name ?? "N/A"
            };
        }

        public static WarehouseOutboundReceiptDetailDto ToDetailDto(this WarehouseOutboundReceipt r)
        {
            return new WarehouseOutboundReceiptDetailDto
            {
                OutboundReceiptId = r.OutboundReceiptId,
                OutboundReceiptCode = r.OutboundReceiptCode,
                WarehouseId = r.WarehouseId,
                WarehouseName = r.Warehouse?.Name ?? "N/A",
                BatchId = r.BatchId,
                BatchCode = r.Batch?.BatchCode ?? "N/A",
                Quantity = r.Quantity,
                ExportedAt = r.ExportedAt,
                StaffName = r.ExportedByNavigation?.User?.Name ?? "N/A",
                Note = r.Note,
                DestinationNote = r.DestinationNote,
                
                // Thông tin sản phẩm từ Inventory
                InventoryName = r.Inventory?.Products?.FirstOrDefault()?.ProductName ?? "N/A",
                CoffeeType = r.Batch?.CoffeeType?.TypeName ?? "N/A",
                Quality = GetQualityFromEvaluation(r.Batch?.ProcessingBatchEvaluations?.FirstOrDefault()) ?? "Chưa đánh giá",
                Origin = r.Batch?.CoffeeType?.TypicalRegion ?? "Đắk Lắk, Việt Nam",
                ProductionDate = r.Batch?.CreatedAt,
                ExpiryDate = r.Batch?.CreatedAt?.AddYears(1), // Fake: 1 năm sau ngày tạo
                MoistureContent = GetMoistureFromParameters(r.Batch?.ProcessingBatchProgresses) ?? GetMoistureFromEvaluation(r.Batch?.ProcessingBatchEvaluations?.FirstOrDefault()) ?? 12.5,
                NetWeight = r.Quantity, // Sử dụng quantity của receipt
                
                // Thông tin đơn hàng liên kết
                OrderInfo = r.OutboundRequest?.OrderItem?.Order != null ? new
                {
                    OrderCode = r.OutboundRequest.OrderItem.Order.OrderCode ?? "N/A",
                    CustomerName = r.OutboundRequest.OrderItem.Order.CreatedByNavigation?.Name ?? "N/A",
                    OrderQuantity = r.OutboundRequest.OrderItem.Quantity ?? 0,
                    OrderUnit = "kg",
                    OrderStatus = r.OutboundRequest.OrderItem.Order.Status ?? "N/A"
                } : null,
                
                // Thông tin người tạo (staff hiện tại)
                CreatedByName = r.ExportedByNavigation?.User?.Name ?? "Hệ thống",
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt,
                
                // Chữ ký tự động (fake)
                InspectorSignature = GenerateFakeSignature("Nguyễn Văn A"), // Fake người kiểm tra
                StaffSignature = GenerateFakeSignature(r.ExportedByNavigation?.User?.Name ?? "Nhân viên"),
                RecipientSignature = GenerateFakeSignature("Khách hàng")
            };
        }

        // Helper methods để lấy dữ liệu từ ProcessingBatchEvaluation
        private static string? GetQualityFromEvaluation(ProcessingBatchEvaluation? evaluation)
        {
            if (evaluation == null) return null;
            
            // Lấy từ EvaluationResult hoặc TotalScore
            if (!string.IsNullOrEmpty(evaluation.EvaluationResult))
            {
                return evaluation.EvaluationResult;
            }
            
            // Nếu có TotalScore, chuyển đổi thành grade
            if (evaluation.TotalScore.HasValue)
            {
                var score = evaluation.TotalScore.Value;
                return score switch
                {
                    >= 90 => "Grade A+ (Specialty)",
                    >= 85 => "Grade A (Premium)",
                    >= 80 => "Grade B+ (Good)",
                    >= 75 => "Grade B (Standard)",
                    >= 70 => "Grade C (Acceptable)",
                    _ => "Grade D (Below Standard)"
                };
            }
            
            return null;
        }

        private static double? GetMoistureFromParameters(ICollection<ProcessingBatchProgress>? progresses)
        {
            if (progresses == null || !progresses.Any()) return null;
            
            // Tìm tất cả parameters có tên liên quan đến độ ẩm
            var moistureParams = progresses
                .SelectMany(p => p.ProcessingParameters ?? new List<ProcessingParameter>())
                .Where(param => param != null && !param.IsDeleted && 
                    (param.ParameterName?.ToLower().Contains("moisture") == true ||
                     param.ParameterName?.ToLower().Contains("humidity") == true ||
                     param.ParameterName?.ToLower().Contains("độ ẩm") == true ||
                     param.ParameterName?.ToLower().Contains("ẩm") == true))
                .OrderByDescending(param => param.RecordedAt ?? param.CreatedAt) // Lấy giá trị mới nhất
                .ToList();
            
            if (!moistureParams.Any()) return null;
            
            // Thử parse giá trị từ ParameterValue
            foreach (var param in moistureParams)
            {
                if (!string.IsNullOrEmpty(param.ParameterValue))
                {
                    // Loại bỏ ký tự % và các ký tự không phải số
                    var cleanValue = param.ParameterValue.Trim().Trim('%', ' ', '\t', '\n', '\r');
                    
                    if (double.TryParse(cleanValue, out var moisture))
                    {
                        return moisture;
                    }
                }
            }
            
            return null;
        }

        private static double? GetMoistureFromEvaluation(ProcessingBatchEvaluation? evaluation)
        {
            if (evaluation == null) return null;
            
            // Thử parse từ CriteriaSnapshot (JSON) hoặc Comments
            if (!string.IsNullOrEmpty(evaluation.CriteriaSnapshot))
            {
                try
                {
                    // Tìm moisture content trong JSON
                    if (evaluation.CriteriaSnapshot.Contains("MoisturePercent") || 
                        evaluation.CriteriaSnapshot.Contains("moisture"))
                    {
                        // Simple parsing - có thể cải thiện bằng JSON deserializer
                        var lines = evaluation.CriteriaSnapshot.Split('\n');
                        foreach (var line in lines)
                        {
                            if (line.Contains("MoisturePercent") || line.Contains("moisture"))
                            {
                                var parts = line.Split(':');
                                if (parts.Length > 1 && double.TryParse(parts[1].Trim().Trim('"', '}'), out var moisture))
                                {
                                    return moisture;
                                }
                            }
                        }
                    }
                }
                catch
                {
                    // Ignore parsing errors
                }
            }
            
            // Thử parse từ Comments
            if (!string.IsNullOrEmpty(evaluation.Comments))
            {
                var comments = evaluation.Comments.ToLower();
                if (comments.Contains("moisture") || comments.Contains("độ ẩm"))
                {
                    // Simple regex-like parsing
                    var words = comments.Split(' ', ',', ';', ':');
                    for (int i = 0; i < words.Length - 1; i++)
                    {
                        if ((words[i].Contains("moisture") || words[i].Contains("ẩm")) && 
                            double.TryParse(words[i + 1].Trim('%'), out var moisture))
                        {
                            return moisture;
                        }
                    }
                }
            }
            
            return null;
        }

        // Helper method để tạo chữ ký fake
        private static string GenerateFakeSignature(string name)
        {
            if (string.IsNullOrEmpty(name)) return "Ký tên";
            
            // Tạo chữ ký fake dựa trên tên
            var words = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (words.Length == 0) return "Ký tên";
            
            // Lấy chữ cái đầu của từng từ
            var initials = string.Join("", words.Select(w => w.Length > 0 ? w[0].ToString().ToUpper() : ""));
            
            // Tạo chữ ký fake với style
            return $"✍️ {initials}";
        }
    }
}
