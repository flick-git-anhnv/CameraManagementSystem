using iParkingv8.Object.Enums.CMS;

namespace Kztek.Object.MultyPlatform.LegacyiParkingv8.Objects.CMS
{
    /// <summary>
    /// Bản tin gửi lên khi cần hỗ trợ || Nhận lệnh từ server
    /// </summary>
    public class OperatorMessage
    {
        public string Id { get; set; } = string.Empty;
        public string LaneId { get; set; } = string.Empty;
        public string ControlUnitId { get; set; } = string.Empty;
        public EmOperatorMessageType MessageType { get; set; }
        public EmOperatorAction Action { get; set; }
        //public EventData Event { get; set; }
    }
}
