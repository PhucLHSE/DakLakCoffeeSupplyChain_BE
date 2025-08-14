using DakLakCoffeeSupplyChain.Common.DTOs.FarmingCommitmentDTOs.FarmingCommitmentsDetailsDTOs;
using System.ComponentModel.DataAnnotations;

namespace DakLakCoffeeSupplyChain.Common.DTOs.FarmingCommitmentDTOs
{
    public class FarmingCommitmentUpdateDto : IValidatableObject
    {

        //public Guid? Id { get; set; }
        [Required(ErrorMessage = "Tên bản cam kết không được để trống")]
        public string CommitmentName { get; set; } = string.Empty;

        public string Note { get; set; } = string.Empty;

        public ICollection<FarmingCommitmentsDetailsUpdateDto> FarmingCommitmentsDetailsUpdateDtos { get; set; } = [];

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (FarmingCommitmentsDetailsUpdateDtos == null || FarmingCommitmentsDetailsUpdateDtos.Count == 0)
                yield return new ValidationResult("Phải có ít nhất một chi tiết đơn cam kết",
                    [nameof(FarmingCommitmentsDetailsUpdateDtos)]);
        }
    }
}
