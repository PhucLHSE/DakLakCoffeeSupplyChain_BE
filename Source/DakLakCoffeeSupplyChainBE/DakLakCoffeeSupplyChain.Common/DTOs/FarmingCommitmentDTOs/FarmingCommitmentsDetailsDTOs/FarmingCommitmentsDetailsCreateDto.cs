using System.ComponentModel.DataAnnotations;

namespace DakLakCoffeeSupplyChain.Common.DTOs.FarmingCommitmentDTOs.FarmingCommitmentsDetailsDTOs
{
    public class FarmingCommitmentsDetailsCreateDto
    {
        [Required(ErrorMessage = "Chi tiết đơn đăng ký không được để trống")]
        public Guid RegistrationDetailId { get; set; }

        //public Guid PlanDetailId { get; set; }
        [Required(ErrorMessage = "Giá cả cam kết không được phép bỏ trống")]
        public double ConfirmedPrice { get; set; }

        [Required(ErrorMessage = "Sản lượng cam kết không được phép để trống")]
        public double CommittedQuantity { get; set; }

        public DateOnly? EstimatedDeliveryStart { get; set; }

        public DateOnly? EstimatedDeliveryEnd { get; set; }

        [Required(ErrorMessage = "Các chính sách không được để trống, bạn nên có các chính sách tối thiểu để bảo vệ quyền lợi cho cả hai bên khi gặp sự cố")]
        public string Note { get; set; } = string.Empty;

        public Guid? ContractDeliveryItemId { get; set; }
    }
}
