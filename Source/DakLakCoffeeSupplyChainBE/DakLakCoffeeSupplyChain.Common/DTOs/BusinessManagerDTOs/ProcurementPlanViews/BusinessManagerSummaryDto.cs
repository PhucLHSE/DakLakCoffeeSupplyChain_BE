namespace DakLakCoffeeSupplyChain.Common.DTOs.BusinessManagerDTOs.ProcurementPlanViews
{
    public class BusinessManagerSummaryDto
    {
        public Guid ManagerId { get; set; }

        public Guid UserId { get; set; }

        public string ManagerCode { get; set; } = string.Empty;

        public string CompanyName { get; set; } = string.Empty;

        public string CompanyAddress { get; set; } = string.Empty;

        public string Website { get; set; } = string.Empty;

        public string ContactEmail { get; set; } = string.Empty;
    }
}
