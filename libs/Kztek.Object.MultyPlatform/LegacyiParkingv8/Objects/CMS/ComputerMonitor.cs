using iParkingv8.Object.Enums.CMS;

namespace Kztek.Object.MultyPlatform.LegacyiParkingv8.Objects.CMS
{
    public class ComputerMonitor
    {
        public string Name { get; set; } = string.Empty;
        public string Ip { get; set; } = string.Empty;
        public EmDeviceStatus Status { get; set; }
        public List<CameraMonitor> Cameras { get; set; } = [];
        public List<ControlUnitMonitor> ControlUnits { get; set; } = [];
        public List<string> Message { get; set; } = [];
    }
}
