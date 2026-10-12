namespace Kztek.Object.MultyPlatform.LegacyiParkingv8.Enums.Payments
{
    public enum EmVoucherType
    {
        REUSEABLE,
        NON_REUSEABLE,
    }
    public static class VoucherType
    {
        public static string ToDisplayString(EmVoucherType voucherType)
        {
            return voucherType switch
            {
                EmVoucherType.REUSEABLE => "Tái sử dung",
                EmVoucherType.NON_REUSEABLE => "Một lần",
                _ => string.Empty,
            };
        }
    }
}
