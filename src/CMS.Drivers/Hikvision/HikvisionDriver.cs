using CMS.Core.Entities;
using CMS.Core.Enums;
using CMS.Core.Interfaces;
using CMS.Drivers.Abstractions;
using System.Net.Http.Headers;
using System.Text;

namespace CMS.Drivers.Hikvision;

public class HikvisionDriver : BaseCameraDriver
{
    public override CameraBrand Brand => CameraBrand.Hikvision;
    public override string DisplayName => "Hikvision (ISAPI / RTSP)";

    public override StreamEndpoints BuildStreamUrls(CameraDevice camera)
    {
        string auth = FormatAuth(camera.Username, camera.Password);
        int ch = camera.Channel > 0 ? camera.Channel : 1;

        // Chuẩn Hikvision: Channel {ch}01 = Main-stream, {ch}02 = Sub-stream
        string main = string.IsNullOrEmpty(camera.CustomMainRtspUrl)
            ? $"rtsp://{auth}{camera.IpAddress}:{camera.RtspPort}/Streaming/Channels/{ch}01"
            : camera.CustomMainRtspUrl;

        string sub = string.IsNullOrEmpty(camera.CustomSubRtspUrl)
            ? $"rtsp://{auth}{camera.IpAddress}:{camera.RtspPort}/Streaming/Channels/{ch}02"
            : camera.CustomSubRtspUrl;

        return new StreamEndpoints(main, sub);
    }

    public override async Task<byte[]?> GetSnapshotAsync(CameraDevice camera, CancellationToken cancellationToken = default)
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
            int ch = camera.Channel > 0 ? camera.Channel : 1;
            string url = $"http://{camera.IpAddress}:{camera.HttpPort}/ISAPI/Streaming/channels/{ch}01/picture";

            if (!string.IsNullOrEmpty(camera.Username))
            {
                var authBytes = Encoding.ASCII.GetBytes($"{camera.Username}:{camera.Password}");
                http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(authBytes));
            }

            var resp = await http.GetAsync(url, cancellationToken);
            if (resp.IsSuccessStatusCode)
            {
                return await resp.Content.ReadAsByteArrayAsync(cancellationToken);
            }
        }
        catch
        {
            // Fallback sang các phương thức khác
        }

        return null;
    }

    public override async Task<bool> SendPtzCommandAsync(CameraDevice camera, PtzCommand command, double speed = 0.5, CancellationToken cancellationToken = default)
    {
        try
        {
            // Hikvision ISAPI PTZ continuous control
            int ch = camera.Channel > 0 ? camera.Channel : 1;
            string url = $"http://{camera.IpAddress}:{camera.HttpPort}/ISAPI/PTZCtrl/channels/{ch}/continuous";

            int pan = 0, tilt = 0, zoom = 0;
            int spd = (int)(speed * 100);

            switch (command)
            {
                case PtzCommand.Up: tilt = spd; break;
                case PtzCommand.Down: tilt = -spd; break;
                case PtzCommand.Left: pan = -spd; break;
                case PtzCommand.Right: pan = spd; break;
                case PtzCommand.UpLeft: pan = -spd; tilt = spd; break;
                case PtzCommand.UpRight: pan = spd; tilt = spd; break;
                case PtzCommand.DownLeft: pan = -spd; tilt = -spd; break;
                case PtzCommand.DownRight: pan = spd; tilt = -spd; break;
                case PtzCommand.ZoomIn: zoom = spd; break;
                case PtzCommand.ZoomOut: zoom = -spd; break;
                case PtzCommand.Stop: pan = 0; tilt = 0; zoom = 0; break;
            }

            string xml = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<PTZData>
    <pan>{pan}</pan>
    <tilt>{tilt}</tilt>
    <zoom>{zoom}</zoom>
</PTZData>";

            var handler = new HttpClientHandler
            {
                Credentials = new System.Net.NetworkCredential(camera.Username, camera.Password)
            };
            using var http = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(2) };

            var content = new StringContent(xml, Encoding.UTF8, "application/xml");
            var resp = await http.PutAsync(url, content, cancellationToken);
            return resp.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public override async Task<bool> PresetAsync(CameraDevice camera, string action, int presetIndex, CancellationToken cancellationToken = default)
    {
        try
        {
            int ch = camera.Channel > 0 ? camera.Channel : 1;
            var handler = new HttpClientHandler
            {
                Credentials = new System.Net.NetworkCredential(camera.Username, camera.Password)
            };
            using var http = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(3) };

            if (action.Equals("goto", StringComparison.OrdinalIgnoreCase))
            {
                string url = $"http://{camera.IpAddress}:{camera.HttpPort}/ISAPI/PTZCtrl/channels/{ch}/presets/{presetIndex}/goto";
                var resp = await http.PutAsync(url, null, cancellationToken);
                return resp.IsSuccessStatusCode;
            }
            else // set
            {
                string url = $"http://{camera.IpAddress}:{camera.HttpPort}/ISAPI/PTZCtrl/channels/{ch}/presets/{presetIndex}";
                string xml = $@"<?xml version=""1.0"" encoding=""UTF-8""?><PTZPreset><id>{presetIndex}</id><presetName>Preset{presetIndex}</presetName></PTZPreset>";
                var content = new StringContent(xml, Encoding.UTF8, "application/xml");
                var resp = await http.PutAsync(url, content, cancellationToken);
                return resp.IsSuccessStatusCode;
            }
        }
        catch
        {
            return false;
        }
    }
}
