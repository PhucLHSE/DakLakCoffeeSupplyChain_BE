using DakLakCoffeeSupplyChain.Common.DTOs.ContractDeliveryBatchDTOs.ContractDeliveryItem;
using DakLakCoffeeSupplyChain.Common.Enum.ContractDeliveryBatchEnums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.DTOs.ContractDeliveryBatchDTOs
{
    public class ContractDeliveryBatchViewDetailDto
    {
        public Guid DeliveryBatchId { get; set; }

        public string DeliveryBatchCode { get; set; } = string.Empty;

        public Guid ContractId { get; set; }

        public string ContractNumber { get; set; } = string.Empty;

        public string ContractTitle { get; set; } = string.Empty;

        public int DeliveryRound { get; set; }

        public DateOnly? ExpectedDeliveryDate { get; set; }

        public double? TotalPlannedQuantity { get; set; }


        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ContractDeliveryBatchStatus Status { get; set; } = ContractDeliveryBatchStatus.Planned;

        public DateTime? CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public List<ContractDeliveryItemViewDto> ContractDeliveryItems { get; set; } = new();
    }
}
