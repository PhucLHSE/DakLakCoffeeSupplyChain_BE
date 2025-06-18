using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.DTOs.WarehouseDTOs
{
    public class WarehouseCreateDto
    {
        public string Name { get; set; } = null!;
        public string Location { get; set; } = null!;
        public Guid ManagerId { get; set; }
        public double? Capacity { get; set; }
    }
}
