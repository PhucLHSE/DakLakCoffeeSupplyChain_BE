namespace DakLakCoffeeSupplyChain.Common.Enum.CultivationRegistrationEnums
{
    public enum CultivationRegistrationStatus
    {
        Pending = 0,    // Trạng thái mặc định, chờ duyệt từ phía BM
        Active = 1,     // Trạng thái sau khi đã được BM duyệt
        Cancel = 2,     // Trạng thái tự farmer hủy bỏ trước khi BM duyệt
        Rejected = 3,   // Trạng thái sau khi bị BM từ chối
        Unknown = 4,    // Trạng thái khi hệ thống bị lỗi, không được lưu trong database
    }
}
