using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.DTOs.WarehouseDTOs
{
    public class WarehouseUpdateDto
    {
        public Guid WarehouseId { get; set; }
        public string Name { get; set; }
        public string Location { get; set; }
        public double? Capacity { get; set; }
        public Guid ManagerId { get; set; }
    }
}
