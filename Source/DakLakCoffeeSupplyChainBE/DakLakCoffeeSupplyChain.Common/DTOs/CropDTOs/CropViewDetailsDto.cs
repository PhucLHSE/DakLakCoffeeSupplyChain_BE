using DakLakCoffeeSupplyChain.Common.Enum.CropEnums;
using DakLakCoffeeSupplyChain.Common.DTOs.MediaDTOs;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.DTOs.CropDTOs
{
    public class CropViewDetailsDto
    {
        public Guid CropId { get; set; }

        public string CropCode { get; set; } = string.Empty;

        public string Address { get; set; } = string.Empty;

        public string FarmName { get; set; } = string.Empty;

        public decimal? CropArea { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public CropStatus Status { get; set; } = CropStatus.Inactive;

        public DateTime? CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public Guid? CreatedBy { get; set; }

        public Guid? UpdatedBy { get; set; }

        public bool? IsDeleted { get; set; }

        public string? Note { get; set; }
        public bool? IsApproved { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public Guid? ApprovedBy { get; set; }
        public string? RejectReason { get; set; }
        
        // Media files
        public List<string>? Images { get; set; }
        public List<string>? Videos { get; set; }
        public List<string>? Documents { get; set; }
    }
}
