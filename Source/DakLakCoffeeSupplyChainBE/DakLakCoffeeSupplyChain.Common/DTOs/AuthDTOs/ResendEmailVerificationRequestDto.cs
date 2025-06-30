using System.ComponentModel.DataAnnotations;

namespace DakLakCoffeeSupplyChain.Common.DTOs.AuthDTOs
{
    public class ResendEmailVerificationRequestDto
    {
        [Required(ErrorMessage = "Email không được để trống")]
        public string Email { get; set; } = string.Empty;
    }
}
