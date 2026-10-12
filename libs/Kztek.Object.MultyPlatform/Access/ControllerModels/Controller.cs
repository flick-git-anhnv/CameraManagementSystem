using System;

namespace Kztek.Object
{
    /// <summary>
    /// Thông tin cấu hình của một thiết bị kiểm soát ra vào (controller/camera).
    /// Đây là đối tượng trung tâm truyền giữa các layer trong hệ thống KZTEK.
    /// </summary>
    public class Controller
    {
        /// <summary>ID định danh duy nhất của thiết bị trong hệ thống.</summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>Tên hiển thị của thiết bị.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Địa chỉ IP kết nối TCP/IP (bỏ trống nếu dùng Serial).</summary>
        public string Ip { get; set; } = string.Empty;

        /// <summary>Cổng TCP kết nối (thường là 4370 cho ZKTeco, 8000 cho Dahua, v.v.).</summary>
        public int Port { get; set; }

        /// <summary>Tên đăng nhập xác thực với thiết bị.</summary>
        public string Username { get; set; } = string.Empty;

        /// <summary>Mật khẩu xác thực với thiết bị.</summary>
        public string Password { get; set; } = string.Empty;

        /// <summary>Địa chỉ thiết bị trên bus serial RS-485 (chỉ dùng khi giao tiếp Serial).</summary>
        public int Address { get; set; }

        /// <summary>Loại phần cứng của thiết bị — xác định driver/SDK sử dụng.</summary>
        public EmControllerType ControllerType { get; set; }

        /// <summary>Kiểu giao tiếp vật lý với thiết bị (TCP/IP, Serial, USB).</summary>
        public CommunicationType.EmCommunicationType CommunicationType { get; set; }

        /// <summary>Chế độ nhận dạng được bật trên thiết bị (có thể kết hợp thẻ + vân tay + khuôn mặt).</summary>
        public EmIdentifyMode IdentifyMode { get; set; }

        /// <summary>Loại thiết bị: Controller (kiểm soát ra vào) hoặc Camera.</summary>
        public EmDevicetype DeviceType { get; set; }

        /// <summary>Trạng thái kết nối hiện tại của thiết bị (<c>true</c> = đang kết nối).</summary>
        public bool IsConnect { get; set; }

        /// <summary>
        /// Loại phần cứng controller được hỗ trợ.
        /// Mỗi giá trị tương ứng với một driver/SDK tích hợp khác nhau.
        /// </summary>
        public enum EmControllerType
        {
            /// <summary>Không hỗ trợ / chưa xác định loại thiết bị.</summary>
            NOT_SUPPORT,
            /// <summary>Thiết bị AOPU.</summary>
            AOPU,
            /// <summary>Thiết bị HANNET.</summary>
            HANNET,
            /// <summary>Thiết bị Dahua (SDK Dahua NetSDK).</summary>
            DAHUA,
            /// <summary>Thiết bị FingerTec.</summary>
            FINGERTEC,
            /// <summary>Thiết bị ZKTeco (SDK ZKTeco).</summary>
            ZKTECO,
            /// <summary>Thiết bị KZTEK KZE-02 (Wiegand 26/34).</summary>
            KZE02,
            /// <summary>Thiết bị KZTEK KZE-05.</summary>
            KZE05,
            /// <summary>Thiết bị KZTEK KZE-32.</summary>
            KZE32,
            /// <summary>Thiết bị KZTEK KZELV (elevator control).</summary>
            KZELV,
            /// <summary>Thiết bị HANET (nhận dạng khuôn mặt).</summary>
            HANET,
            /// <summary>Thiết bị Tiandy (camera AI).</summary>
            Tiandy,
            /// <summary>Thiết bị KZTEK KZIO-0808 (8 input / 8 output).</summary>
            KZIO0808,
            /// <summary>Thiết bị KZTEK KZIO-1616 (16 input / 16 output).</summary>
            KZIO1616,
        }

        /// <summary>Loại thiết bị trong hệ thống kiểm soát.</summary>
        public enum EmDevicetype
        {
            /// <summary>Thiết bị kiểm soát ra vào (controller/BĐK).</summary>
            Controller = 1,
            /// <summary>Camera (RTSP, IP camera, AI camera).</summary>
            Camera = 2,
        }

        /// <summary>
        /// Chế độ nhận dạng được kích hoạt trên thiết bị — có thể kết hợp bằng bitwise OR.
        /// </summary>
        [Flags]
        public enum EmIdentifyMode
        {
            /// <summary>Nhận dạng bằng thẻ từ.</summary>
            Card = 1,
            /// <summary>Nhận dạng bằng vân tay.</summary>
            Finger = 2,
            /// <summary>Nhận dạng bằng khuôn mặt.</summary>
            Face = 4,
        }

        /// <summary>
        /// Tra cứu <see cref="EmControllerType"/> theo tên chuỗi (không phân biệt hoa/thường).
        /// </summary>
        /// <param name="controllerType">Tên loại controller cần tra.</param>
        /// <returns><see cref="EmControllerType"/> tương ứng, hoặc <see cref="EmControllerType.NOT_SUPPORT"/> nếu không tìm thấy.</returns>
        public EmControllerType GetControllerTypeByName(string controllerType)
        {
            foreach (EmControllerType item in Enum.GetValues(typeof(EmControllerType)))
            {
                string temp = item.ToString();
                if (item.ToString().ToUpper() == controllerType.ToUpper())
                {
                    return item;
                }
            }
            return EmControllerType.NOT_SUPPORT;
        }
    }
}
