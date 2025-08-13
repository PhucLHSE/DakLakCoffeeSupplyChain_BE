using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.DTOs.ProcessingBatchsProgressDTOs
{
    public class AvailableBatchForProgressDto
    {
        // Thông tin cơ bản của batch
        public Guid BatchId { get; set; }
        public string BatchCode { get; set; }
        public string SystemBatchCode { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        
        // Thông tin liên kết
        public Guid CoffeeTypeId { get; set; }
        public string CoffeeTypeName { get; set; }
        public Guid CropSeasonId { get; set; }
        public string CropSeasonName { get; set; }
        public int MethodId { get; set; }
        public string MethodName { get; set; }
        public Guid FarmerId { get; set; }
        public string FarmerName { get; set; }
        
        // Thông tin khối lượng
        public double TotalInputQuantity { get; set; }
        public double TotalProcessedQuantity { get; set; }
        public double RemainingQuantity { get; set; }
        public string InputUnit { get; set; }
        
        // Thông tin tiến độ
        public int TotalProgresses { get; set; }
        public DateOnly? LastProgressDate { get; set; }
        
        // Thông tin bổ sung
        public bool CanCreateProgress => RemainingQuantity > 0;
        public string StatusDescription => RemainingQuantity > 0 ? "Có thể tạo tiến độ" : "Đã chế biến hết";
    }
}
