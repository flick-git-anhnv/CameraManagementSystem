using CMS.Core.Entities;
using CMS.Core.Enums;
using CMS.Core.Interfaces;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace CMS.Drivers.Onvif;

public class OnvifDiscoveryService : IDeviceDiscoveryService
{
    private const string WsDiscoveryMulticastAddress = "239.255.255.250";
    private const int WsDiscoveryPort = 3702;

    public async Task<List<DiscoveredDevice>> DiscoverOnvifDevicesAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        var devices = new Dictionary<string, DiscoveredDevice>();
        var messageId = Guid.NewGuid().ToString("D");

        // Chuẩn WS-Discovery Probe Message cho ONVIF
        string probeMessage = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<Envelope xmlns:dn=""http://www.onvif.org/ver10/network/wsdl"" 
          xmlns=""http://www.w3.org/2003/05/soap-envelope"">
  <Header>
    <wsa:MessageID xmlns:wsa=""http://schemas.xmlsoap.org/ws/2004/08/addressing"">urn:uuid:{messageId}</wsa:MessageID>
    <wsa:To xmlns:wsa=""http://schemas.xmlsoap.org/ws/2004/08/addressing"">urn:schemas-xmlsoap-org:ws:2005:04:discovery</wsa:To>
    <wsa:Action xmlns:wsa=""http://schemas.xmlsoap.org/ws/2004/08/addressing"">http://schemas.xmlsoap.org/ws/2005/04/discovery/Probe</wsa:Action>
  </Header>
  <Body>
    <Probe xmlns=""http://schemas.xmlsoap.org/ws/2005/04/discovery"">
      <Types>dn:NetworkVideoTransmitter</Types>
    </Probe>
  </Body>
</Envelope>";

        byte[] requestBytes = Encoding.UTF8.GetBytes(probeMessage);

        using var client = new UdpClient();
        client.EnableBroadcast = true;
        client.Client.ReceiveTimeout = (int)timeout.TotalMilliseconds;

        var endpoint = new IPEndPoint(IPAddress.Parse(WsDiscoveryMulticastAddress), WsDiscoveryPort);

        try
        {
            await client.SendAsync(requestBytes, requestBytes.Length, endpoint);

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            while (stopwatch.Elapsed < timeout && !cancellationToken.IsCancellationRequested)
            {
                if (client.Available > 0)
                {
                    var result = await client.ReceiveAsync(cancellationToken);
                    string responseXml = Encoding.UTF8.GetString(result.Buffer);

                    var parsed = ParseProbeMatch(responseXml, result.RemoteEndPoint.Address.ToString());
                    if (parsed != null && !devices.ContainsKey(parsed.IPAddress))
                    {
                        devices[parsed.IPAddress] = parsed;
                    }
                }
                else
                {
                    await Task.Delay(50, cancellationToken);
                }
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Socket timeout hoặc không có thêm phản hồi
        }

        return devices.Values.ToList();
    }

    public async Task<List<DiscoveredDevice>> ScanIpRangeAsync(string startIp, string endIp, int[] ports, CancellationToken cancellationToken = default)
    {
        var discovered = new List<DiscoveredDevice>();

        if (!IPAddress.TryParse(startIp, out var start) || !IPAddress.TryParse(endIp, out var end))
        {
            return discovered;
        }

        byte[] startBytes = start.GetAddressBytes();
        byte[] endBytes = end.GetAddressBytes();

        // Chỉ hỗ trợ quét IPv4
        if (startBytes.Length != 4 || endBytes.Length != 4)
            return discovered;

        uint startNum = (uint)(startBytes[0] << 24 | startBytes[1] << 16 | startBytes[2] << 8 | startBytes[3]);
        uint endNum = (uint)(endBytes[0] << 24 | endBytes[1] << 16 | endBytes[2] << 8 | endBytes[3]);

        if (startNum > endNum) return discovered;

        var tasks = new List<Task>();
        using var semaphore = new SemaphoreSlim(50); // Giới hạn 50 tác vụ đồng thời

        for (uint ipNum = startNum; ipNum <= endNum; ipNum++)
        {
            byte[] bytes = new byte[]
            {
                (byte)((ipNum >> 24) & 0xFF),
                (byte)((ipNum >> 16) & 0xFF),
                (byte)((ipNum >> 8) & 0xFF),
                (byte)(ipNum & 0xFF)
            };
            string currentIp = new IPAddress(bytes).ToString();

            tasks.Add(Task.Run(async () =>
            {
                await semaphore.WaitAsync(cancellationToken);
                try
                {
                    foreach (var port in ports)
                    {
                        if (cancellationToken.IsCancellationRequested) break;

                        using var tcp = new TcpClient();
                        var connectTask = tcp.ConnectAsync(currentIp, port);
                        var timeoutTask = Task.Delay(800, cancellationToken);

                        if (await Task.WhenAny(connectTask, timeoutTask) == connectTask && tcp.Connected)
                        {
                            lock (discovered)
                            {
                                if (!discovered.Any(d => d.IPAddress == currentIp))
                                {
                                    discovered.Add(new DiscoveredDevice
                                    {
                                        IPAddress = currentIp,
                                        Port = port,
                                        Manufacturer = "Generic / IP Camera",
                                        DetectedBrand = CameraBrand.ONVIF,
                                        ServiceUrl = $"http://{currentIp}:{port}/onvif/device_service"
                                    });
                                }
                            }
                            break;
                        }
                    }
                }
                catch
                {
                    // Port đóng
                }
                finally
                {
                    semaphore.Release();
                }
            }, cancellationToken));
        }

        await Task.WhenAll(tasks);
        return discovered;
    }

    private static DiscoveredDevice? ParseProbeMatch(string responseXml, string fallbackIp)
    {
        try
        {
            // Trích xuất XAddrs (URL dịch vụ của camera)
            var xaddrsMatch = Regex.Match(responseXml, @"<[^:]*:?XAddrs[^>]*>(.*?)</[^:]*:?XAddrs>", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            string serviceUrl = "";
            string ip = fallbackIp;
            int port = 80;

            if (xaddrsMatch.Success)
            {
                var urls = xaddrsMatch.Groups[1].Value.Trim().Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                if (urls.Length > 0)
                {
                    serviceUrl = urls[0];
                    if (Uri.TryCreate(serviceUrl, UriKind.Absolute, out var uri))
                    {
                        ip = uri.Host;
                        port = uri.Port > 0 ? uri.Port : 80;
                    }
                }
            }

            // Trích xuất scopes (Manufacturer, Model, Hardware...)
            string scopes = "";
            var scopesMatch = Regex.Match(responseXml, @"<[^:]*:?Scopes[^>]*>(.*?)</[^:]*:?Scopes>", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            if (scopesMatch.Success)
            {
                scopes = scopesMatch.Groups[1].Value;
            }

            var brand = DetectBrandFromScopes(scopes, responseXml);

            string model = ExtractScopeValue(scopes, "hardware/") ?? ExtractScopeValue(scopes, "name/") ?? "IP Camera";
            string manufacturer = ExtractScopeValue(scopes, "manufacturer/") ?? brand.ToString();

            return new DiscoveredDevice
            {
                IPAddress = ip,
                Port = port,
                ServiceUrl = serviceUrl,
                Manufacturer = manufacturer,
                Model = model,
                DetectedBrand = brand,
                DiscoveredAt = DateTime.Now
            };
        }
        catch
        {
            return null;
        }
    }

    private static CameraBrand DetectBrandFromScopes(string scopes, string rawXml)
    {
        string combined = (scopes + " " + rawXml).ToLowerInvariant();

        if (combined.Contains("hikvision")) return CameraBrand.Hikvision;
        if (combined.Contains("dahua")) return CameraBrand.Dahua;
        if (combined.Contains("uniview") || combined.Contains("uniview")) return CameraBrand.Uniview;
        if (combined.Contains("hanwha") || combined.Contains("samsung")) return CameraBrand.Hanwha;
        if (combined.Contains("tiandy")) return CameraBrand.Tiandy;
        if (combined.Contains("bosch")) return CameraBrand.Bosch;
        if (combined.Contains("pelco")) return CameraBrand.Pelco;
        if (combined.Contains("milesight")) return CameraBrand.Milesight;
        if (combined.Contains("zkteco")) return CameraBrand.ZKTeco;

        return CameraBrand.ONVIF;
    }

    private static string? ExtractScopeValue(string scopes, string prefix)
    {
        var match = Regex.Match(scopes, @"onvif://www\.onvif\.org/" + prefix + @"([^\s]+)", RegexOptions.IgnoreCase);
        if (match.Success)
        {
            return Uri.UnescapeDataString(match.Groups[1].Value);
        }
        return null;
    }
}
