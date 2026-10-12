namespace Kztek.Object.MultyPlatform.LegacyiParkingv8.Enums.QR;

/// <summary>
/// Mã ngân hàng Napas cho thiết bị hiển thị QR tĩnh.
/// Port từ <c>IParkingv8.QRScreenController.EmNapasBankCode</c> (WinForms).
/// Giá trị int là Napas bank code — chỉ dùng khi parse QR; UI hiển thị tên enum (<c>.ToString()</c>).
/// <see cref="LaneOptionalConfig.BankName"/> vẫn lưu <c>string</c> (tên enum) để backward-compat WinForms.
/// </summary>
public enum EmNapasBankCode
{
    VietinBank  = 970415,
    Vietcombank = 970436,
    BIDV        = 970418,
    Agribank    = 970405,
    OCB         = 970448,
    MBBank      = 970422,
    Techcombank = 970407,
    ACB         = 970416,
    VPBank      = 970432,
    TPBank      = 970423,
    HSBC        = 458761,
}
