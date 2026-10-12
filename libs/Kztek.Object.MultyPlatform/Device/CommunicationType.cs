namespace Kztek.Object
{
    /// <summary>
    /// Nhóm kiểu giao tiếp vật lý với thiết bị kiểm soát ra vào.
    /// </summary>
    public class CommunicationType
    {
        /// <summary>
        /// Kiểu giao tiếp với thiết bị.
        /// </summary>
        public enum EmCommunicationType
        {
            /// <summary>Kết nối qua mạng LAN/WAN bằng TCP/IP.</summary>
            TCP_IP,
            /// <summary>Kết nối qua cổng serial (RS-232/RS-485).</summary>
            SERIAL,
            /// <summary>Kết nối qua cổng USB.</summary>
            USB
        }

        /// <summary>
        /// Kiểm tra kiểu giao tiếp có phải TCP/IP không.
        /// </summary>
        /// <param name="communicationType">Kiểu giao tiếp cần kiểm tra.</param>
        /// <returns><c>true</c> nếu là TCP/IP.</returns>
        public static bool IS_TCP(EmCommunicationType communicationType)
        {
            return communicationType == EmCommunicationType.TCP_IP;
        }

        /// <summary>
        /// Trả về tên hiển thị của kiểu giao tiếp.
        /// </summary>
        /// <param name="communicationType">Kiểu giao tiếp.</param>
        /// <returns>Chuỗi tên hiển thị (ví dụ: "TCP IP", "Serial", "USB").</returns>
        public static string ToString(EmCommunicationType communicationType)
        {
            switch (communicationType)
            {
                case EmCommunicationType.TCP_IP:
                    return "TCP IP";
                case EmCommunicationType.SERIAL:
                    return "Serial";
                case EmCommunicationType.USB:
                    return "USB";
                default:
                    return "";
            }
        }
    }
}
