using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.DTOs.WarehouseReceiptDTOs
{
    public class WarehouseReceiptConfirmDto
    {
        public double ConfirmedQuantity { get; set; }
        public string? Note { get; set; }
    }
}
