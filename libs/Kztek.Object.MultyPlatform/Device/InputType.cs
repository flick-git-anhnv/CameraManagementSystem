namespace Kztek.Object
{
    /// <summary>Nhóm kiểu tín hiệu đầu vào từ thiết bị kiểm soát bãi đỗ.</summary>
    public class InputTupe
    {
        /// <summary>
        /// Loại tín hiệu đầu vào (input) phát sinh từ thiết bị điều khiển barrier/bãi đỗ.
        /// </summary>
        public enum EmInputType
        {
            /// <summary>Vòng từ phát hiện xe (detector loop) kích hoạt.</summary>
            Loop,
            /// <summary>Tín hiệu thoát (xe đã ra khỏi khu vực).</summary>
            Exit,
            /// <summary>Thẻ đã được rút ra khỏi đầu đọc (card dispenser).</summary>
            CardbeTaken,
            /// <summary>Tín hiệu báo động (alarm).</summary>
            Alarm,
            /// <summary>Nút bấm thủ công (nút mở barrier tay).</summary>
            Button,
            /// <summary>Thẻ được đọc trên đầu đọc thẻ lối vào.</summary>
            Card,
            /// <summary>Thẻ được đọc trên đầu đọc thẻ lối ra.</summary>
            CardOut,
            /// <summary>Nút bấm bất thường (sự cố, khẩn cấp).</summary>
            ButtonAbnormal
        }
    }
}
