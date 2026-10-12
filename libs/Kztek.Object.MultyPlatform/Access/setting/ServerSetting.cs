namespace Kztek.Object
{
    /// <summary>
    /// Cài đặt kết nối đến máy chủ quản lý bãi đỗ xe.
    /// Xác định địa chỉ server và nền tảng phần mềm để client chọn đúng giao thức API.
    /// </summary>
    public class ServerSetting
    {
        /// <summary>
        /// Địa chỉ URL gốc của server (ví dụ: http://192.168.1.10:5000).
        /// Các API endpoint được ghép thêm vào URL này.
        /// </summary>
        public string Url { get; set; } = string.Empty;

        /// <summary>
        /// Nền tảng phần mềm của server — xác định giao thức API client sử dụng.
        /// Mặc định <see cref="EmSoftwareMode.PHP"/> (tương thích ngược với iParkingv5/v6).
        /// </summary>
        public EmSoftwareMode SoftwareMode { get; set; } = EmSoftwareMode.PHP;
    }
}
