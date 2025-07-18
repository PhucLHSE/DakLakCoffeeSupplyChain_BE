using System.ComponentModel.DataAnnotations;

namespace DakLakCoffeeSupplyChain.Common.DTOs.FarmingCommitmentDTOs
{
    public class FarmingCommitmentCreateDto
    {
        [Required(ErrorMessage = "Tên bản cam kết không được để trống")]
        public string CommitmentName { get; set; } = string.Empty;
        [Required(ErrorMessage = "Chi tiết đơn đăng ký không được để trống")]
        public Guid RegistrationDetailId { get; set; }

        //public Guid PlanDetailId { get; set; }

        //public Guid FarmerId { get; set; }
        [Required(ErrorMessage = "Giá cả cam kết không được phép bỏ trống")]
        public double ConfirmedPrice { get; set; }
        [Required(ErrorMessage = "Sản lượng cam kết không được phép để trống")]
        public double CommittedQuantity { get; set; }

        public DateOnly? EstimatedDeliveryStart { get; set; }

        public DateOnly? EstimatedDeliveryEnd { get; set; }

        public string Note { get; set; } = string.Empty;

        public Guid? ContractDeliveryItemId { get; set; }
    }
}
