using DakLakCoffeeSupplyChain.Common.DTOs.ProcessingBatchsProgressDTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.DTOs.ProcessingBatchDTOs
{
    public class ProcessingBatchDetailFullDto
    {
        // Thông tin cơ bản
        public Guid BatchId { get; set; }
        public string BatchCode { get; set; }
        public string SystemBatchCode { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }

        // Thông tin liên kết
        public Guid CropSeasonId { get; set; }
        public string CropSeasonName { get; set; }

        public Guid CoffeeTypeId { get; set; }
        public string TypeName { get; set; }

        public int MethodId { get; set; }
        public string MethodName { get; set; }

        public Guid? FarmerId { get; set; }
        public string FarmerName { get; set; }

        // Số liệu đầu vào/đầu ra
        public double? TotalInputQuantity { get; set; }
        public double? TotalOutputQuantity => Progresses?.OrderByDescending(p => p.StepIndex)?.FirstOrDefault()?.OutputQuantity ?? 0;

        // Các bước sơ chế (progresses)
        public List<ProcessingProgressWithStageDto> Progresses { get; set; }

        // Sản phẩm tạo ra (nếu có)
        //public List<ProcessingProductDto> Products { get; set; }
    }
}
