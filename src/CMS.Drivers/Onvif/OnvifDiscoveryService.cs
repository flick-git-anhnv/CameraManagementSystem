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

        var localIps = new List<IPAddress>();
        try
        {
            foreach (var ni in System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus != System.Net.NetworkInformation.OperationalStatus.Up) continue;
                if (ni.NetworkInterfaceType == System.Net.NetworkInformation.NetworkInterfaceType.Loopback) continue;

                var ipProps = ni.GetIPProperties();
                foreach (var unicast in ipProps.UnicastAddresses)
                {
                    if (unicast.Address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        localIps.Add(unicast.Address);
                    }
                }
            }
        }
        catch
        {
        }

        if (localIps.Count == 0)
        {
            localIps.Add(IPAddress.Any);
        }

        var clients = new List<UdpClient>();
        var multicastEndpoint = new IPEndPoint(IPAddress.Parse(WsDiscoveryMulticastAddress), WsDiscoveryPort);
        var broadcastEndpoint = new IPEndPoint(IPAddress.Broadcast, WsDiscoveryPort);

        try
        {
            foreach (var localIp in localIps)
            {
                try
                {
                    var client = new UdpClient(new IPEndPoint(localIp, 0))
                    {
                        EnableBroadcast = true
                    };
                    client.Client.ReceiveTimeout = (int)timeout.TotalMilliseconds;

                    try
                    {
                        client.Client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastInterface, localIp.GetAddressBytes());
                    }
                    catch
                    {
                    }

                    clients.Add(client);

                    // Gửi multicast và broadcast trên từng card mạng
                    _ = client.SendAsync(requestBytes, requestBytes.Length, multicastEndpoint);
                    _ = client.SendAsync(requestBytes, requestBytes.Length, broadcastEndpoint);
                }
                catch
                {
                }
            }

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            while (stopwatch.Elapsed < timeout && !cancellationToken.IsCancellationRequested)
            {
                bool anyData = false;

                foreach (var client in clients)
                {
                    try
                    {
                        while (client.Available > 0)
                        {
                            anyData = true;
                            var result = await client.ReceiveAsync(cancellationToken);
                            string responseXml = Encoding.UTF8.GetString(result.Buffer);

                            var parsed = ParseProbeMatch(responseXml, result.RemoteEndPoint.Address.ToString());
                            if (parsed != null && !devices.ContainsKey(parsed.IPAddress))
                            {
                                devices[parsed.IPAddress] = parsed;
                            }
                        }
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                    }
                }

                if (!anyData)
                {
                    await Task.Delay(50, cancellationToken);
                }
            }
        }
        finally
        {
            foreach (var client in clients)
            {
                try
                {
                    client.Dispose();
                }
                catch
                {
                }
            }
        }

        return devices.Values.ToList();
    }

    public async Task<List<DiscoveredDevice>> ScanIpRangeAsync(string startIp, string endIp, int[] ports, CancellationToken cancellationToken = default)
    {
        var discovered = new List<DiscoveredDevice>();

        if (cancellationToken.IsCancellationRequested)
        {
            return discovered;
        }

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
        using var semaphore = new SemaphoreSlim(150); // Concurrency cao cho quét LAN nội bộ cực nhanh

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
                    if (cancellationToken.IsCancellationRequested) return;

                    // Kiểm tra đồng thời các cổng trên cùng 1 IP (timeout ngắn 350ms cho mạng LAN)
                    var probeTasks = ports.Select(async port =>
                    {
                        try
                        {
                            using var tcp = new TcpClient();
                            var connectTask = tcp.ConnectAsync(currentIp, port);
                            var timeoutTask = Task.Delay(350, cancellationToken);

                            if (await Task.WhenAny(connectTask, timeoutTask) == connectTask && tcp.Connected)
                            {
                                return port;
                            }
                        }
                        catch { }
                        return -1;
                    });

                    var probeResults = await Task.WhenAll(probeTasks);
                    var openPorts = probeResults.Where(p => p > 0).ToList();

                    if (openPorts.Count > 0)
                    {
                        int bestPort = openPorts.Contains(80) ? 80 :
                                       openPorts.Contains(443) ? 443 :
                                       openPorts.Contains(8000) ? 8000 :
                                       openPorts.Contains(37777) ? 80 :
                                       openPorts.Contains(8080) ? 8080 : 80;

                        var info = await IdentifyDeviceAsync(currentIp, openPorts, cancellationToken);
                        lock (discovered)
                        {
                            if (!discovered.Any(d => d.IPAddress == currentIp))
                            {
                                discovered.Add(new DiscoveredDevice
                                {
                                    IPAddress = currentIp,
                                    Port = bestPort,
                                    Manufacturer = info.manufacturer,
                                    Model = info.model,
                                    DetectedBrand = info.brand,
                                    ServiceUrl = $"http://{currentIp}:{bestPort}/onvif/device_service",
                                    DiscoveredAt = DateTime.Now
                                });
                            }
                        }
                    }
                }
                catch
                {
                }
                finally
                {
                    semaphore.Release();
                }
            }, cancellationToken));
        }

        try
        {
            await Task.WhenAll(tasks);
        }
        catch (OperationCanceledException)
        {
        }

        return discovered;
    }

    public async Task<List<DiscoveredDevice>> AutoDiscoverAllAsync(CancellationToken cancellationToken = default)
    {
        var devices = new Dictionary<string, DiscoveredDevice>();

        // 1. Quét WS-Discovery Multicast & Broadcast (1.5 giây)
        try
        {
            var wsDevices = await DiscoverOnvifDevicesAsync(TimeSpan.FromSeconds(1.5), cancellationToken);
            foreach (var d in wsDevices)
            {
                devices[d.IPAddress] = d;
            }
        }
        catch { }

        if (cancellationToken.IsCancellationRequested)
            return devices.Values.ToList();

        // 2. Tự động quét dải subnet nội bộ đang hoạt động
        var subnets = GetLocalSubnets();
        int[] cameraPorts = new[] { 554, 80, 443, 8000, 37777, 8080 };

        var scanTasks = new List<Task<List<DiscoveredDevice>>>();
        foreach (var (startIp, endIp) in subnets)
        {
            scanTasks.Add(ScanIpRangeAsync(startIp, endIp, cameraPorts, cancellationToken));
        }

        var results = await Task.WhenAll(scanTasks);
        foreach (var list in results)
        {
            foreach (var d in list)
            {
                if (!devices.ContainsKey(d.IPAddress))
                {
                    devices[d.IPAddress] = d;
                }
            }
        }

        return devices.Values.ToList();
    }

    private static List<(string startIp, string endIp)> GetLocalSubnets()
    {
        var list = new List<(string, string)>();
        try
        {
            foreach (var ni in System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus != System.Net.NetworkInformation.OperationalStatus.Up) continue;
                if (ni.NetworkInterfaceType == System.Net.NetworkInformation.NetworkInterfaceType.Loopback) continue;

                string desc = (ni.Description + " " + ni.Name).ToLowerInvariant();
                if (desc.Contains("virtual") || desc.Contains("hyper-v") || desc.Contains("wsl") || 
                    desc.Contains("vethernet") || desc.Contains("vmware") || desc.Contains("virtualbox") || 
                    desc.Contains("radmin") || desc.Contains("tap") || desc.Contains("bluetooth") || desc.Contains("vpn"))
                {
                    continue;
                }

                var ipProps = ni.GetIPProperties();
                foreach (var unicast in ipProps.UnicastAddresses)
                {
                    if (unicast.Address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        var bytes = unicast.Address.GetAddressBytes();
                        if (bytes[0] == 169 && bytes[1] == 254) continue; // APIPA
                        if (bytes[0] == 26) continue; // Radmin VPN
                        if (bytes[0] == 127) continue; // Loopback
                        if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) continue; // Virtual Class B

                        string prefix = $"{bytes[0]}.{bytes[1]}.{bytes[2]}.";
                        string start = $"{prefix}1";
                        string end = $"{prefix}254";
                        if (!list.Any(s => s.Item1 == start))
                        {
                            list.Add((start, end));
                        }
                    }
                }
            }
        }
        catch { }

        // Sắp xếp ưu tiên các dải mạng thực tế đang có camera
        list = list.OrderByDescending(s => s.Item1.Contains(".21.")).ThenByDescending(s => s.Item1.Contains(".1.")).ToList();

        if (list.Count == 0)
        {
            list.Add(("192.168.21.1", "192.168.21.254"));
            list.Add(("192.168.1.1", "192.168.1.254"));
        }

        return list;
    }

    private static async Task<(CameraBrand brand, string manufacturer, string model)> IdentifyDeviceAsync(string ip, List<int> openPorts, CancellationToken cancellationToken = default)
    {
        if (openPorts.Contains(37777)) return (CameraBrand.Dahua, "Dahua", "Dahua IPC / DVR");
        if (openPorts.Contains(8000)) return (CameraBrand.Hikvision, "Hikvision", "Hikvision IPC / NVR");

        // Thử đọc trang web quản trị qua HTTP / HTTPS (cho phép self-signed cert của camera)
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true
        };
        using var http = new HttpClient(handler) { Timeout = TimeSpan.FromMilliseconds(600) };

        var urlsToTest = new List<string>();
        if (openPorts.Contains(80)) urlsToTest.Add($"http://{ip}:80/");
        if (openPorts.Contains(443)) urlsToTest.Add($"https://{ip}:443/");
        if (openPorts.Contains(8080)) urlsToTest.Add($"http://{ip}:8080/");

        foreach (var url in urlsToTest)
        {
            try
            {
                var resp = await http.GetAsync(url, cancellationToken);
                string body = await resp.Content.ReadAsStringAsync(cancellationToken);
                string lower = body.ToLowerInvariant();

                if (lower.Contains("tiandy")) return (CameraBrand.Tiandy, "Tiandy", "Tiandy IP Camera");
                if (lower.Contains("hikvision") || lower.Contains("ezviz") || lower.Contains("hilook")) return (CameraBrand.Hikvision, "Hikvision", "Hikvision Camera");
                if (lower.Contains("dahua") || lower.Contains("imou")) return (CameraBrand.Dahua, "Dahua", "Dahua Camera");
                if (lower.Contains("uniview") || lower.Contains("unv")) return (CameraBrand.Uniview, "Uniview", "Uniview Camera");
                if (lower.Contains("aiview") || lower.Contains("bkav") || lower.Contains("nuxt")) return (CameraBrand.ONVIF, "Bkav AIView", "AIView Camera");
                if (lower.Contains("hanwha") || lower.Contains("samsung") || lower.Contains("wiseprv")) return (CameraBrand.Hanwha, "Hanwha", "Hanwha Camera");
                if (lower.Contains("kztek") || lower.Contains("e02")) return (CameraBrand.ONVIF, "KZTEK", "KZ-E02 Controller");
            }
            catch
            {
            }
        }

        return (CameraBrand.ONVIF, "Generic / ONVIF", "IP Camera");
    }

    internal static DiscoveredDevice? ParseProbeMatch(string responseXml, string fallbackIp)
    {
        try
        {
            // Trích xuất XAddrs (URL dịch vụ của camera)
            var xaddrsMatch = Regex.Match(responseXml, @"<[^:]*:?XAddrs[^>]*>(.*?)</[^:]*:?XAddrs>", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            var scopesMatch = Regex.Match(responseXml, @"<[^:]*:?Scopes[^>]*>(.*?)</[^:]*:?Scopes>", RegexOptions.Singleline | RegexOptions.IgnoreCase);

            if (!xaddrsMatch.Success && !scopesMatch.Success && !responseXml.Contains("ProbeMatch", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

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
            string scopes = scopesMatch.Success ? scopesMatch.Groups[1].Value : "";

            var brand = DetectBrandFromScopes(scopes, responseXml);

            string model = ExtractScopeValue(scopes, "hardware/") 
                           ?? ExtractScopeValue(scopes, "model/")
                           ?? ExtractScopeValue(scopes, "name/") 
                           ?? "IP Camera";
            string manufacturer = ExtractScopeValue(scopes, "manufacturer/") 
                                  ?? (brand != CameraBrand.ONVIF ? brand.ToString() : "Generic");

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

    internal static CameraBrand DetectBrandFromScopes(string scopes, string rawXml)
    {
        string combined = (scopes + " " + rawXml).ToLowerInvariant();

        if (combined.Contains("hikvision") || combined.Contains("ezviz") || combined.Contains("hilook")) return CameraBrand.Hikvision;
        if (combined.Contains("dahua") || combined.Contains("imou") || combined.Contains("dh-")) return CameraBrand.Dahua;
        if (combined.Contains("uniview") || combined.Contains("unv")) return CameraBrand.Uniview;
        if (combined.Contains("hanwha") || combined.Contains("samsung") || combined.Contains("wiseprv")) return CameraBrand.Hanwha;
        if (combined.Contains("tiandy")) return CameraBrand.Tiandy;
        if (combined.Contains("bosch")) return CameraBrand.Bosch;
        if (combined.Contains("pelco")) return CameraBrand.Pelco;
        if (combined.Contains("milesight")) return CameraBrand.Milesight;
        if (combined.Contains("zkteco")) return CameraBrand.ZKTeco;

        return CameraBrand.ONVIF;
    }

    internal static string? ExtractScopeValue(string scopes, string prefix)
    {
        var match = Regex.Match(scopes, @"onvif://www\.onvif\.org/" + prefix + @"([^\s""]+)", RegexOptions.IgnoreCase);
        if (match.Success)
        {
            return Uri.UnescapeDataString(match.Groups[1].Value);
        }
        return null;
    }
}
