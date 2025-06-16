using DakLakCoffeeSupplyChain.Common.Enum.ContractEnums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.DTOs.ContractDTOs
{
    public class ContractViewAllDto
    {
        public Guid ContractId { get; set; }

        public string ContractCode { get; set; } = string.Empty;

        public string ContractTitle { get; set; } = string.Empty;

        public string SellerName { get; set; } = string.Empty;

        public string BuyerName { get; set; } = string.Empty;

        public int? DeliveryRounds { get; set; }

        public double? TotalQuantity { get; set; }

        public double? TotalValue { get; set; }

        public DateOnly? StartDate { get; set; }

        public DateOnly? EndDate { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ContractStatus Status { get; set; } = ContractStatus.NotStarted;
    }
}
