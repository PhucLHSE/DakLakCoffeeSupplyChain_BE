using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.DTOs.BusinessManagerDTOs
{
    public class BusinessManagerCreateDto
    {
        [Required(ErrorMessage = "Company name is required.")]
        [StringLength(100, ErrorMessage = "Company name must be at most 100 characters.")]
        public string CompanyName { get; set; } = string.Empty;

        [StringLength(50, ErrorMessage = "Position must be at most 50 characters.")]
        public string Position { get; set; } = string.Empty;

        [StringLength(100, ErrorMessage = "Department must be at most 100 characters.")]
        public string Department { get; set; } = string.Empty;

        [Required(ErrorMessage = "Company address is required.")]
        [StringLength(255, ErrorMessage = "Company address must be at most 255 characters.")]
        public string CompanyAddress { get; set; } = string.Empty;

        [StringLength(50, ErrorMessage = "Tax ID must be at most 50 characters.")]
        public string TaxId { get; set; } = string.Empty;

        [StringLength(255, ErrorMessage = "Website must be at most 255 characters.")]
        [Url(ErrorMessage = "Website must be a valid URL.")]
        public string? Website { get; set; }

        [Required(ErrorMessage = "Contact email is required.")]
        [StringLength(100, ErrorMessage = "Contact email must be at most 100 characters.")]
        [EmailAddress(ErrorMessage = "Contact email must be a valid email address.")]
        public string ContactEmail { get; set; } = string.Empty;

        [StringLength(255, ErrorMessage = "Business license URL must be at most 255 characters.")]
        [Url(ErrorMessage = "Business license URL must be a valid URL.")]
        public string? BusinessLicenseUrl { get; set; }
    }
}
