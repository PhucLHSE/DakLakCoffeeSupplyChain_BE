using DakLakCoffeeSupplyChain.Common.Enum.ProcessingEnums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.DTOs.ProcessingBatchDTOs
{
    public class ProcessingBatchUpdateDto
    {
        public Guid BatchId { get; set; }
        public string BatchCode { get; set; }
        public Guid CoffeeTypeId { get; set; }
        public Guid CropSeasonId { get; set; }
        public int MethodId { get; set; }
        public double InputQuantity { get; set; }
        public string InputUnit { get; set; }
        public ProcessingStatus Status { get; set; }
    }
}
