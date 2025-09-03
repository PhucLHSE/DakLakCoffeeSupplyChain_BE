namespace DakLakCoffeeSupplyChain.Common.DTOs.AuthDTOs
{
    public class VerifyAccountByAdminDto
    {
        public bool Action { get; set; } = false;
        public string Reason { get; set; } = string.Empty;
    }
}
