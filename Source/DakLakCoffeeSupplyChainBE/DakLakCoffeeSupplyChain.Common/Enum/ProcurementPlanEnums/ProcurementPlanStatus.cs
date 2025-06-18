namespace DakLakCoffeeSupplyChain.Common.Enum.ProcurementPlanEnums
{
    public enum ProcurementPlanStatus
    {
        Draft = -1,             // Bản nháp, dùng làm default
        Open = 0,               // Trạng thái mở, cho phép đăng ký
        Closed = 1,             // Trạng thái đóng, không cho phép đăng ký nữa
        Cancelled = 2,          // Trạng thái hủy bỏ, không còn hiệu lực
        Deleted = 3,            // Trạng thái xóa, không còn hiển thị
    }
}
