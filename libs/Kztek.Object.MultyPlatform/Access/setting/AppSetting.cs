namespace Kztek.Object
{
    /// <summary>
    /// Cài đặt chung của ứng dụng — được lưu vào file config và đọc lên khi khởi động.
    /// </summary>
    public class AppSetting
    {
        /// <summary>Tự động đăng nhập khi khởi động ứng dụng (không hiện màn hình login).</summary>
        public bool IsAutoLogin { get; set; }

        /// <summary>Ẩn giao diện chính vào system tray khi đóng cửa sổ.</summary>
        public bool IsHideView { get; set; }

        /// <summary>Khởi động ứng dụng cùng Windows (tự chạy khi bật máy).</summary>
        public bool IsStartWithWindow { get; set; }

        /// <summary>Tự động khởi động lại ứng dụng khi gặp lỗi nghiêm trọng.</summary>
        public bool IsAutoRestartApp { get; set; }

        /// <summary>Tự động đồng bộ dữ liệu nhân viên xuống thiết bị khi có thay đổi từ server.</summary>
        public bool IsAutoDownloadUser { get; set; }

        /// <summary>Thông tin kết nối đến máy chủ quản lý (URL + chế độ PHP/DotNET).</summary>
        public ServerSetting ServerSetting { get; set; } = new ServerSetting();

        /// <summary>Địa chỉ URL của server nhận dạng khuôn mặt riêng (nếu có).</summary>
        public string FaceServerUrl { get; set; } = string.Empty;

        /// <summary>
        /// Kiểu lưu trữ log dịch vụ — giá trị là <see cref="EmLogServiceType"/> ép kiểu sang int.
        /// </summary>
        public int LogServiceType { get; set; }
    }
}
