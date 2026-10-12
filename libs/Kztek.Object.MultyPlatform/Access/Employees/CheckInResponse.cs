namespace Kztek.Object
{
    /// <summary>
    /// Kết quả kiểm tra chấm công (check-in/check-out) của nhân viên từ thiết bị hoặc server.
    /// </summary>
    public class CheckInResponse
    {
        /// <summary><c>true</c> nếu chấm công thành công và nhân viên được phép ra/vào.</summary>
        public bool IsSuccess { get; set; } = false;

        /// <summary>Thông báo lỗi nếu chấm công thất bại (ví dụ: "Ngoài giờ làm việc", "Không có quyền").</summary>
        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>Thông tin nhân viên được nhận dạng (null nếu không tìm thấy hoặc lỗi).</summary>
        public Staff? StaffInfo { get; set; }
    }
}
