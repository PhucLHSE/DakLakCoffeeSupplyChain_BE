using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.DTOs.InventoryLogDTOs
{
    public class InventoryLogListItemDto
    {
        public Guid LogId { get; set; }
        public string ActionType { get; set; } = default!;
        public double QuantityChanged { get; set; }
        public string? Note { get; set; }
        public DateTime LoggedAt { get; set; }
        public bool TriggeredBySystem { get; set; }
        public string? UpdatedByName { get; set; }
    }
}
