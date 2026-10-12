namespace Kztek.Object.Entity.ConfigObjects
{
    /// <summary>
    /// Cấu hình kết nối MQTT broker — dùng để nhận sự kiện real-time từ thiết bị hoặc hệ thống bên thứ ba.
    /// </summary>
    public class MQTTConfig
    {
        /// <summary>Bật/tắt sử dụng MQTT.</summary>
        public bool IsEnabled { get; set; }

        /// <summary>Địa chỉ MQTT broker (ví dụ: mqtt://192.168.1.10 hoặc tcp://host:1883).</summary>
        public string Url { get; set; } = string.Empty;

        /// <summary>Tên đăng nhập xác thực với MQTT broker.</summary>
        public string Username { get; set; } = string.Empty;

        /// <summary>Mật khẩu xác thực với MQTT broker.</summary>
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// Topic MQTT đăng ký nhận tin. Mặc định "#" — subscribe tất cả topic.
        /// Có thể chỉ định topic cụ thể, ví dụ: "parking/lane/+/event".
        /// </summary>
        public string Topic { get; set; } = "#";
    }
}
