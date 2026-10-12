using CMS.Core.Entities;
using CMS.Core.Enums;
using CMS.Core.Interfaces;
using CMS.Drivers.Abstractions;

namespace CMS.Drivers.GenericRtsp;

public class GenericRtspDriver : BaseCameraDriver
{
    private readonly CameraBrand _brand;

    public GenericRtspDriver(CameraBrand brand = CameraBrand.ONVIF)
    {
        _brand = brand;
    }

    public override CameraBrand Brand => _brand;
    public override string DisplayName => $"{_brand} (RTSP / ONVIF)";

    public override StreamEndpoints BuildStreamUrls(CameraDevice camera)
    {
        // 1. Nếu người dùng cấu hình URL tùy biến thủ công, ưu tiên dùng URL đó
        if (!string.IsNullOrEmpty(camera.CustomMainRtspUrl))
        {
            return new StreamEndpoints(
                camera.CustomMainRtspUrl,
                string.IsNullOrEmpty(camera.CustomSubRtspUrl) ? camera.CustomMainRtspUrl : camera.CustomSubRtspUrl);
        }

        string auth = FormatAuth(camera.Username, camera.Password);
        int ch = camera.Channel > 0 ? camera.Channel : 1;

        // 2. Mẫu URL theo từng hãng
        string main = "";
        string sub = "";

        switch (_brand)
        {
            case CameraBrand.Uniview:
                main = $"rtsp://{auth}{camera.IpAddress}:{camera.RtspPort}/unicast/c{ch}/s0/live";
                sub = $"rtsp://{auth}{camera.IpAddress}:{camera.RtspPort}/unicast/c{ch}/s1/live";
                break;

            case CameraBrand.Hanwha:
                main = $"rtsp://{auth}{camera.IpAddress}:{camera.RtspPort}/profile1/media.smp";
                sub = $"rtsp://{auth}{camera.IpAddress}:{camera.RtspPort}/profile2/media.smp";
                break;

            case CameraBrand.Tiandy:
                main = $"rtsp://{auth}{camera.IpAddress}:{camera.RtspPort}/live1.sdp";
                sub = $"rtsp://{auth}{camera.IpAddress}:{camera.RtspPort}/live2.sdp";
                break;

            case CameraBrand.Bosch:
                main = $"rtsp://{auth}{camera.IpAddress}:{camera.RtspPort}/rtsp_tunnel?inst=1";
                sub = $"rtsp://{auth}{camera.IpAddress}:{camera.RtspPort}/rtsp_tunnel?inst=2";
                break;

            case CameraBrand.Milesight:
                main = $"rtsp://{auth}{camera.IpAddress}:{camera.RtspPort}/main";
                sub = $"rtsp://{auth}{camera.IpAddress}:{camera.RtspPort}/sub";
                break;

            case CameraBrand.ZKTeco:
                main = $"rtsp://{auth}{camera.IpAddress}:{camera.RtspPort}/ch01/0";
                sub = $"rtsp://{auth}{camera.IpAddress}:{camera.RtspPort}/ch01/1";
                break;

            case CameraBrand.ONVIF:
            default:
                // Mẫu ONVIF phổ thông
                main = $"rtsp://{auth}{camera.IpAddress}:{camera.RtspPort}/onvif1";
                sub = $"rtsp://{auth}{camera.IpAddress}:{camera.RtspPort}/onvif2";
                break;
        }

        return new StreamEndpoints(main, sub);
    }

    public override async Task<bool> SendPtzCommandAsync(CameraDevice camera, PtzCommand command, double speed = 0.5, CancellationToken cancellationToken = default)
    {
        try
        {
            int ch = camera.Channel > 0 ? camera.Channel : 1;
            int spd = (int)Math.Max(1, Math.Min(8, speed * 8));

            // 1. Thử gửi qua chuẩn CGI phổ biến (Tiandy, Dahua, Uniview, OEM)
            string cgiCode = command switch
            {
                PtzCommand.Up => "Up",
                PtzCommand.Down => "Down",
                PtzCommand.Left => "Left",
                PtzCommand.Right => "Right",
                PtzCommand.UpLeft => "LeftUp",
                PtzCommand.UpRight => "RightUp",
                PtzCommand.DownLeft => "LeftDown",
                PtzCommand.DownRight => "RightDown",
                PtzCommand.ZoomIn => "ZoomTele",
                PtzCommand.ZoomOut => "ZoomWide",
                PtzCommand.FocusNear => "FocusNear",
                PtzCommand.FocusFar => "FocusFar",
                _ => ""
            };

            string cgiAction = (command == PtzCommand.Stop) ? "stop" : "start";
            string cgiUrl = $"http://{camera.IpAddress}:{camera.HttpPort}/cgi-bin/ptz.cgi?action={cgiAction}&channel={ch}&code={cgiCode}&arg1=0&arg2={spd}&arg3=0";

            var handler = new System.Net.Http.HttpClientHandler
            {
                Credentials = new System.Net.NetworkCredential(camera.Username, camera.Password)
            };
            using var http = new System.Net.Http.HttpClient(handler) { Timeout = TimeSpan.FromSeconds(2) };

            try
            {
                var cgiResp = await http.GetAsync(cgiUrl, cancellationToken);
                if (cgiResp.IsSuccessStatusCode)
                {
                    return true;
                }
            }
            catch
            {
                // Thử tiếp chuẩn ONVIF SOAP
            }

            // 2. Thử chuẩn ONVIF SOAP ContinuousMove & Stop
            double x = 0.0, y = 0.0, z = 0.0;
            switch (command)
            {
                case PtzCommand.Up: y = speed; break;
                case PtzCommand.Down: y = -speed; break;
                case PtzCommand.Left: x = -speed; break;
                case PtzCommand.Right: x = speed; break;
                case PtzCommand.UpLeft: x = -speed; y = speed; break;
                case PtzCommand.UpRight: x = speed; y = speed; break;
                case PtzCommand.DownLeft: x = -speed; y = -speed; break;
                case PtzCommand.DownRight: x = speed; y = -speed; break;
                case PtzCommand.ZoomIn: z = speed; break;
                case PtzCommand.ZoomOut: z = -speed; break;
                case PtzCommand.Stop: break;
            }

            string soapBody = (command == PtzCommand.Stop)
                ? $@"<?xml version=""1.0"" encoding=""utf-8""?>
<s:Envelope xmlns:s=""http://www.w3.org/2003/05/soap-envelope"" xmlns:ptz=""http://www.onvif.org/ver20/ptz/wsdl"">
  <s:Body>
    <ptz:Stop>
      <ptz:ProfileToken>Profile_{ch}</ptz:ProfileToken>
      <ptz:PanTilt>true</ptz:PanTilt>
      <ptz:Zoom>true</ptz:Zoom>
    </ptz:Stop>
  </s:Body>
</s:Envelope>"
                : $@"<?xml version=""1.0"" encoding=""utf-8""?>
<s:Envelope xmlns:s=""http://www.w3.org/2003/05/soap-envelope"" xmlns:ptz=""http://www.onvif.org/ver20/ptz/wsdl"" xmlns:tt=""http://www.onvif.org/ver10/schema"">
  <s:Body>
    <ptz:ContinuousMove>
      <ptz:ProfileToken>Profile_{ch}</ptz:ProfileToken>
      <ptz:Velocity>
        <tt:PanTilt x=""{x:0.00}"" y=""{y:0.00}""/>
        <tt:Zoom x=""{z:0.00}""/>
      </ptz:Velocity>
    </ptz:ContinuousMove>
  </s:Body>
</s:Envelope>";

            var onvifEndpoints = new[]
            {
                $"http://{camera.IpAddress}:{camera.HttpPort}/onvif/ptz_service",
                $"http://{camera.IpAddress}:{camera.HttpPort}/onvif/PTZ",
                $"http://{camera.IpAddress}:{camera.HttpPort}/onvif/device_service"
            };

            var content = new System.Net.Http.StringContent(soapBody, System.Text.Encoding.UTF8, "application/soap+xml");
            foreach (var ep in onvifEndpoints)
            {
                try
                {
                    var resp = await http.PostAsync(ep, content, cancellationToken);
                    if (resp.IsSuccessStatusCode)
                    {
                        return true;
                    }
                }
                catch
                {
                }
            }
        }
        catch
        {
        }

        return false;
    }

    public override async Task<bool> PresetAsync(CameraDevice camera, string action, int presetIndex, CancellationToken cancellationToken = default)
    {
        try
        {
            int ch = camera.Channel > 0 ? camera.Channel : 1;
            string code = action.Equals("goto", StringComparison.OrdinalIgnoreCase) ? "GotoPreset" : "SetPreset";
            string cgiUrl = $"http://{camera.IpAddress}:{camera.HttpPort}/cgi-bin/ptz.cgi?action=start&channel={ch}&code={code}&arg1=0&arg2={presetIndex}&arg3=0";

            var handler = new System.Net.Http.HttpClientHandler
            {
                Credentials = new System.Net.NetworkCredential(camera.Username, camera.Password)
            };
            using var http = new System.Net.Http.HttpClient(handler) { Timeout = TimeSpan.FromSeconds(3) };

            var resp = await http.GetAsync(cgiUrl, cancellationToken);
            return resp.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}

