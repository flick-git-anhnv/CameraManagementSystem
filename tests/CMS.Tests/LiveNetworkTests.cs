using CMS.Core.Entities;
using CMS.Core.Enums;
using CMS.Drivers;
using CMS.Drivers.Onvif;
using Xunit;

namespace CMS.Tests;

public class LiveNetworkTests
{
    [Fact]
    public async Task LiveNetwork_ScanIpRange_DiscoversOnlineCameras()
    {
        var discovery = new OnvifDiscoveryService();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        // Quét dải IP thực tế trong LAN văn phòng nơi có các camera Tiandy, AIView, Dahua
        var results = await discovery.ScanIpRangeAsync("192.168.21.180", "192.168.21.210", new[] { 554, 80 }, cts.Token);

        // Xác nhận phát hiện được các thiết bị thực tế đang hoạt động
        Assert.NotEmpty(results);
        Assert.Contains(results, d => d.IPAddress == "192.168.21.182" || d.IPAddress == "192.168.21.205");
    }

    [Fact]
    public async Task LiveNetwork_TiandyCamera_TestConnectionReturnsTrue()
    {
        var tiandyDriver = CameraDriverFactory.GetDriver(CameraBrand.Tiandy);
        var cam = new CameraDevice
        {
            Id = "cam-tiandy-live",
            Name = "Tiandy Live Camera",
            IpAddress = "192.168.21.182",
            RtspPort = 554,
            HttpPort = 80,
            Brand = CameraBrand.Tiandy
        };

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        bool isOnline = await tiandyDriver.TestConnectionAsync(cam, cts.Token);

        Assert.True(isOnline, "Camera Tiandy tại 192.168.21.182 phải phản hồi kết nối thành công");
    }

    [Fact]
    public async Task LiveNetwork_AiViewCamera_TestConnectionReturnsTrue()
    {
        var onvifDriver = CameraDriverFactory.GetDriver(CameraBrand.ONVIF);
        var cam = new CameraDevice
        {
            Id = "cam-aiview-live",
            Name = "AIView Live Camera",
            IpAddress = "192.168.21.205",
            RtspPort = 554,
            HttpPort = 80,
            Brand = CameraBrand.ONVIF
        };

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        bool isOnline = await onvifDriver.TestConnectionAsync(cam, cts.Token);

        Assert.True(isOnline, "Camera AIView tại 192.168.21.205 phải phản hồi kết nối thành công");
    }

    [Fact]
    public async Task LiveNetwork_AutoDiscoverAll_FindsLanCameras()
    {
        var discovery = new OnvifDiscoveryService();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        var results = await discovery.AutoDiscoverAllAsync(cts.Token);

        Assert.NotEmpty(results);
        Assert.Contains(results, d => d.IPAddress == "192.168.21.182" || d.IPAddress == "192.168.21.205");
    }
}
