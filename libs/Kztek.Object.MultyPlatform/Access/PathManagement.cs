using System.IO;

namespace Kztek.Object
{
    /// <summary>
    /// Quản lý các đường dẫn file cấu hình và ảnh của hệ thống bãi đỗ xe.
    /// Cần gán <see cref="baseConfigBath"/> và <see cref="baseImagePath"/> khi khởi động ứng dụng.
    /// </summary>
    public class PathManagement
    {
        /// <summary>Thư mục gốc chứa các file cấu hình của ứng dụng (phải gán trước khi dùng).</summary>
        public static string baseConfigBath = string.Empty;

        /// <summary>Thư mục gốc lưu trữ ảnh chụp sự kiện xe vào/ra.</summary>
        public static string baseImagePath = string.Empty;

        /// <summary>Đường dẫn file cấu hình chính của ứng dụng (config.txt).</summary>
        public static string configPath() => Path.Combine(baseConfigBath, "config.txt");

        /// <summary>
        /// Đường dẫn file cấu hình độ trễ vòng từ (loop delay) cho một làn cụ thể.
        /// </summary>
        /// <param name="laneId">ID của làn xe cần lấy cấu hình.</param>
        public static string laneLoopDelayConfig(string laneId) => Path.Combine(configPath() + $"lane/{laneId}/loopDelay.txt");

        /// <summary>
        /// Đường dẫn file cấu hình nhóm thẻ ưu tiên cho một làn cụ thể.
        /// </summary>
        /// <param name="laneId">ID của làn xe cần lấy cấu hình.</param>
        public static string preferTicketsConfig(string laneId) => Path.Combine(configPath() + $"lane/{laneId}/preferTickets.txt");
    }
}
