using DakLakCoffeeSupplyChain.Common.Enum.RoleEnums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.DTOs.RoleDTOs
{
    public class RoleViewAllDto
    {
        public int RoleId { get; set; }

        public string RoleName { get; set; } = string.Empty;

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public RoleStatus Status { get; set; } = RoleStatus.Inactive;

    }
}
