namespace Kztek.Object
{
    /// <summary>
    /// Cấu hình kết nối RabbitMQ — dùng để phát/nhận sự kiện bãi đỗ xe qua message broker.
    /// </summary>
    public class RabbitMQConfig
    {
        /// <summary>Bật/tắt sử dụng RabbitMQ.</summary>
        public bool IsEnabled { get; set; }

        /// <summary>Địa chỉ host của RabbitMQ server (IP hoặc hostname).</summary>
        public string RabbitMqUrl { get; set; } = string.Empty;

        /// <summary>Tên đăng nhập xác thực với RabbitMQ.</summary>
        public string RabbitMqUsername { get; set; } = string.Empty;

        /// <summary>Mật khẩu xác thực với RabbitMQ.</summary>
        public string RabbitMqPassword { get; set; } = string.Empty;

        /// <summary>Tên exchange để publish/subscribe message (ví dụ: "parking.events").</summary>
        public string RabbitMQExchangeName { get; set; } = string.Empty;

        /// <summary>Routing key định tuyến message đến đúng queue.</summary>
        public string RabbitMQRoutingKey { get; set; } = string.Empty;

        /// <summary>Cổng kết nối RabbitMQ (mặc định AMQP: 5672).</summary>
        public int Port { get; set; }
    }
}
