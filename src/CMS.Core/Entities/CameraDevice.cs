using CMS.Core.Enums;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CMS.Core.Entities;

/// <summary>
/// Đại diện cho thiết bị camera trong hệ thống VMS
/// </summary>
public class CameraDevice : INotifyPropertyChanged
{
    private string _name = "Camera";
    private string _ipAddress = "192.168.1.100";
    private int _httpPort = 80;
    private int _rtspPort = 554;
    private string _username = "admin";
    private int _channel = 1;
    private CameraBrand _brand = CameraBrand.Hikvision;
    private bool _hasPtz = false;
    private DeviceStatus _status = DeviceStatus.Offline;
    private DateTime? _lastOnlineTime;
    private string _lastError = string.Empty;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string Name
    {
        get => _name;
        set { if (_name != value) { _name = value; OnPropertyChanged(); } }
    }

    public string Code { get; set; } = "CAM-01";
    public string GroupName { get; set; } = "Default";

    // Network & Authentication
    public string IpAddress
    {
        get => _ipAddress;
        set
        {
            if (_ipAddress != value)
            {
                _ipAddress = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ConnectionStatusDetail));
            }
        }
    }

    public int HttpPort
    {
        get => _httpPort;
        set
        {
            if (_httpPort != value)
            {
                _httpPort = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(EndpointSummary));
            }
        }
    }

    public int RtspPort
    {
        get => _rtspPort;
        set
        {
            if (_rtspPort != value)
            {
                _rtspPort = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(EndpointSummary));
                OnPropertyChanged(nameof(ConnectionStatusDetail));
            }
        }
    }

    public int OnvifPort { get; set; } = 80;

    public string Username
    {
        get => _username;
        set { if (_username != value) { _username = value; OnPropertyChanged(); } }
    }

    public string Password { get; set; } = "";

    // Channel & Stream
    public int Channel
    {
        get => _channel;
        set
        {
            if (_channel != value)
            {
                _channel = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(EndpointSummary));
            }
        }
    }

    public CameraBrand Brand
    {
        get => _brand;
        set { if (_brand != value) { _brand = value; OnPropertyChanged(); } }
    }

    public string CustomMainRtspUrl { get; set; } = string.Empty;
    public string CustomSubRtspUrl { get; set; } = string.Empty;

    // Capabilities & Features
    public bool HasPtz
    {
        get => _hasPtz;
        set
        {
            if (_hasPtz != value)
            {
                _hasPtz = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CapabilitySummary));
            }
        }
    }

    public bool HasAudio { get; set; } = false;
    public AiCapability EnabledAiFeatures { get; set; } = AiCapability.None;

    // Recording configuration
    public RecordingMode RecordMode { get; set; } = RecordingMode.Continuous247;
    public int SegmentMinutes { get; set; } = 15; // Mỗi file lưu 15 phút
    public bool RecordSubStream { get; set; } = false;
    public string ScheduleStartTime { get; set; } = "07:00";
    public string ScheduleEndTime { get; set; } = "19:00";
    public string ScheduleDays { get; set; } = "1,2,3,4,5,6,0"; // Thứ 2..Thứ 7, CN (DayOfWeek int)

    // Live state
    public DeviceStatus Status
    {
        get => _status;
        set
        {
            if (_status != value)
            {
                _status = value;
                NotifyStatusProperties();
            }
        }
    }

    public DateTime? LastOnlineTime
    {
        get => _lastOnlineTime;
        set
        {
            if (_lastOnlineTime != value)
            {
                _lastOnlineTime = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ConnectionStatusDetail));
            }
        }
    }

    public string LastError
    {
        get => _lastError;
        set
        {
            if (_lastError != value)
            {
                _lastError = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ConnectionStatusDetail));
            }
        }
    }

    private bool _isBeingEdited;
    public bool IsBeingEdited
    {
        get => _isBeingEdited;
        set
        {
            if (_isBeingEdited != value)
            {
                _isBeingEdited = value;
                OnPropertyChanged();
            }
        }
    }

    // Computed / Helper methods
    public bool IsOnline => Status == DeviceStatus.Online;
    public bool IsChecking => Status == DeviceStatus.Connecting;
    public bool IsOffline => Status != DeviceStatus.Online && Status != DeviceStatus.Connecting;

    public string StatusText => Status switch
    {
        DeviceStatus.Online => "● Đã kết nối",
        DeviceStatus.Connecting => "◌ Đang kiểm tra...",
        DeviceStatus.Offline => "● Không kết nối",
        DeviceStatus.Error => "● Lỗi kết nối",
        _ => "● Chưa kiểm tra"
    };

    public string EndpointSummary => $"RTSP: {RtspPort} • HTTP: {HttpPort} • Kênh {Channel}";

    public string CapabilitySummary => HasPtz ? "🕹️ Có PTZ" : "📷 Cố định";

    public string ConnectionStatusDetail => Status switch
    {
        DeviceStatus.Online => LastOnlineTime.HasValue
            ? $"Trực tuyến • Phản hồi lúc {LastOnlineTime.Value:HH:mm:ss}"
            : "Trực tuyến • Cổng kết nối mở",
        DeviceStatus.Connecting => $"Đang kiểm tra tới {IpAddress}:{RtspPort}...",
        DeviceStatus.Offline => !string.IsNullOrWhiteSpace(LastError)
            ? LastError
            : $"Không phản hồi tại {IpAddress}:{RtspPort}",
        DeviceStatus.Error => !string.IsNullOrWhiteSpace(LastError)
            ? LastError
            : "Lỗi kết nối thiết bị",
        _ => "Nhấn 'Kiểm tra' để xác minh kết nối"
    };

    public void NotifyStatusProperties()
    {
        OnPropertyChanged(nameof(Status));
        OnPropertyChanged(nameof(IsOnline));
        OnPropertyChanged(nameof(IsChecking));
        OnPropertyChanged(nameof(IsOffline));
        OnPropertyChanged(nameof(StatusText));
        OnPropertyChanged(nameof(ConnectionStatusDetail));
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
