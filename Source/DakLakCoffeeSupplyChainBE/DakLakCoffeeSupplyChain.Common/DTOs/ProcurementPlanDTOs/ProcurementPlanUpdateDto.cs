using DakLakCoffeeSupplyChain.Common.DTOs.ProcurementPlanDTOs.ViewDetailsDtos;
using DakLakCoffeeSupplyChain.Common.Enum.ProcurementPlanEnums;

namespace DakLakCoffeeSupplyChain.Common.DTOs.ProcurementPlanDTOs
{
    public class ProcurementPlanUpdateDto
    {
        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public DateOnly? StartDate { get; set; }

        public DateOnly? EndDate { get; set; }

        public ProcurementPlanStatus Status { get; set; } = ProcurementPlanStatus.Draft;

        public ICollection<ProcurementPlanDetailsUpdateDto> ProcurementPlansDetailsUpdateDto { get; set; } = [];
        public ICollection<ProcurementPlanDetailsCreateDto> ProcurementPlansDetailsCreateDto { get; set; } = [];
    }
}
