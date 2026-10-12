namespace Kztek.Object
{
    /// <summary>Nhóm kiểu định dạng mã thẻ từ đầu đọc thẻ gửi lên hệ thống.</summary>
    public class CardFormat
    {
        /// <summary>
        /// Định dạng xuất của mã số thẻ từ đầu đọc.
        /// Các định dạng khác nhau tạo ra chuỗi khác nhau từ cùng 1 thẻ vật lý.
        /// </summary>
        public enum EmCardFormat
        {
            /// <summary>Thập phân (decimal) — chuỗi số nguyên.</summary>
            DECIMA,
            /// <summary>Thập lục phân (hexadecimal) — chuỗi hex thường dùng nhất.</summary>
            HEXA,
            /// <summary>Hex đảo ngược byte (reverse hex).</summary>
            REHEXA,
            /// <summary>Thập phân đảo ngược byte.</summary>
            REDECIMA,
            /// <summary>Định dạng XXX:XXXXX — chia 2 phần bằng dấu hai chấm.</summary>
            XXX_XXXXX,
            /// <summary>Định dạng lấy phần mã sau của thẻ Proximity.</summary>
            PROXI_MA_SAU,
            /// <summary>Định dạng đặc thù cho hệ thống CTIN Đà Nẵng.</summary>
            CTIN_DANANG,
        }

        /// <summary>
        /// Tùy chọn độ dài tối thiểu khi xuất mã thẻ (pad số 0 bên trái nếu cần).
        /// </summary>
        public enum EmCardFormatOption
        {
            /// <summary>Không pad — xuất đúng độ dài thực tế của mã thẻ.</summary>
            Toi_Gian,
            /// <summary>Đảm bảo tối thiểu 8 ký tự (pad 0 bên trái nếu ngắn hơn).</summary>
            Min_8,
            /// <summary>Đảm bảo tối thiểu 10 ký tự (pad 0 bên trái nếu ngắn hơn).</summary>
            Min_10,
        }

        /// <summary>Trả về chuỗi hiển thị của định dạng thẻ.</summary>
        public static string ToString(EmCardFormat cardFormat) =>
            cardFormat == EmCardFormat.XXX_XXXXX ? "XXX:XXXXX" : cardFormat.ToString();

        /// <summary>Trả về chuỗi hiển thị tiếng Việt của tùy chọn độ dài mã thẻ.</summary>
        public static string ToString(EmCardFormatOption option) =>
            option switch
            {
                EmCardFormatOption.Toi_Gian => "Tối Giản",
                EmCardFormatOption.Min_8 => "Tối Thiểu 8 Ký Tự",
                EmCardFormatOption.Min_10 => "Tối Thiểu 10 Ký Tự",
                _ => ""
            };
    }
}
