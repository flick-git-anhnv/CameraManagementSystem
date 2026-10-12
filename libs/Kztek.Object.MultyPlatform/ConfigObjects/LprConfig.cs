using static Kztek.Object.LprDetecter;

namespace Kztek.Object
{
    /// <summary>
    /// Cấu hình kết nối và tham số hoạt động của engine nhận dạng biển số xe (LPR).
    /// </summary>
    public class LprConfig
    {
        /// <summary>Loại engine LPR sử dụng (mặc định: KztekLPRAIServerV2).</summary>
        public EmLprDetecter LPRDetecterType { get; set; } = EmLprDetecter.KztekLPRAIServerV2;

        /// <summary>Địa chỉ URL của LPR server (ví dụ: http://192.168.1.100:8080).</summary>
        public string Url { get; set; } = string.Empty;

        /// <summary>Tên đăng nhập xác thực với LPR server (nếu có).</summary>
        public string Username { get; set; } = string.Empty;

        /// <summary>Mật khẩu xác thực với LPR server (nếu có).</summary>
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// Bật/tắt tính năng nhận dạng loại phương tiện (xe máy, ô tô, xe tải).
        /// Khi <c>true</c>, engine trả thêm thông tin loại phương tiện cùng với biển số.
        /// </summary>
        public bool IsDetectVehicleType { get; set; }

        /// <summary>
        /// Thời gian chờ (ms) giữa các lần chụp lại ảnh khi biển số chưa nhận dạng được.
        /// Mặc định 300ms.
        /// </summary>
        public int RetakePhotoDelay { get; set; } = 300;

        /// <summary>
        /// Số lần chụp lại tối đa khi biển số chưa nhận dạng được trước khi bỏ qua.
        /// Mặc định 5 lần.
        /// </summary>
        public int RetakePhotoTimes { get; set; } = 5;

        /// <summary>Mã thiết bị AI (dùng để đăng ký license với KZTEK AI Server).</summary>
        public string AIDeviceCode { get; set; } = string.Empty;

        /// <summary>Mã kích hoạt (activation code) cho KZTEK AI Server.</summary>
        public string AIActiveCode { get; set; } = string.Empty;
    }
}
