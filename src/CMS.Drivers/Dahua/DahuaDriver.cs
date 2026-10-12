using CMS.Core.Entities;
using CMS.Core.Enums;
using CMS.Core.Interfaces;
using CMS.Drivers.Abstractions;
using System.Net.Http.Headers;
using System.Text;

namespace CMS.Drivers.Dahua;

public class DahuaDriver : BaseCameraDriver
{
    public override CameraBrand Brand => CameraBrand.Dahua;
    public override string DisplayName => "Dahua (DH-CGI / RTSP)";

    public override StreamEndpoints BuildStreamUrls(CameraDevice camera)
    {
        string auth = FormatAuth(camera.Username, camera.Password);
        int ch = camera.Channel > 0 ? camera.Channel : 1;

        // Chuẩn Dahua: subtype=0 (Main-stream), subtype=1 (Sub-stream)
        string main = string.IsNullOrEmpty(camera.CustomMainRtspUrl)
            ? $"rtsp://{auth}{camera.IpAddress}:{camera.RtspPort}/cam/realmonitor?channel={ch}&subtype=0"
            : camera.CustomMainRtspUrl;

        string sub = string.IsNullOrEmpty(camera.CustomSubRtspUrl)
            ? $"rtsp://{auth}{camera.IpAddress}:{camera.RtspPort}/cam/realmonitor?channel={ch}&subtype=1"
            : camera.CustomSubRtspUrl;

        return new StreamEndpoints(main, sub);
    }

    public override async Task<byte[]?> GetSnapshotAsync(CameraDevice camera, CancellationToken cancellationToken = default)
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
            int ch = camera.Channel > 0 ? camera.Channel : 1;
            string url = $"http://{camera.IpAddress}:{camera.HttpPort}/cgi-bin/snapshot.cgi?channel={ch}";

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
        }

        return null;
    }

    public override async Task<bool> SendPtzCommandAsync(CameraDevice camera, PtzCommand command, double speed = 0.5, CancellationToken cancellationToken = default)
    {
        if (!camera.HasPtz) return false;

        try
        {
            int ch = camera.Channel > 0 ? camera.Channel : 1;
            int arg2 = (int)(speed * 8); // Dahua tốc độ 1-8

            string dahuaCmd = command switch
            {
                PtzCommand.Up => "Up",
                PtzCommand.Down => "Down",
                PtzCommand.Left => "Left",
                PtzCommand.Right => "Right",
                PtzCommand.ZoomIn => "ZoomTele",
                PtzCommand.ZoomOut => "ZoomWide",
                _ => ""
            };

            string action = (command == PtzCommand.Stop) ? "stop" : "start";
            string url = $"http://{camera.IpAddress}:{camera.HttpPort}/cgi-bin/ptz.cgi?action={action}&channel={ch}&code={dahuaCmd}&arg1=0&arg2={arg2}&arg3=0";

            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
            if (!string.IsNullOrEmpty(camera.Username))
            {
                var authBytes = Encoding.ASCII.GetBytes($"{camera.Username}:{camera.Password}");
                http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(authBytes));
            }

            var resp = await http.GetAsync(url, cancellationToken);
            return resp.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
