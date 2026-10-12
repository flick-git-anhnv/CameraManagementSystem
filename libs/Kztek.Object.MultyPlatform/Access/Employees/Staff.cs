using System.Collections.Generic;

namespace Kztek.Object
{
    public class Staff
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;

        public FaceInfo? Face { get; set; } = null;
        public FingerInfo? Finger { get; set; } = null;
        public CardInfo? Card { get; set; } = null;
        public Dictionary<string, Dictionary<string, Shift>> workingTimeConfigData { get; set; }
             = new Dictionary<string, Dictionary<string, Shift>>();
    }
    public class FaceInfo
    {
        public List<string> FaceData { get; set; } = new List<string>();
    }
    public class FingerInfo
    {
        public List<string> FingerData { get; set; } = new List<string>();
    }
    public class CardInfo
    {
        public string CardNumber { get; set; } = string.Empty;
        public EmCardType CardType { get; set; } = EmCardType.Mifare;
    }

    /// <summary>Công nghệ thẻ từ sử dụng trong hệ thống kiểm soát ra vào.</summary>
    public enum EmCardType
    {
        /// <summary>Thẻ Proximity — đọc phần mã bên trái (byte thấp).</summary>
        Proximity_Left,
        /// <summary>Thẻ Proximity — đọc phần mã bên phải (byte cao).</summary>
        ProximityRight,
        /// <summary>Thẻ Mifare (ISO 14443-A) — phổ biến nhất trong bãi đỗ xe KZTEK.</summary>
        Mifare,
        /// <summary>Thẻ Mifare Ultralight — dung lượng nhỏ, thường dùng cho thẻ vé tạm.</summary>
        ultra,
    }
}
