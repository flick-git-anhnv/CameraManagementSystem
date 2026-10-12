using CMS.Core.Enums;

namespace CMS.Core.Entities;

/// <summary>
/// Thiết bị camera được phát hiện qua mạng nội bộ (ONVIF WS-Discovery / Broadcast)
/// </summary>
public class DiscoveredDevice
{
    public string IPAddress { get; set; } = string.Empty;
    public int Port { get; set; } = 80;
    public string ServiceUrl { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;
    public string MacAddress { get; set; } = string.Empty;
    public CameraBrand DetectedBrand { get; set; } = CameraBrand.ONVIF;
    public DateTime DiscoveredAt { get; set; } = DateTime.Now;

    public override string ToString() => $"{Manufacturer} {Model} ({IPAddress}:{Port})";
}
