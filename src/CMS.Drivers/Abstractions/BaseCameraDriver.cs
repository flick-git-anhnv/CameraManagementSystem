using CMS.Core.Entities;
using CMS.Core.Enums;
using CMS.Core.Interfaces;
using System.Net.Sockets;

namespace CMS.Drivers.Abstractions;

public abstract class BaseCameraDriver : ICameraDriver
{
    public abstract CameraBrand Brand { get; }
    public abstract string DisplayName { get; }

    public abstract StreamEndpoints BuildStreamUrls(CameraDevice camera);

    public virtual async Task<bool> TestConnectionAsync(CameraDevice camera, CancellationToken cancellationToken = default)
    {
        // 1. Kiểm tra mở cổng RTSP
        try
        {
            using var tcp = new TcpClient();
            var connectTask = tcp.ConnectAsync(camera.IpAddress, camera.RtspPort);
            var timeoutTask = Task.Delay(2500, cancellationToken);

            if (await Task.WhenAny(connectTask, timeoutTask) == connectTask && tcp.Connected)
            {
                return true;
            }
        }
        catch
        {
            // Port RTSP không phản hồi
        }

        // 2. Thử cổng HTTP nếu RTSP thất bại
        try
        {
            using var tcp = new TcpClient();
            var connectTask = tcp.ConnectAsync(camera.IpAddress, camera.HttpPort);
            var timeoutTask = Task.Delay(1500, cancellationToken);

            if (await Task.WhenAny(connectTask, timeoutTask) == connectTask && tcp.Connected)
            {
                return true;
            }
        }
        catch
        {
            // Port HTTP không phản hồi
        }

        return false;
    }

    public virtual Task<bool> SendPtzCommandAsync(CameraDevice camera, PtzCommand command, double speed = 0.5, CancellationToken cancellationToken = default)
    {
        // Mặc định trả về false nếu driver con chưa cài đặt
        return Task.FromResult(false);
    }

    public virtual Task<byte[]?> GetSnapshotAsync(CameraDevice camera, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<byte[]?>(null);
    }

    protected static string FormatAuth(string username, string password)
    {
        if (string.IsNullOrEmpty(username)) return "";
        return $"{Uri.EscapeDataString(username)}:{Uri.EscapeDataString(password)}@";
    }
}
