using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.DTOs.BusinessManagerDTOs
{
    public class BusinessManagerViewAllDto
    {
        public Guid ManagerId { get; set; }

        public string ManagerCode { get; set; } = string.Empty;

        public string CompanyName { get; set; } = string.Empty;

        public string Position { get; set; } = string.Empty;

        public string Department { get; set; } = string.Empty;

        public bool? IsCompanyVerified { get; set; }
    }
}
