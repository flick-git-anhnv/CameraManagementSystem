namespace Kztek.Object
{
    /// <summary>
    /// Dữ liệu đăng nhập gửi lên server để xác thực người dùng hệ thống.
    /// </summary>
    public class AuthRequest
    {
        /// <summary>Tên đăng nhập.</summary>
        public string Username { get; set; } = string.Empty;

        /// <summary>Mật khẩu (plain text — cần đảm bảo kết nối HTTPS khi truyền).</summary>
        public string Password { get; set; } = string.Empty;

        /// <summary>Dữ liệu bổ sung tùy chọn (ví dụ: thông tin thiết bị, version app). Mặc định <c>null</c>.</summary>
        public object? OptionalData { get; set; }

        /// <summary>
        /// Khởi tạo request đăng nhập với thông tin tên đăng nhập và mật khẩu.
        /// </summary>
        /// <param name="username">Tên đăng nhập.</param>
        /// <param name="password">Mật khẩu.</param>
        public AuthRequest(string username, string password)
        {
            OptionalData = null;
            Username = username;
            Password = password;
        }
    }
}
