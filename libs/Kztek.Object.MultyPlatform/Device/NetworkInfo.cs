using static Kztek.Object.CommunicationType;

namespace Kztek.Object.Entity.Device
{
    /// <summary>
    /// Thông tin kết nối mạng/serial của một thiết bị phần cứng.
    /// Dùng chung cho cả TCP/IP và Serial (RS-232/RS-485).
    /// </summary>
    public class NetworkInfo
    {
        /// <summary>Kiểu giao tiếp vật lý với thiết bị (TCP/IP, Serial, USB).</summary>
        public EmCommunicationType CommunicationType { get; set; }

        /// <summary>
        /// Địa chỉ thiết bị trên bus serial (RS-485) — chỉ dùng khi <see cref="CommunicationType"/> là Serial.
        /// Với TCP/IP, giá trị này thường bằng 0.
        /// </summary>
        public int Address { get; set; } = 0;

        /// <summary>Tốc độ baud rate giao tiếp serial (ví dụ: 9600, 19200, 115200).</summary>
        public int Baudrate { get; set; }

        /// <summary>
        /// Địa chỉ cổng kết nối: tên COM port khi dùng Serial (ví dụ: "COM3"),
        /// hoặc địa chỉ IP khi dùng TCP/IP (ví dụ: "192.168.1.100").
        /// </summary>
        public string Comport { get; set; } = string.Empty;

        /// <summary>
        /// Tạo đối tượng <see cref="NetworkInfo"/> với giá trị mặc định (TCP/IP, loopback).
        /// </summary>
        /// <returns>NetworkInfo mặc định kết nối TCP/IP đến 127.0.0.1.</returns>
        public static NetworkInfo CreateDefault()
        {
            return new NetworkInfo()
            {
                CommunicationType = EmCommunicationType.TCP_IP,
                Address = 0,
                Baudrate = 100,
                Comport = "127.0.0.1"
            };
        }
    }
}
