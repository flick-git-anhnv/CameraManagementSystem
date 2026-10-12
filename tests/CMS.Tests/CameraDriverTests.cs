using CMS.Core.Entities;
using CMS.Core.Enums;
using CMS.Core.Interfaces;
using CMS.Drivers;
using CMS.Drivers.Dahua;
using CMS.Drivers.GenericRtsp;
using CMS.Drivers.Hikvision;
using Xunit;

namespace CMS.Tests;

public class CameraDriverTests
{
    private static CameraDevice CreateCamera(
        string ip = "192.168.1.10",
        int rtspPort = 554,
        int httpPort = 80,
        string username = "admin",
        string password = "password123",
        int channel = 1,
        CameraBrand brand = CameraBrand.Hikvision,
        string? customMain = null,
        string? customSub = null,
        bool hasPtz = true)
    {
        return new CameraDevice
        {
            Id = Guid.NewGuid().ToString(),
            Name = "Test Cam",
            IpAddress = ip,
            RtspPort = rtspPort,
            HttpPort = httpPort,
            Username = username,
            Password = password,
            Channel = channel,
            Brand = brand,
            CustomMainRtspUrl = customMain,
            CustomSubRtspUrl = customSub,
            HasPtz = hasPtz
        };
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 1. CameraDriverFactory Tests
    // ─────────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(CameraBrand.Hikvision, typeof(HikvisionDriver))]
    [InlineData(CameraBrand.Dahua, typeof(DahuaDriver))]
    [InlineData(CameraBrand.Uniview, typeof(GenericRtspDriver))]
    [InlineData(CameraBrand.Hanwha, typeof(GenericRtspDriver))]
    [InlineData(CameraBrand.Tiandy, typeof(GenericRtspDriver))]
    [InlineData(CameraBrand.Bosch, typeof(GenericRtspDriver))]
    [InlineData(CameraBrand.Pelco, typeof(GenericRtspDriver))]
    [InlineData(CameraBrand.Milesight, typeof(GenericRtspDriver))]
    [InlineData(CameraBrand.ZKTeco, typeof(GenericRtspDriver))]
    [InlineData(CameraBrand.ONVIF, typeof(GenericRtspDriver))]
    [InlineData(CameraBrand.CustomRTSP, typeof(GenericRtspDriver))]
    public void DriverFactory_ResolvesExpectedDriver(CameraBrand brand, Type expectedType)
    {
        var driver = CameraDriverFactory.GetDriver(brand);
        Assert.NotNull(driver);
        Assert.IsAssignableFrom(expectedType, driver);
    }

    [Fact]
    public void DriverFactory_UnknownBrand_FallsBackToOnvif()
    {
        // Cast an undefined int to CameraBrand
        var unknownBrand = (CameraBrand)999;
        var driver = CameraDriverFactory.GetDriver(unknownBrand);
        Assert.NotNull(driver);
        Assert.Equal(CameraBrand.ONVIF, driver.Brand);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 2. Hikvision Driver Stream URL & Channels Tests
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Hikvision_Channel1_GeneratesChannel101And102()
    {
        var driver = new HikvisionDriver();
        var cam = CreateCamera(channel: 1, brand: CameraBrand.Hikvision);

        var endpoints = driver.BuildStreamUrls(cam);

        Assert.Equal("rtsp://admin:password123@192.168.1.10:554/Streaming/Channels/101", endpoints.MainStreamUrl);
        Assert.Equal("rtsp://admin:password123@192.168.1.10:554/Streaming/Channels/102", endpoints.SubStreamUrl);
    }

    [Fact]
    public void Hikvision_Channel2_GeneratesChannel201And202()
    {
        var driver = new HikvisionDriver();
        var cam = CreateCamera(channel: 2, brand: CameraBrand.Hikvision);

        var endpoints = driver.BuildStreamUrls(cam);

        Assert.Contains("/Streaming/Channels/201", endpoints.MainStreamUrl);
        Assert.Contains("/Streaming/Channels/202", endpoints.SubStreamUrl);
    }

    [Fact]
    public void Hikvision_CustomUrlOverride_PreservesCustomUrl()
    {
        var driver = new HikvisionDriver();
        var cam = CreateCamera(
            customMain: "rtsp://custom-nvr.lan:8554/hik/main",
            customSub: "rtsp://custom-nvr.lan:8554/hik/sub");

        var endpoints = driver.BuildStreamUrls(cam);

        Assert.Equal("rtsp://custom-nvr.lan:8554/hik/main", endpoints.MainStreamUrl);
        Assert.Equal("rtsp://custom-nvr.lan:8554/hik/sub", endpoints.SubStreamUrl);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 3. Dahua Driver Stream URL Tests
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Dahua_Channel1_GeneratesSubtype0And1()
    {
        var driver = new DahuaDriver();
        var cam = CreateCamera(channel: 1, brand: CameraBrand.Dahua);

        var endpoints = driver.BuildStreamUrls(cam);

        Assert.Equal("rtsp://admin:password123@192.168.1.10:554/cam/realmonitor?channel=1&subtype=0", endpoints.MainStreamUrl);
        Assert.Equal("rtsp://admin:password123@192.168.1.10:554/cam/realmonitor?channel=1&subtype=1", endpoints.SubStreamUrl);
    }

    [Fact]
    public void Dahua_Channel4_UrlReflectsChannelNumber()
    {
        var driver = new DahuaDriver();
        var cam = CreateCamera(channel: 4, brand: CameraBrand.Dahua);

        var endpoints = driver.BuildStreamUrls(cam);

        Assert.Contains("channel=4&subtype=0", endpoints.MainStreamUrl);
        Assert.Contains("channel=4&subtype=1", endpoints.SubStreamUrl);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 4. Generic RTSP Driver Multibrand Patterns Tests
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void GenericDriver_Uniview_GeneratesUnicastPath()
    {
        var driver = new GenericRtspDriver(CameraBrand.Uniview);
        var cam = CreateCamera(brand: CameraBrand.Uniview);

        var endpoints = driver.BuildStreamUrls(cam);

        Assert.Contains("/unicast/c1/s0/live", endpoints.MainStreamUrl);
        Assert.Contains("/unicast/c1/s1/live", endpoints.SubStreamUrl);
    }

    [Fact]
    public void GenericDriver_Hanwha_GeneratesProfilePath()
    {
        var driver = new GenericRtspDriver(CameraBrand.Hanwha);
        var cam = CreateCamera(brand: CameraBrand.Hanwha);

        var endpoints = driver.BuildStreamUrls(cam);

        Assert.Contains("/profile1/media.smp", endpoints.MainStreamUrl);
        Assert.Contains("/profile2/media.smp", endpoints.SubStreamUrl);
    }

    [Fact]
    public void GenericDriver_Tiandy_GeneratesLiveSdpPath()
    {
        var driver = new GenericRtspDriver(CameraBrand.Tiandy);
        var cam = CreateCamera(brand: CameraBrand.Tiandy);

        var endpoints = driver.BuildStreamUrls(cam);

        Assert.Contains("/live1.sdp", endpoints.MainStreamUrl);
        Assert.Contains("/live2.sdp", endpoints.SubStreamUrl);
    }

    [Fact]
    public void GenericDriver_Bosch_GeneratesTunnelPath()
    {
        var driver = new GenericRtspDriver(CameraBrand.Bosch);
        var cam = CreateCamera(brand: CameraBrand.Bosch);

        var endpoints = driver.BuildStreamUrls(cam);

        Assert.Contains("/rtsp_tunnel?inst=1", endpoints.MainStreamUrl);
        Assert.Contains("/rtsp_tunnel?inst=2", endpoints.SubStreamUrl);
    }

    [Fact]
    public void GenericDriver_ONVIF_GeneratesOnvif1And2()
    {
        var driver = new GenericRtspDriver(CameraBrand.ONVIF);
        var cam = CreateCamera(brand: CameraBrand.ONVIF);

        var endpoints = driver.BuildStreamUrls(cam);

        Assert.Contains("/onvif1", endpoints.MainStreamUrl);
        Assert.Contains("/onvif2", endpoints.SubStreamUrl);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 5. PTZ and Connection Test Guard Logic
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task SendPtzCommand_CameraWithoutPtz_ReturnsFalse()
    {
        var driver = new HikvisionDriver();
        var cam = CreateCamera(hasPtz: false);

        bool result = await driver.SendPtzCommandAsync(cam, PtzCommand.Up);
        Assert.False(result);
    }

    [Fact]
    public async Task Preset_CameraWithoutPtz_ReturnsFalse()
    {
        var driver = new DahuaDriver();
        var cam = CreateCamera(hasPtz: false);

        bool result = await driver.PresetAsync(cam, "goto", 1);
        Assert.False(result);
    }

    [Fact]
    public async Task TestConnectionAsync_UnreachableHost_ReturnsFalseWithoutThrowing()
    {
        var driver = new HikvisionDriver();
        // Using an unroutable IP (TEST-NET-1 192.0.2.1)
        var cam = CreateCamera(ip: "192.0.2.1", rtspPort: 554, httpPort: 80);

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));
        bool ok = await driver.TestConnectionAsync(cam, cts.Token);
        Assert.False(ok);
    }

    [Fact]
    public async Task GenericRtspDriver_SendPtzCommand_UnreachableHost_ReturnsFalseWithoutThrowing()
    {
        var driver = new GenericRtspDriver(CameraBrand.ONVIF);
        var cam = CreateCamera(ip: "192.0.2.1", httpPort: 80);

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));
        bool result = await driver.SendPtzCommandAsync(cam, PtzCommand.Up, 0.5, cts.Token);
        Assert.False(result);
    }
}
