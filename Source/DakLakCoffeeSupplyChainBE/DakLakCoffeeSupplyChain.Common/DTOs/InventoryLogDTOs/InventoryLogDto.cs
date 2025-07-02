using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.DTOs.InventoryLogDTOs
{
    public class InventoryLogDto
    {
        public Guid LogId { get; set; }
        public string ActionType { get; set; }
        public double QuantityChanged { get; set; }
        public string Note { get; set; }
        public DateTime LoggedAt { get; set; }
    }
}
