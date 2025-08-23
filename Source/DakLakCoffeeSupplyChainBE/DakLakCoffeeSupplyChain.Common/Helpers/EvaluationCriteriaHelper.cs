using System;
using System.Collections.Generic;
using System.Linq;

namespace DakLakCoffeeSupplyChain.Common.Helpers
{
    public class EvaluationCriteria
    {
        /// <summary>
        /// ID tiêu chí
        /// </summary>
        public string CriteriaId { get; set; } = string.Empty;
        
        /// <summary>
        /// Tên tiêu chí
        /// </summary>
        public string CriteriaName { get; set; } = string.Empty;
        
        /// <summary>
        /// Loại tiêu chí: Physical, Chemical, Visual, Quality
        /// </summary>
        public string CriteriaType { get; set; } = string.Empty;
        
        /// <summary>
        /// Giá trị tối thiểu
        /// </summary>
        public decimal? MinValue { get; set; }
        
        /// <summary>
        /// Giá trị tối đa
        /// </summary>
        public decimal? MaxValue { get; set; }
        
        /// <summary>
        /// Giá trị mục tiêu
        /// </summary>
        public decimal? TargetValue { get; set; }
        
        /// <summary>
        /// Đơn vị đo
        /// </summary>
        public string Unit { get; set; } = string.Empty;
        
        /// <summary>
        /// Trọng số đánh giá (0-1)
        /// </summary>
        public decimal Weight { get; set; } = 1.0m;
        
        /// <summary>
        /// Có bắt buộc hay không
        /// </summary>
        public bool IsRequired { get; set; } = true;
        
        /// <summary>
        /// Mô tả chi tiết
        /// </summary>
        public string Description { get; set; } = string.Empty;
    }

    public class FailureReason
    {
        /// <summary>
        /// ID lý do
        /// </summary>
        public string ReasonId { get; set; } = string.Empty;
        
        /// <summary>
        /// Mã lý do
        /// </summary>
        public string ReasonCode { get; set; } = string.Empty;
        
        /// <summary>
        /// Tên lý do
        /// </summary>
        public string ReasonName { get; set; } = string.Empty;
        
        /// <summary>
        /// Danh mục: Quality, Process, Equipment, Safety
        /// </summary>
        public string Category { get; set; } = string.Empty;
        
        /// <summary>
        /// Mức độ nghiêm trọng (1-5)
        /// </summary>
        public int SeverityLevel { get; set; } = 1;
        
        /// <summary>
        /// Mô tả chi tiết
        /// </summary>
        public string Description { get; set; } = string.Empty;
    }

    public static class EvaluationCriteriaHelper
    {
        /// <summary>
        /// Lấy tiêu chí đánh giá cho stage cụ thể
        /// </summary>
        /// <param name="stageCode">Mã stage: harvest, drying, hulling, grading</param>
        /// <returns>Danh sách tiêu chí</returns>
        public static List<EvaluationCriteria> GetCriteriaForStage(string stageCode)
        {
            return stageCode.ToLower() switch
            {
                "harvest" => GetHarvestCriteria(),
                "drying" => GetDryingCriteria(),
                "hulling" => GetHullingCriteria(),
                "grading" => GetGradingCriteria(),
                "fermentation" => GetFermentationCriteria(),
                "washing" => GetWashingCriteria(),
                "sorting" => GetSortingCriteria(),
                "roasting" => GetRoastingCriteria(),
                "packaging" => GetPackagingCriteria(),
                "storage" => GetStorageCriteria(),
                "transport" => GetTransportCriteria(),
                "quality_check" => GetQualityCheckCriteria(),
                "pulping" => GetPulpingCriteria(),
                "demucilaging" => GetDemucilagingCriteria(),
                "polishing" => GetPolishingCriteria(),
                "cupping" => GetCuppingCriteria(),
                "blending" => GetBlendingCriteria(),
                "grinding" => GetGrindingCriteria(),
                "instant_processing" => GetInstantProcessingCriteria(),
                "carbonic-ferment" => GetCarbonicFermentCriteria(),
                _ => new List<EvaluationCriteria>()
            };
        }

        /// <summary>
        /// Lấy lý do không đạt cho stage cụ thể
        /// </summary>
        /// <param name="stageCode">Mã stage</param>
        /// <returns>Danh sách lý do</returns>
        public static List<FailureReason> GetFailureReasonsForStage(string stageCode)
        {
            return stageCode.ToLower() switch
            {
                "harvest" => GetHarvestFailureReasons(),
                "drying" => GetDryingFailureReasons(),
                "hulling" => GetHullingFailureReasons(),
                "grading" => GetGradingFailureReasons(),
                "fermentation" => GetFermentationFailureReasons(),
                "washing" => GetWashingFailureReasons(),
                "sorting" => GetSortingFailureReasons(),
                "roasting" => GetRoastingFailureReasons(),
                "packaging" => GetPackagingFailureReasons(),
                "storage" => GetStorageFailureReasons(),
                "transport" => GetTransportFailureReasons(),
                "quality_check" => GetQualityCheckFailureReasons(),
                "pulping" => GetPulpingFailureReasons(),
                "demucilaging" => GetDemucilagingFailureReasons(),
                "polishing" => GetPolishingFailureReasons(),
                "cupping" => GetCuppingFailureReasons(),
                "blending" => GetBlendingFailureReasons(),
                "grinding" => GetGrindingFailureReasons(),
                "instant_processing" => GetInstantProcessingFailureReasons(),
                "carbonic-ferment" => GetCarbonicFermentFailureReasons(),
                _ => new List<FailureReason>()
            };
        }

        /// <summary>
        /// Tiêu chí đánh giá thu hoạch
        /// </summary>
        private static List<EvaluationCriteria> GetHarvestCriteria()
        {
            return new List<EvaluationCriteria>
            {
                new EvaluationCriteria
                {
                    CriteriaId = "HARVEST_001",
                    CriteriaName = "Độ chín của quả",
                    CriteriaType = "Visual",
                    MinValue = 80,
                    MaxValue = 100,
                    TargetValue = 95,
                    Unit = "%",
                    Weight = 0.3m,
                    Description = "Tỷ lệ quả chín đỏ, không có quả xanh"
                },
                new EvaluationCriteria
                {
                    CriteriaId = "HARVEST_002",
                    CriteriaName = "Kích thước hạt",
                    CriteriaType = "Physical",
                    MinValue = 15,
                    MaxValue = 20,
                    TargetValue = 17,
                    Unit = "mm",
                    Weight = 0.2m,
                    Description = "Đường kính hạt cà phê"
                },
                new EvaluationCriteria
                {
                    CriteriaId = "HARVEST_003",
                    CriteriaName = "Tỷ lệ hạt lỗi",
                    CriteriaType = "Quality",
                    MinValue = 0,
                    MaxValue = 5,
                    TargetValue = 2,
                    Unit = "%",
                    Weight = 0.25m,
                    Description = "Hạt bị sâu, mốc, vỡ"
                },
                new EvaluationCriteria
                {
                    CriteriaId = "HARVEST_004",
                    CriteriaName = "Độ ẩm",
                    CriteriaType = "Chemical",
                    MinValue = 60,
                    MaxValue = 70,
                    TargetValue = 65,
                    Unit = "%",
                    Weight = 0.25m,
                    Description = "Độ ẩm của quả cà phê"
                }
            };
        }

        /// <summary>
        /// Tiêu chí đánh giá phơi khô
        /// </summary>
        private static List<EvaluationCriteria> GetDryingCriteria()
        {
            return new List<EvaluationCriteria>
            {
                new EvaluationCriteria
                {
                    CriteriaId = "DRYING_001",
                    CriteriaName = "Độ ẩm cuối",
                    CriteriaType = "Chemical",
                    MinValue = 10,
                    MaxValue = 12,
                    TargetValue = 11,
                    Unit = "%",
                    Weight = 0.4m,
                    Description = "Độ ẩm sau khi phơi"
                },
                new EvaluationCriteria
                {
                    CriteriaId = "DRYING_002",
                    CriteriaName = "Nhiệt độ phơi",
                    CriteriaType = "Physical",
                    MinValue = 25,
                    MaxValue = 35,
                    TargetValue = 30,
                    Unit = "°C",
                    Weight = 0.3m,
                    Description = "Nhiệt độ môi trường phơi"
                },
                new EvaluationCriteria
                {
                    CriteriaId = "DRYING_003",
                    CriteriaName = "Thời gian phơi",
                    CriteriaType = "Process",
                    MinValue = 7,
                    MaxValue = 25,
                    TargetValue = 15,
                    Unit = "ngày",
                    Weight = 0.3m,
                    Description = "Số ngày phơi"
                }
            };
        }

        /// <summary>
        /// Tiêu chí đánh giá xay vỏ
        /// </summary>
        private static List<EvaluationCriteria> GetHullingCriteria()
        {
            return new List<EvaluationCriteria>
            {
                new EvaluationCriteria
                {
                    CriteriaId = "HULLING_001",
                    CriteriaName = "Tỷ lệ hạt vỡ",
                    CriteriaType = "Quality",
                    MinValue = 0,
                    MaxValue = 3,
                    TargetValue = 1,
                    Unit = "%",
                    Weight = 0.4m,
                    Description = "Hạt bị vỡ trong quá trình xay"
                },
                new EvaluationCriteria
                {
                    CriteriaId = "HULLING_002",
                    CriteriaName = "Độ sạch vỏ",
                    CriteriaType = "Visual",
                    MinValue = 95,
                    MaxValue = 100,
                    TargetValue = 98,
                    Unit = "%",
                    Weight = 0.3m,
                    Description = "Tỷ lệ vỏ được tách sạch"
                },
                new EvaluationCriteria
                {
                    CriteriaId = "HULLING_003",
                    CriteriaName = "Kích thước hạt đồng đều",
                    CriteriaType = "Physical",
                    MinValue = 85,
                    MaxValue = 100,
                    TargetValue = 95,
                    Unit = "%",
                    Weight = 0.3m,
                    Description = "Tỷ lệ hạt có kích thước đồng đều"
                }
            };
        }

        /// <summary>
        /// Tiêu chí đánh giá phân loại
        /// </summary>
        private static List<EvaluationCriteria> GetGradingCriteria()
        {
            return new List<EvaluationCriteria>
            {
                new EvaluationCriteria
                {
                    CriteriaId = "GRADING_001",
                    CriteriaName = "Độ đồng đều kích thước",
                    CriteriaType = "Physical",
                    MinValue = 90,
                    MaxValue = 100,
                    TargetValue = 95,
                    Unit = "%",
                    Weight = 0.35m,
                    Description = "Tỷ lệ hạt cùng kích cỡ"
                },
                new EvaluationCriteria
                {
                    CriteriaId = "GRADING_002",
                    CriteriaName = "Màu sắc đồng đều",
                    CriteriaType = "Visual",
                    MinValue = 85,
                    MaxValue = 100,
                    TargetValue = 95,
                    Unit = "%",
                    Weight = 0.25m,
                    Description = "Tỷ lệ hạt cùng màu sắc"
                },
                new EvaluationCriteria
                {
                    CriteriaId = "GRADING_003",
                    CriteriaName = "Tỷ lệ hạt lỗi",
                    CriteriaType = "Quality",
                    MinValue = 0,
                    MaxValue = 2,
                    TargetValue = 0.5m,
                    Unit = "%",
                    Weight = 0.4m,
                    Description = "Hạt bị đen, mốc, sâu"
                }
            };
        }

        /// <summary>
        /// Tiêu chí đánh giá lên men
        /// </summary>
        private static List<EvaluationCriteria> GetFermentationCriteria()
        {
            return new List<EvaluationCriteria>
            {
                new EvaluationCriteria
                {
                    CriteriaId = "FERMENT_001",
                    CriteriaName = "Thời gian lên men",
                    CriteriaType = "Process",
                    MinValue = 12,
                    MaxValue = 48,
                    TargetValue = 24,
                    Unit = "giờ",
                    Weight = 0.4m,
                    Description = "Thời gian lên men"
                },
                new EvaluationCriteria
                {
                    CriteriaId = "FERMENT_002",
                    CriteriaName = "Nhiệt độ lên men",
                    CriteriaType = "Physical",
                    MinValue = 18,
                    MaxValue = 25,
                    TargetValue = 22,
                    Unit = "°C",
                    Weight = 0.3m,
                    Description = "Nhiệt độ môi trường lên men"
                },
                new EvaluationCriteria
                {
                    CriteriaId = "FERMENT_003",
                    CriteriaName = "pH cuối",
                    CriteriaType = "Chemical",
                    MinValue = 4.5m,
                    MaxValue = 5.5m,
                    TargetValue = 5.0m,
                    Unit = "",
                    Weight = 0.3m,
                    Description = "Độ pH sau lên men"
                }
            };
        }

        /// <summary>
        /// Tiêu chí đánh giá rửa
        /// </summary>
        private static List<EvaluationCriteria> GetWashingCriteria()
        {
            return new List<EvaluationCriteria>
            {
                new EvaluationCriteria
                {
                    CriteriaId = "WASH_001",
                    CriteriaName = "Độ sạch bề mặt",
                    CriteriaType = "Visual",
                    MinValue = 95,
                    MaxValue = 100,
                    TargetValue = 98,
                    Unit = "%",
                    Weight = 0.5m,
                    Description = "Tỷ lệ hạt sạch bề mặt"
                },
                new EvaluationCriteria
                {
                    CriteriaId = "WASH_002",
                    CriteriaName = "Độ ẩm sau rửa",
                    CriteriaType = "Chemical",
                    MinValue = 50,
                    MaxValue = 60,
                    TargetValue = 55,
                    Unit = "%",
                    Weight = 0.5m,
                    Description = "Độ ẩm hạt sau rửa"
                }
            };
        }

                /// <summary>
        /// Tiêu chí đánh giá phân loại chi tiết
        /// </summary>
        private static List<EvaluationCriteria> GetSortingCriteria()
        {
            return new List<EvaluationCriteria>
            {
                new EvaluationCriteria
                {
                    CriteriaId = "SORTING_001",
                    CriteriaName = "Độ chính xác phân loại",
                    CriteriaType = "Quality",
                    MinValue = 95,
                    MaxValue = 100,
                    TargetValue = 98,
                    Unit = "%",
                    Weight = 0.4m,
                    Description = "Tỷ lệ hạt được phân loại đúng"
                },
                new EvaluationCriteria
                {
                    CriteriaId = "SORTING_002",
                    CriteriaName = "Tốc độ phân loại",
                    CriteriaType = "Process",
                    MinValue = 100,
                    MaxValue = 500,
                    TargetValue = 300,
                    Unit = "kg/giờ",
                    Weight = 0.3m,
                    Description = "Năng suất phân loại"
                },
                new EvaluationCriteria
                {
                    CriteriaId = "SORTING_003",
                    CriteriaName = "Tỷ lệ hạt bị loại",
                    CriteriaType = "Quality",
                    MinValue = 0,
                    MaxValue = 5,
                    TargetValue = 2,
                    Unit = "%",
                    Weight = 0.3m,
                    Description = "Hạt không đạt tiêu chuẩn bị loại bỏ"
                }
            };
        }

        /// <summary>
        /// Tiêu chí đánh giá rang cà phê
        /// </summary>
        private static List<EvaluationCriteria> GetRoastingCriteria()
        {
            return new List<EvaluationCriteria>
            {
                new EvaluationCriteria
                {
                    CriteriaId = "ROASTING_001",
                    CriteriaName = "Nhiệt độ rang",
                    CriteriaType = "Physical",
                    MinValue = 180,
                    MaxValue = 250,
                    TargetValue = 220,
                    Unit = "°C",
                    Weight = 0.35m,
                    Description = "Nhiệt độ rang cà phê"
                },
                new EvaluationCriteria
                {
                    CriteriaId = "ROASTING_002",
                    CriteriaName = "Thời gian rang",
                    CriteriaType = "Process",
                    MinValue = 12,
                    MaxValue = 20,
                    TargetValue = 16,
                    Unit = "phút",
                    Weight = 0.3m,
                    Description = "Thời gian rang"
                },
                new EvaluationCriteria
                {
                    CriteriaId = "ROASTING_003",
                    CriteriaName = "Độ chín đều",
                    CriteriaType = "Visual",
                    MinValue = 90,
                    MaxValue = 100,
                    TargetValue = 95,
                    Unit = "%",
                    Weight = 0.35m,
                    Description = "Tỷ lệ hạt rang chín đều"
                }
            };
        }

        /// <summary>
        /// Tiêu chí đánh giá đóng gói
        /// </summary>
        private static List<EvaluationCriteria> GetPackagingCriteria()
        {
            return new List<EvaluationCriteria>
            {
                new EvaluationCriteria
                {
                    CriteriaId = "PACKAGING_001",
                    CriteriaName = "Độ kín khí",
                    CriteriaType = "Quality",
                    MinValue = 95,
                    MaxValue = 100,
                    TargetValue = 98,
                    Unit = "%",
                    Weight = 0.4m,
                    Description = "Độ kín khí của bao bì"
                },
                new EvaluationCriteria
                {
                    CriteriaId = "PACKAGING_002",
                    CriteriaName = "Trọng lượng đóng gói",
                    CriteriaType = "Physical",
                    MinValue = 98,
                    MaxValue = 102,
                    TargetValue = 100,
                    Unit = "%",
                    Weight = 0.3m,
                    Description = "Độ chính xác trọng lượng đóng gói"
                },
                new EvaluationCriteria
                {
                    CriteriaId = "PACKAGING_003",
                    CriteriaName = "Chất lượng bao bì",
                    CriteriaType = "Visual",
                    MinValue = 90,
                    MaxValue = 100,
                    TargetValue = 95,
                    Unit = "%",
                    Weight = 0.3m,
                    Description = "Bao bì không bị rách, móp méo"
                }
            };
        }

        /// <summary>
        /// Tiêu chí đánh giá bảo quản
        /// </summary>
        private static List<EvaluationCriteria> GetStorageCriteria()
        {
            return new List<EvaluationCriteria>
            {
                new EvaluationCriteria
                {
                    CriteriaId = "STORAGE_001",
                    CriteriaName = "Nhiệt độ bảo quản",
                    CriteriaType = "Physical",
                    MinValue = 15,
                    MaxValue = 25,
                    TargetValue = 20,
                    Unit = "°C",
                    Weight = 0.4m,
                    Description = "Nhiệt độ môi trường bảo quản"
                },
                new EvaluationCriteria
                {
                    CriteriaId = "STORAGE_002",
                    CriteriaName = "Độ ẩm bảo quản",
                    CriteriaType = "Chemical",
                    MinValue = 50,
                    MaxValue = 65,
                    TargetValue = 60,
                    Unit = "%",
                    Weight = 0.35m,
                    Description = "Độ ẩm môi trường bảo quản"
                },
                new EvaluationCriteria
                {
                    CriteriaId = "STORAGE_003",
                    CriteriaName = "Thời gian bảo quản",
                    CriteriaType = "Process",
                    MinValue = 0,
                    MaxValue = 365,
                    TargetValue = 180,
                    Unit = "ngày",
                    Weight = 0.25m,
                    Description = "Thời gian bảo quản tối đa"
                }
            };
        }

        /// <summary>
        /// Tiêu chí đánh giá vận chuyển
        /// </summary>
        private static List<EvaluationCriteria> GetTransportCriteria()
        {
            return new List<EvaluationCriteria>
            {
                new EvaluationCriteria
                {
                    CriteriaId = "TRANSPORT_001",
                    CriteriaName = "Nhiệt độ vận chuyển",
                    CriteriaType = "Physical",
                    MinValue = 15,
                    MaxValue = 30,
                    TargetValue = 22,
                    Unit = "°C",
                    Weight = 0.4m,
                    Description = "Nhiệt độ trong quá trình vận chuyển"
                },
                new EvaluationCriteria
                {
                    CriteriaId = "TRANSPORT_002",
                    CriteriaName = "Thời gian vận chuyển",
                    CriteriaType = "Process",
                    MinValue = 0,
                    MaxValue = 72,
                    TargetValue = 24,
                    Unit = "giờ",
                    Weight = 0.3m,
                    Description = "Thời gian vận chuyển"
                },
                new EvaluationCriteria
                {
                    CriteriaId = "TRANSPORT_003",
                    CriteriaName = "Độ an toàn",
                    CriteriaType = "Quality",
                    MinValue = 95,
                    MaxValue = 100,
                    TargetValue = 98,
                    Unit = "%",
                    Weight = 0.3m,
                    Description = "Tỷ lệ hàng hóa an toàn khi đến nơi"
                }
            };
        }

        /// <summary>
        /// Tiêu chí đánh giá kiểm tra chất lượng
        /// </summary>
        private static List<EvaluationCriteria> GetQualityCheckCriteria()
        {
            return new List<EvaluationCriteria>
            {
                new EvaluationCriteria
                {
                    CriteriaId = "QUALITY_001",
                    CriteriaName = "Độ chính xác kiểm tra",
                    CriteriaType = "Quality",
                    MinValue = 98,
                    MaxValue = 100,
                    TargetValue = 99,
                    Unit = "%",
                    Weight = 0.4m,
                    Description = "Độ chính xác của quá trình kiểm tra"
                },
                new EvaluationCriteria
                {
                    CriteriaId = "QUALITY_002",
                    CriteriaName = "Thời gian kiểm tra",
                    CriteriaType = "Process",
                    MinValue = 0,
                    MaxValue = 4,
                    TargetValue = 2,
                    Unit = "giờ",
                    Weight = 0.3m,
                    Description = "Thời gian hoàn thành kiểm tra"
                },
                new EvaluationCriteria
                {
                    CriteriaId = "QUALITY_003",
                    CriteriaName = "Tỷ lệ mẫu kiểm tra",
                    CriteriaType = "Process",
                    MinValue = 5,
                    MaxValue = 20,
                    TargetValue = 10,
                    Unit = "%",
                    Weight = 0.3m,
                    Description = "Tỷ lệ mẫu được kiểm tra so với tổng số"
                }
            };
        }

        /// <summary>
        /// Tiêu chí đánh giá tách vỏ quả (pulping)
        /// </summary>
        private static List<EvaluationCriteria> GetPulpingCriteria()
        {
            return new List<EvaluationCriteria>
            {
                new EvaluationCriteria
                {
                    CriteriaId = "PULPING_001",
                    CriteriaName = "Tỷ lệ tách vỏ thành công",
                    CriteriaType = "Quality",
                    MinValue = 95,
                    MaxValue = 100,
                    TargetValue = 98,
                    Unit = "%",
                    Weight = 0.4m,
                    Description = "Tỷ lệ quả được tách vỏ hoàn toàn"
                },
                new EvaluationCriteria
                {
                    CriteriaId = "PULPING_002",
                    CriteriaName = "Tỷ lệ hạt bị tổn thương",
                    CriteriaType = "Quality",
                    MinValue = 0,
                    MaxValue = 3,
                    TargetValue = 1,
                    Unit = "%",
                    Weight = 0.35m,
                    Description = "Hạt bị vỡ, nứt trong quá trình tách vỏ"
                },
                new EvaluationCriteria
                {
                    CriteriaId = "PULPING_003",
                    CriteriaName = "Năng suất tách vỏ",
                    CriteriaType = "Process",
                    MinValue = 200,
                    MaxValue = 800,
                    TargetValue = 500,
                    Unit = "kg/giờ",
                    Weight = 0.25m,
                    Description = "Khối lượng quả được tách vỏ mỗi giờ"
                }
            };
        }

        /// <summary>
        /// Tiêu chí đánh giá loại bỏ chất nhầy (demucilaging)
        /// </summary>
        private static List<EvaluationCriteria> GetDemucilagingCriteria()
        {
            return new List<EvaluationCriteria>
            {
                new EvaluationCriteria
                {
                    CriteriaId = "DEMUCILAGING_001",
                    CriteriaName = "Độ sạch chất nhầy",
                    CriteriaType = "Visual",
                    MinValue = 90,
                    MaxValue = 100,
                    TargetValue = 95,
                    Unit = "%",
                    Weight = 0.4m,
                    Description = "Tỷ lệ chất nhầy được loại bỏ"
                },
                new EvaluationCriteria
                {
                    CriteriaId = "DEMUCILAGING_002",
                    CriteriaName = "Thời gian xử lý",
                    CriteriaType = "Process",
                    MinValue = 2,
                    MaxValue = 8,
                    TargetValue = 5,
                    Unit = "giờ",
                    Weight = 0.3m,
                    Description = "Thời gian để loại bỏ chất nhầy"
                },
                new EvaluationCriteria
                {
                    CriteriaId = "DEMUCILAGING_003",
                    CriteriaName = "Tỷ lệ hạt bị tổn thương",
                    CriteriaType = "Quality",
                    MinValue = 0,
                    MaxValue = 2,
                    TargetValue = 0.5m,
                    Unit = "%",
                    Weight = 0.3m,
                    Description = "Hạt bị tổn thương trong quá trình xử lý"
                }
            };
        }

        /// <summary>
        /// Tiêu chí đánh giá đánh bóng hạt (polishing)
        /// </summary>
        private static List<EvaluationCriteria> GetPolishingCriteria()
        {
            return new List<EvaluationCriteria>
            {
                new EvaluationCriteria
                {
                    CriteriaId = "POLISHING_001",
                    CriteriaName = "Độ bóng bề mặt",
                    CriteriaType = "Visual",
                    MinValue = 85,
                    MaxValue = 100,
                    TargetValue = 95,
                    Unit = "%",
                    Weight = 0.4m,
                    Description = "Tỷ lệ hạt có bề mặt bóng, sạch"
                },
                new EvaluationCriteria
                {
                    CriteriaId = "POLISHING_002",
                    CriteriaName = "Tỷ lệ hạt bị mài mòn quá mức",
                    CriteriaType = "Quality",
                    MinValue = 0,
                    MaxValue = 2,
                    TargetValue = 0.5m,
                    Unit = "%",
                    Weight = 0.35m,
                    Description = "Hạt bị mài mòn quá mức làm mất chất lượng"
                },
                new EvaluationCriteria
                {
                    CriteriaId = "POLISHING_003",
                    CriteriaName = "Năng suất đánh bóng",
                    CriteriaType = "Process",
                    MinValue = 150,
                    MaxValue = 600,
                    TargetValue = 400,
                    Unit = "kg/giờ",
                    Weight = 0.25m,
                    Description = "Khối lượng hạt được đánh bóng mỗi giờ"
                }
            };
        }

        /// <summary>
        /// Tiêu chí đánh giá kiểm tra hương vị (cupping)
        /// </summary>
        private static List<EvaluationCriteria> GetCuppingCriteria()
        {
            return new List<EvaluationCriteria>
            {
                new EvaluationCriteria
                {
                    CriteriaId = "CUPPING_001",
                    CriteriaName = "Điểm hương vị tổng thể",
                    CriteriaType = "Quality",
                    MinValue = 80,
                    MaxValue = 100,
                    TargetValue = 85,
                    Unit = "điểm",
                    Weight = 0.4m,
                    Description = "Điểm đánh giá hương vị tổng thể (thang 100)"
                },
                new EvaluationCriteria
                {
                    CriteriaId = "CUPPING_002",
                    CriteriaName = "Độ chua (Acidity)",
                    CriteriaType = "Quality",
                    MinValue = 6,
                    MaxValue = 10,
                    TargetValue = 8,
                    Unit = "điểm",
                    Weight = 0.3m,
                    Description = "Điểm đánh giá độ chua (thang 10)"
                },
                new EvaluationCriteria
                {
                    CriteriaId = "CUPPING_003",
                    CriteriaName = "Độ đắng (Bitterness)",
                    CriteriaType = "Quality",
                    MinValue = 6,
                    MaxValue = 10,
                    TargetValue = 8,
                    Unit = "điểm",
                    Weight = 0.3m,
                    Description = "Điểm đánh giá độ đắng (thang 10)"
                }
            };
        }

        /// <summary>
        /// Tiêu chí đánh giá pha trộn (blending)
        /// </summary>
        private static List<EvaluationCriteria> GetBlendingCriteria()
        {
            return new List<EvaluationCriteria>
            {
                new EvaluationCriteria
                {
                    CriteriaId = "BLENDING_001",
                    CriteriaName = "Độ đồng đều pha trộn",
                    CriteriaType = "Quality",
                    MinValue = 95,
                    MaxValue = 100,
                    TargetValue = 98,
                    Unit = "%",
                    Weight = 0.4m,
                    Description = "Tỷ lệ các loại hạt được pha trộn đều"
                },
                new EvaluationCriteria
                {
                    CriteriaId = "BLENDING_002",
                    CriteriaName = "Độ chính xác tỷ lệ",
                    CriteriaType = "Quality",
                    MinValue = 98,
                    MaxValue = 102,
                    TargetValue = 100,
                    Unit = "%",
                    Weight = 0.35m,
                    Description = "Độ chính xác tỷ lệ pha trộn theo công thức"
                },
                new EvaluationCriteria
                {
                    CriteriaId = "BLENDING_003",
                    CriteriaName = "Thời gian pha trộn",
                    CriteriaType = "Process",
                    MinValue = 10,
                    MaxValue = 30,
                    TargetValue = 20,
                    Unit = "phút",
                    Weight = 0.25m,
                    Description = "Thời gian để pha trộn hoàn chỉnh"
                }
            };
        }

        /// <summary>
        /// Tiêu chí đánh giá xay cà phê (grinding)
        /// </summary>
        private static List<EvaluationCriteria> GetGrindingCriteria()
        {
            return new List<EvaluationCriteria>
            {
                new EvaluationCriteria
                {
                    CriteriaId = "GRINDING_001",
                    CriteriaName = "Độ mịn đồng đều",
                    CriteriaType = "Physical",
                    MinValue = 90,
                    MaxValue = 100,
                    TargetValue = 95,
                    Unit = "%",
                    Weight = 0.4m,
                    Description = "Tỷ lệ hạt có độ mịn đồng đều"
                },
                new EvaluationCriteria
                {
                    CriteriaId = "GRINDING_002",
                    CriteriaName = "Nhiệt độ xay",
                    CriteriaType = "Physical",
                    MinValue = 15,
                    MaxValue = 35,
                    TargetValue = 25,
                    Unit = "°C",
                    Weight = 0.35m,
                    Description = "Nhiệt độ trong quá trình xay"
                },
                new EvaluationCriteria
                {
                    CriteriaId = "GRINDING_003",
                    CriteriaName = "Năng suất xay",
                    CriteriaType = "Process",
                    MinValue = 50,
                    MaxValue = 200,
                    TargetValue = 120,
                    Unit = "kg/giờ",
                    Weight = 0.25m,
                    Description = "Khối lượng cà phê được xay mỗi giờ"
                }
            };
        }

        /// <summary>
        /// Tiêu chí đánh giá chế biến cà phê hòa tan (instant processing)
        /// </summary>
        private static List<EvaluationCriteria> GetInstantProcessingCriteria()
        {
            return new List<EvaluationCriteria>
            {
                new EvaluationCriteria
                {
                    CriteriaId = "INSTANT_001",
                    CriteriaName = "Độ hòa tan",
                    CriteriaType = "Quality",
                    MinValue = 95,
                    MaxValue = 100,
                    TargetValue = 98,
                    Unit = "%",
                    Weight = 0.4m,
                    Description = "Tỷ lệ cà phê hòa tan hoàn toàn trong nước"
                },
                new EvaluationCriteria
                {
                    CriteriaId = "INSTANT_002",
                    CriteriaName = "Hàm lượng caffein",
                    CriteriaType = "Chemical",
                    MinValue = 1.5m,
                    MaxValue = 3.5m,
                    TargetValue = 2.5m,
                    Unit = "%",
                    Weight = 0.35m,
                    Description = "Hàm lượng caffein trong sản phẩm cuối"
                },
                new EvaluationCriteria
                {
                    CriteriaId = "INSTANT_003",
                    CriteriaName = "Thời gian bảo quản",
                    CriteriaType = "Process",
                    MinValue = 12,
                    MaxValue = 36,
                    TargetValue = 24,
                    Unit = "tháng",
                    Weight = 0.25m,
                    Description = "Thời gian bảo quản tối đa"
                }
            };
        }

        /// <summary>
        /// Tiêu chí đánh giá lên men carbonic
        /// </summary>
        private static List<EvaluationCriteria> GetCarbonicFermentCriteria()
        {
            return new List<EvaluationCriteria>
            {
                new EvaluationCriteria
                {
                    CriteriaId = "CARBONIC_FERMENT_001",
                    CriteriaName = "Thời gian lên men carbonic",
                    CriteriaType = "Process",
                    MinValue = 12,
                    MaxValue = 48,
                    TargetValue = 24,
                    Unit = "giờ",
                    Weight = 0.4m,
                    Description = "Thời gian lên men carbonic"
                },
                new EvaluationCriteria
                {
                    CriteriaId = "CARBONIC_FERMENT_002",
                    CriteriaName = "Nhiệt độ lên men carbonic",
                    CriteriaType = "Physical",
                    MinValue = 18,
                    MaxValue = 25,
                    TargetValue = 22,
                    Unit = "°C",
                    Weight = 0.3m,
                    Description = "Nhiệt độ môi trường lên men carbonic"
                },
                new EvaluationCriteria
                {
                    CriteriaId = "CARBONIC_FERMENT_003",
                    CriteriaName = "pH cuối",
                    CriteriaType = "Chemical",
                    MinValue = 4.5m,
                    MaxValue = 5.5m,
                    TargetValue = 5.0m,
                    Unit = "",
                    Weight = 0.3m,
                    Description = "Độ pH sau lên men carbonic"
                }
            };
        }

        /// <summary>
        /// Lý do không đạt thu hoạch
        /// </summary>
        private static List<FailureReason> GetHarvestFailureReasons()
        {
            return new List<FailureReason>
            {
                new FailureReason
                {
                    ReasonId = "HARVEST_FAIL_001",
                    ReasonCode = "UNRIPE_FRUITS",
                    ReasonName = "Quả chưa chín đủ",
                    Category = "Quality",
                    SeverityLevel = 3,
                    Description = "Tỷ lệ quả xanh quá cao (>20%)"
                },
                new FailureReason
                {
                    ReasonId = "HARVEST_FAIL_002",
                    ReasonCode = "DAMAGED_FRUITS",
                    ReasonName = "Quả bị hư hỏng",
                    Category = "Quality",
                    SeverityLevel = 4,
                    Description = "Quả bị sâu, mốc, vỡ"
                },
                new FailureReason
                {
                    ReasonId = "HARVEST_FAIL_003",
                    ReasonCode = "WRONG_HARVEST_TIME",
                    ReasonName = "Thời điểm thu hoạch không phù hợp",
                    Category = "Process",
                    SeverityLevel = 2,
                    Description = "Thu hoạch quá sớm hoặc quá muộn"
                }
            };
        }

        /// <summary>
        /// Lý do không đạt phơi khô
        /// </summary>
        private static List<FailureReason> GetDryingFailureReasons()
        {
            return new List<FailureReason>
            {
                new FailureReason
                {
                    ReasonId = "DRYING_FAIL_001",
                    ReasonCode = "HIGH_MOISTURE",
                    ReasonName = "Độ ẩm quá cao",
                    Category = "Quality",
                    SeverityLevel = 4,
                    Description = "Độ ẩm >12%, cần phơi thêm"
                },
                new FailureReason
                {
                    ReasonId = "DRYING_FAIL_002",
                    ReasonCode = "OVER_DRYING",
                    ReasonName = "Phơi quá khô",
                    Category = "Quality",
                    SeverityLevel = 3,
                    Description = "Độ ẩm <10%, hạt bị khô quá"
                },
                new FailureReason
                {
                    ReasonId = "DRYING_FAIL_003",
                    ReasonCode = "INSUFFICIENT_DRYING_TIME",
                    ReasonName = "Thời gian phơi không đủ",
                    Category = "Process",
                    SeverityLevel = 3,
                    Description = "Phơi chưa đủ ngày"
                },
                new FailureReason
                {
                    ReasonId = "DRYING_FAIL_004",
                    ReasonCode = "POOR_DRYING_CONDITIONS",
                    ReasonName = "Điều kiện phơi không tốt",
                    Category = "Process",
                    SeverityLevel = 2,
                    Description = "Thời tiết ẩm, thiếu nắng"
                }
            };
        }

        /// <summary>
        /// Lý do không đạt xay vỏ
        /// </summary>
        private static List<FailureReason> GetHullingFailureReasons()
        {
            return new List<FailureReason>
            {
                new FailureReason
                {
                    ReasonId = "HULLING_FAIL_001",
                    ReasonCode = "HIGH_BREAKAGE_RATE",
                    ReasonName = "Tỷ lệ hạt vỡ cao",
                    Category = "Quality",
                    SeverityLevel = 4,
                    Description = "Tỷ lệ hạt vỡ >3%"
                },
                new FailureReason
                {
                    ReasonId = "HULLING_FAIL_002",
                    ReasonCode = "INCOMPLETE_HULLING",
                    ReasonName = "Tách vỏ không hoàn toàn",
                    Category = "Quality",
                    SeverityLevel = 3,
                    Description = "Còn sót vỏ trấu"
                },
                new FailureReason
                {
                    ReasonId = "HULLING_FAIL_003",
                    ReasonCode = "EQUIPMENT_ISSUE",
                    ReasonName = "Vấn đề thiết bị",
                    Category = "Equipment",
                    SeverityLevel = 3,
                    Description = "Máy xay không hoạt động tốt"
                }
            };
        }

        /// <summary>
        /// Lý do không đạt phân loại
        /// </summary>
        private static List<FailureReason> GetGradingFailureReasons()
        {
            return new List<FailureReason>
            {
                new FailureReason
                {
                    ReasonId = "GRADING_FAIL_001",
                    ReasonCode = "INCONSISTENT_SIZE",
                    ReasonName = "Kích thước không đồng đều",
                    Category = "Quality",
                    SeverityLevel = 3,
                    Description = "Tỷ lệ hạt cùng kích cỡ <90%"
                },
                new FailureReason
                {
                    ReasonId = "GRADING_FAIL_002",
                    ReasonCode = "COLOR_VARIATION",
                    ReasonName = "Màu sắc không đồng đều",
                    Category = "Quality",
                    SeverityLevel = 2,
                    Description = "Màu sắc hạt không đồng nhất"
                },
                new FailureReason
                {
                    ReasonId = "GRADING_FAIL_003",
                    ReasonCode = "HIGH_DEFECT_RATE",
                    ReasonName = "Tỷ lệ hạt lỗi cao",
                    Category = "Quality",
                    SeverityLevel = 4,
                    Description = "Tỷ lệ hạt lỗi >2%"
                }
            };
        }

        /// <summary>
        /// Lý do không đạt lên men
        /// </summary>
        private static List<FailureReason> GetFermentationFailureReasons()
        {
            return new List<FailureReason>
            {
                new FailureReason
                {
                    ReasonId = "FERMENT_FAIL_001",
                    ReasonCode = "OVER_FERMENTATION",
                    ReasonName = "Lên men quá lâu",
                    Category = "Process",
                    SeverityLevel = 4,
                    Description = "Lên men >48 giờ"
                },
                new FailureReason
                {
                    ReasonId = "FERMENT_FAIL_002",
                    ReasonCode = "UNDER_FERMENTATION",
                    ReasonName = "Lên men chưa đủ",
                    Category = "Process",
                    SeverityLevel = 3,
                    Description = "Lên men <12 giờ"
                },
                new FailureReason
                {
                    ReasonId = "FERMENT_FAIL_003",
                    ReasonCode = "WRONG_TEMPERATURE",
                    ReasonName = "Nhiệt độ lên men không phù hợp",
                    Category = "Process",
                    SeverityLevel = 3,
                    Description = "Nhiệt độ <18°C hoặc >25°C"
                }
            };
        }

        /// <summary>
        /// Lý do không đạt rửa
        /// </summary>
        private static List<FailureReason> GetWashingFailureReasons()
        {
            return new List<FailureReason>
            {
                new FailureReason
                {
                    ReasonId = "WASH_FAIL_001",
                    ReasonCode = "INCOMPLETE_WASHING",
                    ReasonName = "Rửa chưa sạch",
                    Category = "Quality",
                    SeverityLevel = 3,
                    Description = "Bề mặt hạt còn bẩn"
                },
                new FailureReason
                {
                    ReasonId = "WASH_FAIL_002",
                    ReasonCode = "EXCESSIVE_WASHING",
                    ReasonName = "Rửa quá mạnh",
                    Category = "Quality",
                    SeverityLevel = 2,
                    Description = "Rửa làm hạt bị tổn thương"
                }
            };
        }

        /// <summary>
        /// Lý do không đạt phân loại
        /// </summary>
        private static List<FailureReason> GetSortingFailureReasons()
        {
            return new List<FailureReason>
            {
                new FailureReason
                {
                    ReasonId = "SORTING_FAIL_001",
                    ReasonCode = "INACCURATE_SORTING",
                    ReasonName = "Phân loại không chính xác",
                    Category = "Quality",
                    SeverityLevel = 4,
                    Description = "Tỷ lệ phân loại sai >5%"
                },
                new FailureReason
                {
                    ReasonId = "SORTING_FAIL_002",
                    ReasonCode = "SLOW_PROCESSING",
                    ReasonName = "Xử lý chậm",
                    Category = "Process",
                    SeverityLevel = 3,
                    Description = "Năng suất <100 kg/giờ"
                }
            };
        }

        /// <summary>
        /// Lý do không đạt rang
        /// </summary>
        private static List<FailureReason> GetRoastingFailureReasons()
        {
            return new List<FailureReason>
            {
                new FailureReason
                {
                    ReasonId = "ROASTING_FAIL_001",
                    ReasonCode = "OVER_ROASTING",
                    ReasonName = "Rang quá chín",
                    Category = "Quality",
                    SeverityLevel = 4,
                    Description = "Nhiệt độ >250°C hoặc thời gian >20 phút"
                },
                new FailureReason
                {
                    ReasonId = "ROASTING_FAIL_002",
                    ReasonCode = "UNDER_ROASTING",
                    ReasonName = "Rang chưa chín",
                    Category = "Quality",
                    SeverityLevel = 3,
                    Description = "Nhiệt độ <180°C hoặc thời gian <12 phút"
                }
            };
        }

        /// <summary>
        /// Lý do không đạt đóng gói
        /// </summary>
        private static List<FailureReason> GetPackagingFailureReasons()
        {
            return new List<FailureReason>
            {
                new FailureReason
                {
                    ReasonId = "PACKAGING_FAIL_001",
                    ReasonCode = "POOR_SEALING",
                    ReasonName = "Đóng gói không kín",
                    Category = "Quality",
                    SeverityLevel = 4,
                    Description = "Độ kín khí <95%"
                },
                new FailureReason
                {
                    ReasonId = "PACKAGING_FAIL_002",
                    ReasonCode = "WRONG_WEIGHT",
                    ReasonName = "Sai trọng lượng",
                    Category = "Quality",
                    SeverityLevel = 3,
                    Description = "Trọng lượng sai >2%"
                }
            };
        }

        /// <summary>
        /// Lý do không đạt bảo quản
        /// </summary>
        private static List<FailureReason> GetStorageFailureReasons()
        {
            return new List<FailureReason>
            {
                new FailureReason
                {
                    ReasonId = "STORAGE_FAIL_001",
                    ReasonCode = "WRONG_TEMPERATURE",
                    ReasonName = "Nhiệt độ bảo quản không phù hợp",
                    Category = "Process",
                    SeverityLevel = 4,
                    Description = "Nhiệt độ <15°C hoặc >25°C"
                },
                new FailureReason
                {
                    ReasonId = "STORAGE_FAIL_002",
                    ReasonCode = "WRONG_HUMIDITY",
                    ReasonName = "Độ ẩm bảo quản không phù hợp",
                    Category = "Process",
                    SeverityLevel = 3,
                    Description = "Độ ẩm <50% hoặc >65%"
                }
            };
        }

        /// <summary>
        /// Lý do không đạt vận chuyển
        /// </summary>
        private static List<FailureReason> GetTransportFailureReasons()
        {
            return new List<FailureReason>
            {
                new FailureReason
                {
                    ReasonId = "TRANSPORT_FAIL_001",
                    ReasonCode = "WRONG_TEMPERATURE",
                    ReasonName = "Nhiệt độ vận chuyển không phù hợp",
                    Category = "Process",
                    SeverityLevel = 4,
                    Description = "Nhiệt độ <15°C hoặc >30°C"
                },
                new FailureReason
                {
                    ReasonId = "TRANSPORT_FAIL_002",
                    ReasonCode = "LONG_TRANSPORT",
                    ReasonName = "Vận chuyển quá lâu",
                    Category = "Process",
                    SeverityLevel = 3,
                    Description = "Thời gian vận chuyển >72 giờ"
                }
            };
        }

        /// <summary>
        /// Lý do không đạt kiểm tra chất lượng
        /// </summary>
        private static List<FailureReason> GetQualityCheckFailureReasons()
        {
            return new List<FailureReason>
            {
                new FailureReason
                {
                    ReasonId = "QUALITY_FAIL_001",
                    ReasonCode = "INACCURATE_CHECK",
                    ReasonName = "Kiểm tra không chính xác",
                    Category = "Quality",
                    SeverityLevel = 4,
                    Description = "Độ chính xác <98%"
                },
                new FailureReason
                {
                    ReasonId = "QUALITY_FAIL_002",
                    ReasonCode = "SLOW_CHECK",
                    ReasonName = "Kiểm tra chậm",
                    Category = "Process",
                    SeverityLevel = 3,
                    Description = "Thời gian kiểm tra >4 giờ"
                }
            };
        }

        /// <summary>
        /// Lý do không đạt tách vỏ quả
        /// </summary>
        private static List<FailureReason> GetPulpingFailureReasons()
        {
            return new List<FailureReason>
            {
                new FailureReason
                {
                    ReasonId = "PULPING_FAIL_001",
                    ReasonCode = "INCOMPLETE_PULPING",
                    ReasonName = "Tách vỏ không hoàn toàn",
                    Category = "Quality",
                    SeverityLevel = 4,
                    Description = "Tỷ lệ tách vỏ <95%"
                },
                new FailureReason
                {
                    ReasonId = "PULPING_FAIL_002",
                    ReasonCode = "EXCESSIVE_DAMAGE",
                    ReasonName = "Hạt bị tổn thương nhiều",
                    Category = "Quality",
                    SeverityLevel = 3,
                    Description = "Tỷ lệ hạt tổn thương >3%"
                }
            };
        }

        /// <summary>
        /// Lý do không đạt loại bỏ chất nhầy
        /// </summary>
        private static List<FailureReason> GetDemucilagingFailureReasons()
        {
            return new List<FailureReason>
            {
                new FailureReason
                {
                    ReasonId = "DEMUCILAGING_FAIL_001",
                    ReasonCode = "INCOMPLETE_REMOVAL",
                    ReasonName = "Loại bỏ chất nhầy không hoàn toàn",
                    Category = "Quality",
                    SeverityLevel = 4,
                    Description = "Độ sạch chất nhầy <90%"
                },
                new FailureReason
                {
                    ReasonId = "DEMUCILAGING_FAIL_002",
                    ReasonCode = "LONG_PROCESSING",
                    ReasonName = "Xử lý quá lâu",
                    Category = "Process",
                    SeverityLevel = 3,
                    Description = "Thời gian xử lý >8 giờ"
                }
            };
        }

        /// <summary>
        /// Lý do không đạt đánh bóng
        /// </summary>
        private static List<FailureReason> GetPolishingFailureReasons()
        {
            return new List<FailureReason>
            {
                new FailureReason
                {
                    ReasonId = "POLISHING_FAIL_001",
                    ReasonCode = "POOR_POLISHING",
                    ReasonName = "Đánh bóng không đạt yêu cầu",
                    Category = "Quality",
                    SeverityLevel = 4,
                    Description = "Độ bóng bề mặt <85%"
                },
                new FailureReason
                {
                    ReasonId = "POLISHING_FAIL_002",
                    ReasonCode = "EXCESSIVE_WEAR",
                    ReasonName = "Mài mòn quá mức",
                    Category = "Quality",
                    SeverityLevel = 3,
                    Description = "Tỷ lệ hạt bị mài mòn >2%"
                }
            };
        }

        /// <summary>
        /// Lý do không đạt kiểm tra hương vị
        /// </summary>
        private static List<FailureReason> GetCuppingFailureReasons()
        {
            return new List<FailureReason>
            {
                new FailureReason
                {
                    ReasonId = "CUPPING_FAIL_001",
                    ReasonCode = "LOW_SCORE",
                    ReasonName = "Điểm hương vị thấp",
                    Category = "Quality",
                    SeverityLevel = 4,
                    Description = "Điểm hương vị tổng thể <80"
                },
                new FailureReason
                {
                    ReasonId = "CUPPING_FAIL_002",
                    ReasonCode = "UNBALANCED_FLAVOR",
                    ReasonName = "Hương vị không cân bằng",
                    Category = "Quality",
                    SeverityLevel = 3,
                    Description = "Độ chua hoặc đắng <6 điểm"
                }
            };
        }

        /// <summary>
        /// Lý do không đạt pha trộn
        /// </summary>
        private static List<FailureReason> GetBlendingFailureReasons()
        {
            return new List<FailureReason>
            {
                new FailureReason
                {
                    ReasonId = "BLENDING_FAIL_001",
                    ReasonCode = "INCOMPLETE_MIXING",
                    ReasonName = "Pha trộn không đều",
                    Category = "Quality",
                    SeverityLevel = 4,
                    Description = "Độ đồng đều pha trộn <95%"
                },
                new FailureReason
                {
                    ReasonId = "BLENDING_FAIL_002",
                    ReasonCode = "WRONG_RATIO",
                    ReasonName = "Tỷ lệ pha trộn sai",
                    Category = "Quality",
                    SeverityLevel = 3,
                    Description = "Độ chính xác tỷ lệ sai >2%"
                }
            };
        }

        /// <summary>
        /// Lý do không đạt xay cà phê
        /// </summary>
        private static List<FailureReason> GetGrindingFailureReasons()
        {
            return new List<FailureReason>
            {
                new FailureReason
                {
                    ReasonId = "GRINDING_FAIL_001",
                    ReasonCode = "INCONSISTENT_GRIND",
                    ReasonName = "Độ mịn không đồng đều",
                    Category = "Quality",
                    SeverityLevel = 4,
                    Description = "Độ mịn đồng đều <90%"
                },
                new FailureReason
                {
                    ReasonId = "GRINDING_FAIL_002",
                    ReasonCode = "HIGH_TEMPERATURE",
                    ReasonName = "Nhiệt độ xay quá cao",
                    Category = "Process",
                    SeverityLevel = 3,
                    Description = "Nhiệt độ xay >35°C"
                }
            };
        }

        /// <summary>
        /// Lý do không đạt chế biến cà phê hòa tan
        /// </summary>
        private static List<FailureReason> GetInstantProcessingFailureReasons()
        {
            return new List<FailureReason>
            {
                new FailureReason
                {
                    ReasonId = "INSTANT_FAIL_001",
                    ReasonCode = "POOR_SOLUBILITY",
                    ReasonName = "Độ hòa tan kém",
                    Category = "Quality",
                    SeverityLevel = 4,
                    Description = "Độ hòa tan <95%"
                },
                new FailureReason
                {
                    ReasonId = "INSTANT_FAIL_002",
                    ReasonCode = "LOW_CAFFEINE",
                    ReasonName = "Hàm lượng caffein thấp",
                    Category = "Quality",
                    SeverityLevel = 3,
                    Description = "Hàm lượng caffein <1.5%"
                }
            };
        }

        /// <summary>
        /// Lý do không đạt lên men carbonic
        /// </summary>
        private static List<FailureReason> GetCarbonicFermentFailureReasons()
        {
            return new List<FailureReason>
            {
                new FailureReason
                {
                    ReasonId = "CARBONIC_FERMENT_FAIL_001",
                    ReasonCode = "OVER_FERMENTATION",
                    ReasonName = "Lên men quá lâu",
                    Category = "Process",
                    SeverityLevel = 4,
                    Description = "Lên men >48 giờ"
                },
                new FailureReason
                {
                    ReasonId = "CARBONIC_FERMENT_FAIL_002",
                    ReasonCode = "UNDER_FERMENTATION",
                    ReasonName = "Lên men chưa đủ",
                    Category = "Process",
                    SeverityLevel = 3,
                    Description = "Lên men <12 giờ"
                },
                new FailureReason
                {
                    ReasonId = "CARBONIC_FERMENT_FAIL_003",
                    ReasonCode = "WRONG_TEMPERATURE",
                    ReasonName = "Nhiệt độ lên men không phù hợp",
                    Category = "Process",
                    SeverityLevel = 3,
                    Description = "Nhiệt độ <18°C hoặc >25°C"
                }
            };
        }

        /// <summary>
        /// Đánh giá tiêu chí dựa trên giá trị thực tế
        /// </summary>
        /// <param name="criteria">Tiêu chí đánh giá</param>
        /// <param name="actualValue">Giá trị thực tế</param>
        /// <returns>Kết quả đánh giá (Pass/Fail)</returns>
        public static string EvaluateCriteria(EvaluationCriteria criteria, decimal actualValue)
        {
            if (criteria.MinValue.HasValue && actualValue < criteria.MinValue.Value)
                return "Fail";
            
            if (criteria.MaxValue.HasValue && actualValue > criteria.MaxValue.Value)
                return "Fail";
            
            return "Pass";
        }

        /// <summary>
        /// Tính điểm đánh giá tổng hợp
        /// </summary>
        /// <param name="criteriaResults">Kết quả từng tiêu chí</param>
        /// <returns>Điểm tổng hợp (0-100)</returns>
        public static decimal CalculateOverallScore(List<(EvaluationCriteria criteria, string result)> criteriaResults)
        {
            if (!criteriaResults.Any())
                return 0;

            decimal totalWeight = criteriaResults.Sum(x => x.criteria.Weight);
            decimal weightedScore = 0;

            foreach (var (criteria, result) in criteriaResults)
            {
                decimal score = result == "Pass" ? 100 : 0;
                weightedScore += score * criteria.Weight;
            }

            return totalWeight > 0 ? weightedScore / totalWeight : 0;
        }
    }
}
