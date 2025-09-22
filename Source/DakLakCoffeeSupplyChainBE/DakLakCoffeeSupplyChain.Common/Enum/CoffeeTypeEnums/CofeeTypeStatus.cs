namespace DakLakCoffeeSupplyChain.Common.Enum.CoffeeTypeEnums
{
    public enum CoffeeTypeStatus
    {
        InActive = 0, // Default, Khi ở trạng thái này thì không thể sử dụng loại cà phê này để tạo mới các bản ghi khác
        Active = 1,
        Unknown = 2
    }
}
