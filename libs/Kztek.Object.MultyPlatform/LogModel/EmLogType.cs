namespace Kztek.Object
{
    /// <summary>Mức độ của một bản ghi log trong hệ thống.</summary>
    public enum EmLogType
    {
        /// <summary>Thông tin thông thường, không có vấn đề gì.</summary>
        INFO,
        /// <summary>Lỗi xảy ra, cần xem xét và xử lý.</summary>
        ERROR,
        /// <summary>Cảnh báo — hệ thống vẫn hoạt động nhưng có điều bất thường.</summary>
        WARNING,
    }
}
