using System.ComponentModel.DataAnnotations;

namespace DakLakCoffeeSupplyChain.Common.DTOs.ProcurementPlanDTOs
{
    public class ProcurementPlanDeleteDto
    {
        [Required]
        public Guid PlanId { get; set; }
    }
}
