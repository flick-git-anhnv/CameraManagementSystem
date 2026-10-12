
namespace Kztek.Object
{
    /// <summary>
    /// Cấu hình định dạng mã thẻ cho từng đầu đọc thẻ (reader) trên một thiết bị (BĐK).
    /// Cho phép mỗi reader xuất mã thẻ theo định dạng và nhóm thẻ khác nhau.
    /// </summary>
    public class CardFormatConfig
    {
        /// <summary>Chỉ số (index) của đầu đọc thẻ trên thiết bị (bắt đầu từ 0).</summary>
        public int ReaderIndex { get; set; }

        /// <summary>Định dạng xuất mã thẻ (Hex, Decimal, Reverse, v.v.).</summary>
        public CardFormat.EmCardFormat OutputFormat { get; set; } = CardFormat.EmCardFormat.HEXA;

        /// <summary>Tùy chọn độ dài tối thiểu khi xuất mã thẻ.</summary>
        public CardFormat.EmCardFormatOption OutputOption { get; set; } = CardFormat.EmCardFormatOption.Toi_Gian;

        /// <summary>ID nhóm thẻ áp dụng cho reader này (dùng để tra cứu cấu hình nghiệp vụ).</summary>
        public string CardGroupId { get; set; } = string.Empty;

        /// <summary>ID nhóm thẻ vé ngày thủ công — ghi đè nhóm mặc định khi cần xử lý vé ngày đặc biệt.</summary>
        public string DailyCardGroupIdManual { get; set; } = string.Empty;

        /// <summary>Chỉ số barrier sẽ đóng lại sau khi xe đi qua reader này (dạng chuỗi index).</summary>
        public string CloseBarrieIndex { get; set; } = string.Empty;
    }
}
