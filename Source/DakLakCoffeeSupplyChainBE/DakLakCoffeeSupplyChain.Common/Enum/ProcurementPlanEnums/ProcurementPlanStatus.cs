namespace DakLakCoffeeSupplyChain.Common.Enum.ProcurementPlanEnums
{ 
    /// <summary>
    /// Closed và cancelled khác nhau như thế nào?
    /// Điểm chung:
    /// - Có thể được kích hoạt khi status đang ở trạng thái open
    /// - Ngay cả khi sản lượng đã được đăng ký vẫn chưa đủ, closed và cancelled vẫn có thể được kích hoạt
    /// Điểm riêng:
    /// - Closed: Có thể được cập nhật ngay sau khi plan đã đạt đủ sản lượng đăng ký
    /// - Cancelled: Chỉ có thể được kích hoạt khi plan đó đang được mở vẫn chưa có cam kết nào
    /// </summary>
    public enum ProcurementPlanStatus
    {
        Draft = -1,             // Bản nháp, dùng làm default
        Open = 0,               // Trạng thái mở, cho phép đăng ký, trạng thái này chỉ được chuyển sang khi đang ở Draft
        Closed = 1,             // Trạng thái đóng, không cho phép đăng ký nữa, trạng thái này chỉ được chuyển sang khi đang ở Open
        Cancelled = 2,          // Trạng thái hủy bỏ, không còn hiệu lực, trạng thái này chỉ được chuyển sang khi đang ở Open và plan đó chưa có cam kết nào
        //Deleted = 3,            // Trạng thái xóa, không còn hiển thị, trạng thái này chỉ được chuyển sang khi đang ở Draft
    }
}
