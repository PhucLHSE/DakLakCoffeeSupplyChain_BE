using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.DTOs.BusinessManagerDTOs
{
    public class BusinessManagerViewDetailsDto
    {
        public Guid ManagerId { get; set; }

        public string ManagerCode { get; set; } = string.Empty;

        public string CompanyName { get; set; } = string.Empty;

        public string Position { get; set; } = string.Empty;

        public string Department { get; set; } = string.Empty;

        public string CompanyAddress { get; set; } = string.Empty;

        public string TaxId { get; set; } = string.Empty;

        public string Website { get; set; } = string.Empty;

        public string ContactEmail { get; set; } = string.Empty;

        public string BusinessLicenseUrl { get; set; } = string.Empty;

        public bool? IsCompanyVerified { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }


        // Thông tin user
        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string PhoneNumber { get; set; } = string.Empty;
    }
}
