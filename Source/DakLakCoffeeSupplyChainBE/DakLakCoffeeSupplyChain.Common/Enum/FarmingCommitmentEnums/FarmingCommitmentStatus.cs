namespace DakLakCoffeeSupplyChain.Common.Enum.FarmingCommitmentEnums
{
    public enum FarmingCommitmentStatus
    {
        Pending_farmer = 0,             // Trạng thái chờ farmer duyệt
        Active = 1,                     // Trạng thái đã được farmer duyệt và đi vào hoạt động
        Complete = 2,                   // Trạng thái sau khi cả 2 bên đã hoàn thành cam kết
        Cancelled = 3,                  // Trạng thái sau khi BM hủy bỏ cam kết
        Breached = 4,                   // Trạng thái khi một trong 2 bên vi phạm điều khoản đã đề ra
        Unknown = 5,                    // Trạng thái khi hệ thống bị lỗi khi dữ liệu trạng thái của hợp đồng không được lưu trong hệ thống.
    }
}
