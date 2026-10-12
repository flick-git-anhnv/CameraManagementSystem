using CMS.Core.Entities;
using CMS.Core.Enums;
using CMS.Drivers.Onvif;
using System.Net;
using Xunit;

namespace CMS.Tests;

public class OnvifDiscoveryTests
{
    // ─────────────────────────────────────────────────────────────────────────
    // 1. WS-Discovery Probe Matches XML Parsing Tests
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ParseProbeMatch_HikvisionSample_ParsesCorrectly()
    {
        string hikvisionXml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<s:Envelope xmlns:s=""http://www.w3.org/2003/05/soap-envelope"" xmlns:wsa=""http://schemas.xmlsoap.org/ws/2004/08/addressing"" xmlns:d=""http://schemas.xmlsoap.org/ws/2005/04/discovery"">
  <s:Header>
    <wsa:MessageID>urn:uuid:12345678-1234-1234-1234-123456789abc</wsa:MessageID>
    <wsa:RelatesTo>urn:uuid:87654321-4321-4321-4321-cba987654321</wsa:RelatesTo>
    <wsa:To>http://schemas.xmlsoap.org/ws/2004/08/addressing/role/anonymous</wsa:To>
    <wsa:Action>http://schemas.xmlsoap.org/ws/2005/04/discovery/ProbeMatches</wsa:Action>
  </s:Header>
  <s:Body>
    <d:ProbeMatches>
      <d:ProbeMatch>
        <wsa:EndpointReference>
          <wsa:Address>urn:uuid:01234567-89ab-cdef-0123-456789abcdef</wsa:Address>
        </wsa:EndpointReference>
        <d:Types>dn:NetworkVideoTransmitter</d:Types>
        <d:Scopes>onvif://www.onvif.org/type/video_encoder onvif://www.onvif.org/type/audio_encoder onvif://www.onvif.org/hardware/DS-2CD2043G0-I onvif://www.onvif.org/name/HIKVISION%20DS-2CD2043G0-I onvif://www.onvif.org/manufacturer/HIKVISION</d:Scopes>
        <d:XAddrs>http://192.168.1.64/onvif/device_service</d:XAddrs>
        <d:MetadataVersion>10</d:MetadataVersion>
      </d:ProbeMatch>
    </d:ProbeMatches>
  </s:Body>
</s:Envelope>";

        var device = OnvifDiscoveryService.ParseProbeMatch(hikvisionXml, "192.168.1.64");

        Assert.NotNull(device);
        Assert.Equal("192.168.1.64", device.IPAddress);
        Assert.Equal(80, device.Port);
        Assert.Equal("http://192.168.1.64/onvif/device_service", device.ServiceUrl);
        Assert.Equal(CameraBrand.Hikvision, device.DetectedBrand);
        Assert.Equal("HIKVISION", device.Manufacturer);
        Assert.Equal("DS-2CD2043G0-I", device.Model);
    }

    [Fact]
    public void ParseProbeMatch_DahuaSample_ParsesCorrectly()
    {
        string dahuaXml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<s:Envelope xmlns:s=""http://www.w3.org/2003/05/soap-envelope"" xmlns:wsa=""http://schemas.xmlsoap.org/ws/2004/08/addressing"" xmlns:d=""http://schemas.xmlsoap.org/ws/2005/04/discovery"">
  <s:Body>
    <d:ProbeMatches>
      <d:ProbeMatch>
        <wsa:EndpointReference><wsa:Address>uuid:dahua-camera-001</wsa:Address></wsa:EndpointReference>
        <d:Scopes>onvif://www.onvif.org/type/NetworkVideoTransmitter onvif://www.onvif.org/manufacturer/Dahua onvif://www.onvif.org/hardware/IPC-HFW2431S onvif://www.onvif.org/name/DH-IPC-HFW2431S</d:Scopes>
        <d:XAddrs>http://192.168.1.108:80/onvif/device_service</d:XAddrs>
      </d:ProbeMatch>
    </d:ProbeMatches>
  </s:Body>
</s:Envelope>";

        var device = OnvifDiscoveryService.ParseProbeMatch(dahuaXml, "192.168.1.108");

        Assert.NotNull(device);
        Assert.Equal("192.168.1.108", device.IPAddress);
        Assert.Equal(80, device.Port);
        Assert.Equal(CameraBrand.Dahua, device.DetectedBrand);
        Assert.Equal("Dahua", device.Manufacturer);
        Assert.Equal("IPC-HFW2431S", device.Model);
    }

    [Fact]
    public void ParseProbeMatch_UniviewCustomPort_ParsesCorrectly()
    {
        string unvXml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Envelope xmlns=""http://www.w3.org/2003/05/soap-envelope"">
  <Body>
    <ProbeMatches xmlns=""http://schemas.xmlsoap.org/ws/2005/04/discovery"">
      <ProbeMatch>
        <Scopes>onvif://www.onvif.org/manufacturer/Uniview onvif://www.onvif.org/model/IPC2124LR3-PF40M</Scopes>
        <XAddrs>http://192.168.0.50:8088/onvif/device_service</XAddrs>
      </ProbeMatch>
    </ProbeMatches>
  </Body>
</Envelope>";

        var device = OnvifDiscoveryService.ParseProbeMatch(unvXml, "192.168.0.50");

        Assert.NotNull(device);
        Assert.Equal("192.168.0.50", device.IPAddress);
        Assert.Equal(8088, device.Port);
        Assert.Equal(CameraBrand.Uniview, device.DetectedBrand);
        Assert.Equal("IPC2124LR3-PF40M", device.Model);
    }

    [Fact]
    public void ParseProbeMatch_InvalidXml_ReturnsNull()
    {
        string corruptedXml = "THIS IS NOT XML CONTENT";
        var device = OnvifDiscoveryService.ParseProbeMatch(corruptedXml, "192.168.1.1");
        // Robust parser should not throw exception
        Assert.Null(device);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 2. Brand Detection & Scopes Heuristics Tests
    // ─────────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("onvif://www.onvif.org/name/HIKVISION", "", CameraBrand.Hikvision)]
    [InlineData("onvif://www.onvif.org/hardware/EZVIZ_C6N", "", CameraBrand.Hikvision)]
    [InlineData("onvif://www.onvif.org/manufacturer/Dahua", "", CameraBrand.Dahua)]
    [InlineData("onvif://www.onvif.org/name/IMOU_Cruiser", "", CameraBrand.Dahua)]
    [InlineData("onvif://www.onvif.org/name/Uniview-Camera", "", CameraBrand.Uniview)]
    [InlineData("onvif://www.onvif.org/manufacturer/UNV", "", CameraBrand.Uniview)]
    [InlineData("onvif://www.onvif.org/manufacturer/Hanwha", "", CameraBrand.Hanwha)]
    [InlineData("onvif://www.onvif.org/manufacturer/Samsung", "", CameraBrand.Hanwha)]
    [InlineData("onvif://www.onvif.org/name/Tiandy-TC-C32XN", "", CameraBrand.Tiandy)]
    [InlineData("onvif://www.onvif.org/hardware/Bosch-FLEXIDOME", "", CameraBrand.Bosch)]
    [InlineData("onvif://www.onvif.org/name/Pelco-Sarix", "", CameraBrand.Pelco)]
    [InlineData("onvif://www.onvif.org/manufacturer/Milesight", "", CameraBrand.Milesight)]
    [InlineData("onvif://www.onvif.org/name/ZKTeco-SpeedFace", "", CameraBrand.ZKTeco)]
    [InlineData("onvif://www.onvif.org/type/NetworkVideoTransmitter", "", CameraBrand.ONVIF)]
    public void DetectBrandFromScopes_CorrectlyMapsBrands(string scopes, string rawXml, CameraBrand expected)
    {
        var brand = OnvifDiscoveryService.DetectBrandFromScopes(scopes, rawXml);
        Assert.Equal(expected, brand);
    }

    [Fact]
    public void ExtractScopeValue_UrlEncodedCharacters_DecodesCorrectly()
    {
        string scopes = "onvif://www.onvif.org/name/Hikvision%20LPR%20Camera%20Entrance onvif://www.onvif.org/hardware/DS-2CD7A26G0";
        string? name = OnvifDiscoveryService.ExtractScopeValue(scopes, "name/");
        string? hardware = OnvifDiscoveryService.ExtractScopeValue(scopes, "hardware/");

        Assert.Equal("Hikvision LPR Camera Entrance", name);
        Assert.Equal("DS-2CD7A26G0", hardware);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 3. IP Range Scanner Tests
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ScanIpRangeAsync_InvalidIps_ReturnsEmpty()
    {
        var service = new OnvifDiscoveryService();
        var result = await service.ScanIpRangeAsync("invalid_ip", "not_an_ip", new[] { 80, 554 });
        Assert.Empty(result);
    }

    [Fact]
    public async Task ScanIpRangeAsync_StartGreaterThanEnd_ReturnsEmpty()
    {
        var service = new OnvifDiscoveryService();
        var result = await service.ScanIpRangeAsync("192.168.1.100", "192.168.1.50", new[] { 80 });
        Assert.Empty(result);
    }

    [Fact]
    public async Task ScanIpRangeAsync_CancellationRequested_TerminatesCleanly()
    {
        var service = new OnvifDiscoveryService();
        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        var result = await service.ScanIpRangeAsync("192.168.1.1", "192.168.1.254", new[] { 80 }, cts.Token);
        Assert.Empty(result);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 4. Live Broadcast Discovery Safe Execution
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task DiscoverOnvifDevicesAsync_RunsWithoutThrowing()
    {
        var service = new OnvifDiscoveryService();
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));

        // Must run safely and not throw exceptions across network adapters
        var devices = await service.DiscoverOnvifDevicesAsync(TimeSpan.FromMilliseconds(300), cts.Token);
        Assert.NotNull(devices);
    }
}
