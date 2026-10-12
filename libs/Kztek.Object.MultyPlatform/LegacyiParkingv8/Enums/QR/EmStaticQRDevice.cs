namespace Kztek.Object.MultyPlatform.LegacyiParkingv8.Enums.QR;

/// <summary>
/// Loại thiết bị hiển thị QR tĩnh.
/// Port từ <c>IParkingv8.QRScreenController.EmStaticQRDevice</c> (WinForms).
/// Giá trị enum khớp WinForms để backward-compat: <see cref="LaneOptionalConfig.StaticQRType"/> lưu dưới dạng <c>int</c>.
/// </summary>
public enum EmStaticQRDevice
{
    /// <summary>Thiết bị hiển thị QR View (màn hình QR tĩnh).</summary>
    QRVIEW = 0,
}
