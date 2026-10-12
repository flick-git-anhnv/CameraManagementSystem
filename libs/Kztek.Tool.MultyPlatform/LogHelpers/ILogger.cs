using Kztek.Tool;
using System;
using System.Runtime.CompilerServices;

namespace Kztek.Tool.LogHelpers
{
    /// <summary>
    /// Hợp đồng ghi log đa loại (API, Device, System, User) hỗ trợ nhiều backend (file, SQLite).
    /// Tham số <c>[CallerMemberName]</c>, <c>[CallerLineNumber]</c>, <c>[CallerFilePath]</c> được điền tự động bởi compiler.
    /// </summary>
    public interface ILogger
    {
        /// <summary>Thư mục lưu file log hoặc database log.</summary>
        string SaveLogFolder { get; set; }

        /// <summary>Bật/tắt ghi log. Khi <c>false</c>, các lời gọi Save* bị bỏ qua.</summary>
        bool IsSaveLog { get; set; }

        /// <summary>Khởi tạo dịch vụ log (tạo DB, mở file, ...).</summary>
        void InitLogService();

        /// <summary>Giải phóng tài nguyên log (đóng kết nối DB, flush file).</summary>
        void Disconnect();

        /// <summary>Ghi log một lần gọi API (tổng hợp start–end).</summary>
        void SaveAPILog(ApiLog apiLog,
                        [CallerMemberName] string callerName = "",
                        [CallerLineNumber] int lineNumber = 0,
                        [CallerFilePath] string filePath = "");
        /// <summary>Ghi log chi tiết từng request/response của một lần gọi API.</summary>
        void SaveAPILogDetail(ApiLogDetail apiLogDetail,
                             [CallerMemberName] string callerName = "",
                             [CallerLineNumber] int lineNumber = 0,
                             [CallerFilePath] string filePath = "");

        /// <summary>Ghi log lệnh gửi/phản hồi tới thiết bị phần cứng (LED, barrier, camera, ...).</summary>
        void SaveDeviceLog(DeviceLog deviceLog,
                          [CallerMemberName] string callerName = "",
                          [CallerLineNumber] int lineNumber = 0,
                          [CallerFilePath] string filePath = "");

        /// <summary>Ghi log trạng thái kết nối thiết bị (online/offline/error).</summary>
        void SaveDeviceStatusLog(DeviceStatusLog deviceStatusLog,
                                [CallerMemberName] string callerName = "",
                                [CallerLineNumber] int lineNumber = 0,
                                [CallerFilePath] string filePath = "");

        /// <summary>Ghi log sự kiện hệ thống (khởi động, lỗi ứng dụng, cấu hình thay đổi, ...).</summary>
        void SaveSystemLog(SystemLog systemLog,
                                [CallerMemberName] string callerName = "",
                                [CallerLineNumber] int lineNumber = 0,
                                [CallerFilePath] string filePath = "");

        /// <summary>Ghi log hành động người dùng (đăng nhập, đăng xuất, thao tác quan trọng).</summary>
        void SaveUserLog(UserLog userLog,
                                [CallerMemberName] string callerName = "",
                                [CallerLineNumber] int lineNumber = 0,
                                [CallerFilePath] string filePath = "");

        /// <summary>Xóa log cũ hơn <paramref name="day"/> ngày.</summary>
        void ClearLogAfterDay(int day);

        /// <summary>Xóa log trước thời điểm <paramref name="time"/>.</summary>
        void ClearLogBeforeTime(DateTime time);

        /// <summary>Truy vấn log thô bằng câu SQL hoặc filter expression.</summary>
        string GetLogByQuery(string query);
    }
}

