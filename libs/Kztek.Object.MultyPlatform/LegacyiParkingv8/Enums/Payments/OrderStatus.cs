namespace Kztek.Object.MultyPlatform.LegacyiParkingv8.Enums.Payments
{
    public enum EmOrderStatus
    {
        SUCCESS,
        PENDING,
        CANCLED,
        FAILED
    }
    public static class OrderStatus
    {
        public static string ToDisplayString(this EmOrderStatus orderStatus)
        {
            return orderStatus switch
            {
                EmOrderStatus.SUCCESS => "Thành công",
                EmOrderStatus.PENDING => "Chờ",
                EmOrderStatus.CANCLED => "Hủy",
                EmOrderStatus.FAILED => "Lỗi",
                _ => string.Empty,
            };
        }
    }
}
