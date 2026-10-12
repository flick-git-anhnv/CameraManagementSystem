namespace Kztek.Object
{
    /// <summary>Nhóm enum ngày trong tuần — dùng để cấu hình lịch làm việc của nhân viên.</summary>
    public class DayOfWeek
    {
        /// <summary>
        /// Ngày trong tuần — dùng trong cấu hình ca làm việc (workingTimeConfigData).
        /// Lưu ý: khác với <see cref="System.DayOfWeek"/> (bắt đầu từ Sunday), enum này bắt đầu từ Monday.
        /// </summary>
        public enum EmDayOfWeek
        {
            /// <summary>Thứ Hai.</summary>
            Monday,
            /// <summary>Thứ Ba.</summary>
            Tuesday,
            /// <summary>Thứ Tư.</summary>
            Wednesday,
            /// <summary>Thứ Năm.</summary>
            Thursday,
            /// <summary>Thứ Sáu.</summary>
            Friday,
            /// <summary>Thứ Bảy.</summary>
            Saturday,
            /// <summary>Chủ Nhật.</summary>
            Sunday
        }
    }
}
