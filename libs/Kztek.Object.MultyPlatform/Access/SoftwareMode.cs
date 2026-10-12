namespace Kztek.Object
{
    /// <summary>
    /// Nền tảng phần mềm của máy chủ quản lý bãi đỗ xe.
    /// Xác định giao thức API mà client sẽ sử dụng để giao tiếp.
    /// </summary>
    public enum EmSoftwareMode
    {
        /// <summary>Máy chủ chạy trên nền PHP (iParkingv5/v6 — API theo chuẩn cũ).</summary>
        PHP,
        /// <summary>Máy chủ chạy trên nền .NET (iParkingv7/v8 — API RESTful mới).</summary>
        DotNET
    }
}
