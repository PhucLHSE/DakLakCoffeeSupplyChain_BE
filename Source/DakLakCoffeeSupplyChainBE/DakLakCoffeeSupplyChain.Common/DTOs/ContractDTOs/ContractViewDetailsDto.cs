using DakLakCoffeeSupplyChain.Common.DTOs.ContractDTOs.ContractItemDTOs;
using DakLakCoffeeSupplyChain.Common.DTOs.ContractDTOs.SettlementDTOs;
using DakLakCoffeeSupplyChain.Common.Enum.ContractEnums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.DTOs.ContractDTOs
{
    public class ContractViewDetailsDto
    {
        public Guid ContractId { get; set; }

        public string ContractCode { get; set; } = string.Empty;

        public string ContractNumber { get; set; } = string.Empty;

        public string ContractTitle { get; set; } = string.Empty;

        public string ContractFileUrl { get; set; } = string.Empty;

        public string SellerName { get; set; } = string.Empty;

        public Guid BuyerId { get; set; }

        public string BuyerName { get; set; } = string.Empty;

        public int? DeliveryRounds { get; set; }

        public double? TotalQuantity { get; set; }

        public double? TotalValue { get; set; }

        public DateOnly? StartDate { get; set; }

        public DateOnly? EndDate { get; set; }

        public DateTime? SignedAt { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ContractStatus Status { get; set; } = ContractStatus.NotStarted;

        public string CancelReason { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        public string ContractType { get; set; } = string.Empty;

        public Guid? ParentContractId { get; set; }

        public string ParentContractCode { get; set; } = string.Empty;

        public int? PaymentRounds { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public SettlementStatus SettlementStatus { get; set; } = SettlementStatus.None;

        public DateOnly? SettledAt { get; set; }

        public string SettlementFileUrl { get; set; } = string.Empty;

        //Parsed from SettlementFilesJson; UI có thể render list link.
        public List<SettlementRound> SettlementFiles { get; set; } = new();

        public string SettlementNote { get; set; } = string.Empty;

        public List<ContractItemViewDto> ContractItems { get; set; } = new();
    }
}
