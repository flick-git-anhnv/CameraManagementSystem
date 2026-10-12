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
}
