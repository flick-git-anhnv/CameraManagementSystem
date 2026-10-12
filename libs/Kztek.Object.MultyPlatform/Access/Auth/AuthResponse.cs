namespace Kztek.Object
{
    /// <summary>
    /// Phản hồi từ server sau khi đăng nhập thành công — chứa thông tin người dùng và token xác thực.
    /// </summary>
    public class AuthResponse
    {
        /// <summary>ID người dùng trong hệ thống.</summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>Tên đăng nhập.</summary>
        public string Username { get; set; } = string.Empty;

        /// <summary>Mật khẩu (thường trả về rỗng hoặc đã hash — không dùng để xác thực tiếp).</summary>
        public string Password { get; set; } = string.Empty;

        /// <summary>Tên hiển thị của người dùng.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>JWT/Bearer token dùng cho các request API tiếp theo.</summary>
        public string Token { get; set; } = string.Empty;
    }
}
