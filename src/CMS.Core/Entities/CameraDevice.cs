using CMS.Core.Enums;

namespace CMS.Core.Entities;

/// <summary>
/// Đại diện cho thiết bị camera trong hệ thống VMS
/// </summary>
public class CameraDevice
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "Camera";
    public string Code { get; set; } = "CAM-01";
    public string GroupName { get; set; } = "Default";

    // Network & Authentication
    public string IpAddress { get; set; } = "192.168.1.100";
    public int HttpPort { get; set; } = 80;
    public int RtspPort { get; set; } = 554;
    public int OnvifPort { get; set; } = 80;
    public string Username { get; set; } = "admin";
    public string Password { get; set; } = "";

    // Channel & Stream
    public int Channel { get; set; } = 1;
    public CameraBrand Brand { get; set; } = CameraBrand.Hikvision;
    public string CustomMainRtspUrl { get; set; } = string.Empty;
    public string CustomSubRtspUrl { get; set; } = string.Empty;

    // Capabilities & Features
    public bool HasPtz { get; set; } = false;
    public bool HasAudio { get; set; } = false;
    public AiCapability EnabledAiFeatures { get; set; } = AiCapability.None;

    // Recording configuration
    public RecordingMode RecordMode { get; set; } = RecordingMode.Continuous247;
    public int SegmentMinutes { get; set; } = 15; // Mỗi file lưu 15 phút
    public bool RecordSubStream { get; set; } = false;

    // Live state
    public DeviceStatus Status { get; set; } = DeviceStatus.Offline;
    public DateTime? LastOnlineTime { get; set; }
    public string LastError { get; set; } = string.Empty;

    // Computed / Helper methods
    public bool IsOnline => Status == DeviceStatus.Online;
}
