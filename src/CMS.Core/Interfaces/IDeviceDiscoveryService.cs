using CMS.Core.Entities;

namespace CMS.Core.Interfaces;

public interface IDeviceDiscoveryService
{
    /// <summary>
    /// Phát hiện các camera trong mạng LAN qua ONVIF WS-Discovery (Multicast UDP)
    /// </summary>
    Task<List<DiscoveredDevice>> DiscoverOnvifDevicesAsync(TimeSpan timeout, CancellationToken cancellationToken = default);

    /// <summary>
    /// Quét dải IP (Ping & port check) để tìm thiết bị mở cổng RTSP/HTTP
    /// </summary>
    Task<List<DiscoveredDevice>> ScanIpRangeAsync(string startIp, string endIp, int[] ports, CancellationToken cancellationToken = default);
}
