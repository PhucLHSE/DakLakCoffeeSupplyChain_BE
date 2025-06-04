using DakLakCoffeeSupplyChain.Common.Enum.UserAccountEnums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.DTOs.UserAccountDTOs
{
    public class UserAccountViewAllDto
    {
        public Guid UserId { get; set; }

        public string UserCode { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string PhoneNumber { get; set; } = string.Empty;

        public string RoleName { get; set; } = string.Empty;

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public UserAccountStatus Status { get; set; } = UserAccountStatus.Unknown;

        public DateTime? LastLogin { get; set; }

        public DateTime RegistrationDate { get; set; }
    }
}
