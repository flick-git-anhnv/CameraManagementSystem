using CMS.Core.Entities;
using CMS.Core.Enums;

namespace CMS.Core.Interfaces;

public record StreamEndpoints(string MainStreamUrl, string SubStreamUrl);

/// <summary>
/// Interface trừu tượng cho driver của từng hãng camera hoặc chuẩn kết nối
/// </summary>
public interface ICameraDriver
{
    CameraBrand Brand { get; }
    string DisplayName { get; }

    /// <summary>
    /// Xây dựng URL RTSP cho camera (Main stream và Sub stream)
    /// </summary>
    StreamEndpoints BuildStreamUrls(CameraDevice camera);

    /// <summary>
    /// Kiểm tra kết nối tới camera (RTSP hoặc ONVIF handshake)
    /// </summary>
    Task<bool> TestConnectionAsync(CameraDevice camera, CancellationToken cancellationToken = default);

    /// <summary>
    /// Điều khiển PTZ (Pan/Tilt/Zoom) nếu camera hỗ trợ
    /// </summary>
    Task<bool> SendPtzCommandAsync(CameraDevice camera, PtzCommand command, double speed = 0.5, CancellationToken cancellationToken = default);

    /// <summary>
    /// Chụp ảnh tĩnh (Snapshot JPEG) trực tiếp từ camera
    /// </summary>
    Task<byte[]?> GetSnapshotAsync(CameraDevice camera, CancellationToken cancellationToken = default);
}
