namespace Kztek.Object.MultyPlatform.ConfigObjects
{
    /// <summary>
    /// Cấu hình kết nối đến server nhận dạng khuôn mặt.
    /// </summary>
    public class FaceConfig
    {
        /// <summary>Loại engine nhận dạng khuôn mặt đang sử dụng.</summary>
        public EmFaceDetecter Type { get; set; } = EmFaceDetecter.KztekFace;

        /// <summary>Địa chỉ URL của face server (ví dụ: http://192.168.1.100:5000).</summary>
        public string Url { get; set; } = string.Empty;

        /// <summary>Tên đăng nhập xác thực với face server (nếu có).</summary>
        public string Username { get; set; } = string.Empty;

        /// <summary>Mật khẩu xác thực với face server (nếu có).</summary>
        public string Password { get; set; } = string.Empty;
    }

    /// <summary>Loại engine nhận dạng khuôn mặt được hỗ trợ.</summary>
    public enum EmFaceDetecter
    {
        /// <summary>Engine nhận dạng khuôn mặt của KZTEK (mặc định).</summary>
        KztekFace
    }
}
