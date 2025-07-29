using DakLakCoffeeSupplyChain.Common.DTOs.FarmingCommitmentDTOs.FarmingCommitmentsDetailsDTOs;
using System.ComponentModel.DataAnnotations;

namespace DakLakCoffeeSupplyChain.Common.DTOs.FarmingCommitmentDTOs
{
    public class FarmingCommitmentCreateDto : IValidatableObject
    {
        [Required(ErrorMessage = "Tên bản cam kết không được để trống")]
        public string CommitmentName { get; set; } = string.Empty;
        [Required(ErrorMessage = "Phiếu đăng ký không được phép để trống")] //Lỗi này chỉ có dev được phép thấy
        public Guid RegistrationId { get; set; }

        public string Note { get; set; } = string.Empty;

        public ICollection<FarmingCommitmentsDetailsCreateDto> FarmingCommitmentsDetailsCreateDtos { get; set; } = [];

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (FarmingCommitmentsDetailsCreateDtos == null || FarmingCommitmentsDetailsCreateDtos.Count == 0)
                yield return new ValidationResult("Phải có ít nhất một chi tiết đơn cam kết", 
                    [nameof(FarmingCommitmentsDetailsCreateDtos)]);
        }
    }
}
