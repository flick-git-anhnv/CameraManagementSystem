using iParkingv8.Object.Enums.CMS;

namespace Kztek.Object.MultyPlatform.LegacyiParkingv8.Objects.CMS
{
    /// <summary>
    /// Dữ liệu trạng thái cập nhật lên rabbit
    /// </summary>
    public class ComputerMonitorMessage
    {
        public EmMonitorMessageType MessageType { get; set; } = EmMonitorMessageType.Heartbeat;
        public ComputerMonitor? Computer { get; set; }
    }
}
