using DakLakCoffeeSupplyChain.Common.DTOs.MediaDTOs;
using DakLakCoffeeSupplyChain.Common.DTOs.ProcessingParameterDTOs;
using DakLakCoffeeSupplyChain.Common.DTOs.ProcessingWastesDTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.DTOs.ProcessingBatchsProgressDTOs
{
    public class ProcessingProgressWithStageDto
    {
        public Guid ProgressId { get; set; }
        public int StepIndex { get; set; }
        public DateTime ProgressDate { get; set; }

        // Người cập nhật
        public string UpdatedByName { get; set; }

        // Giai đoạn
        public string StageId { get; set; }
        public string StageName { get; set; }
        public string StageDescription { get; set; }

        // Kết quả
        public double? OutputQuantity { get; set; }

        // Dữ liệu liên quan
        public List<ProcessingParameterViewAllDto> Parameters { get; set; }
        public List<ProcessingWasteViewAllDto> Wastes { get; set; }
        public List<MediaFileResponse> MediaFiles { get; set; }
    }

}
