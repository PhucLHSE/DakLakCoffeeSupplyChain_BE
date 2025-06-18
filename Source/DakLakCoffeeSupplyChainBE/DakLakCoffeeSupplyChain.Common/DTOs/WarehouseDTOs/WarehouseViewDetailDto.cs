using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.DTOs.WarehouseDTOs
{
    public class WarehouseViewDetailDto
    {
        public Guid WarehouseId { get; set; }
        public string WarehouseCode { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string Location { get; set; } = null!;
        public double? Capacity { get; set; }
        public Guid ManagerId { get; set; }
        public string ManagerName { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
