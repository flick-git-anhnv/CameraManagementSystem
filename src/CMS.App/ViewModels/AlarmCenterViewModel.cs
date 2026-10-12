using CMS.Core.Entities;
using CMS.Core.Enums;
using CMS.Core.Interfaces;
using CMS.Infrastructure.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace CMS.App.ViewModels;

public partial class AlarmItemViewModel : ObservableObject
{
    private readonly AlarmEvent _model;

    public AlarmEvent Model => _model;

    public string Id => _model.Id;
    public string CameraId => _model.CameraId;
    public string CameraName => _model.CameraName;
    public AlarmEventType EventType => _model.EventType;
    public AlarmLevel Level => _model.Level;
    public DateTime Timestamp => _model.Timestamp;
    public string TimeFormatted => _model.Timestamp.ToString("yyyy-MM-dd HH:mm:ss");
    public string Description => _model.Description;
    public string? SnapshotPath => _model.SnapshotPath;

    [ObservableProperty]
    private bool _isHandled;

    [ObservableProperty]
    private string? _handledBy;

    public string LevelBadgeColor => Level switch
    {
        AlarmLevel.Critical => "#DC2626", // Red
        AlarmLevel.Warning => "#D97706",  // Amber
        _ => "#2563EB"                     // Blue
    };

    public string LevelName => Level switch
    {
        AlarmLevel.Critical => "CRITICAL",
        AlarmLevel.Warning => "WARNING",
        _ => "INFO"
    };

    public string EventTypeIcon => EventType switch
    {
        AlarmEventType.MotionDetected => "🏃",
        AlarmEventType.LineCrossing => "🚧",
        AlarmEventType.Intrusion => "🚨",
        AlarmEventType.FaceBlacklist => "👤❌",
        AlarmEventType.PlateBlacklist => "🚗❌",
        AlarmEventType.CameraOffline => "🔌",
        _ => "⚠️"
    };

    public AlarmItemViewModel(AlarmEvent model)
    {
        _model = model;
        _isHandled = model.IsHandled;
        _handledBy = model.HandledBy;
    }

    public void MarkHandled(string operatorName = "Admin")
    {
        IsHandled = true;
        HandledBy = operatorName;
        _model.IsHandled = true;
        _model.HandledBy = operatorName;
        _model.HandledAt = DateTime.Now;
    }
}

public partial class AlarmCenterViewModel : ObservableObject
{
    private readonly SqliteAlarmRepository _alarmRepo;
    private readonly ICameraRepository _cameraRepo;

    [ObservableProperty]
    private ObservableCollection<AlarmItemViewModel> _alarms = new();

    [ObservableProperty]
    private ObservableCollection<AlarmItemViewModel> _filteredAlarms = new();

    [ObservableProperty]
    private AlarmItemViewModel? _selectedAlarm;

    [ObservableProperty]
    private string _filterCamera = "All Cameras";

    [ObservableProperty]
    private ObservableCollection<string> _cameraOptions = new() { "All Cameras" };

    [ObservableProperty]
    private int _unreadCount = 0;

    [ObservableProperty]
    private string _statusSummary = "Monitoring Alarm & Event stream";

    public AlarmCenterViewModel(SqliteAlarmRepository? alarmRepo = null, ICameraRepository? cameraRepo = null)
    {
        _alarmRepo = alarmRepo ?? new SqliteAlarmRepository();
        _cameraRepo = cameraRepo ?? new SqliteCameraRepository();

        _ = LoadAlarmsAsync();
    }

    [RelayCommand]
    public async Task LoadAlarmsAsync()
    {
        // 1. Tải danh sách camera cho filter
        var cameras = await _cameraRepo.GetAllAsync();
        CameraOptions.Clear();
        CameraOptions.Add("All Cameras");
        foreach (var cam in cameras)
        {
            CameraOptions.Add(cam.Name);
        }

        // 2. Tải danh sách alarm gần đây
        var items = await _alarmRepo.GetRecentAlarmsAsync(100);

        // Tạo dữ liệu mẫu thực tế nếu rỗng
        if (items.Count == 0 && cameras.Count > 0)
        {
            var seedAlarms = new List<AlarmEvent>
            {
                new AlarmEvent
                {
                    CameraId = cameras[0].Id,
                    CameraName = cameras[0].Name,
                    EventType = AlarmEventType.MotionDetected,
                    Level = AlarmLevel.Warning,
                    Timestamp = DateTime.Now.AddMinutes(-5),
                    Description = "Motion detected in restricted gate perimeter"
                },
                new AlarmEvent
                {
                    CameraId = cameras[0].Id,
                    CameraName = cameras[0].Name,
                    EventType = AlarmEventType.LineCrossing,
                    Level = AlarmLevel.Critical,
                    Timestamp = DateTime.Now.AddMinutes(-12),
                    Description = "Tripwire crossed: North Perimeter Fence"
                }
            };

            if (cameras.Count > 1)
            {
                seedAlarms.Add(new AlarmEvent
                {
                    CameraId = cameras[1].Id,
                    CameraName = cameras[1].Name,
                    EventType = AlarmEventType.PlateBlacklist,
                    Level = AlarmLevel.Critical,
                    Timestamp = DateTime.Now.AddMinutes(-25),
                    Description = "Blacklisted vehicle detected: 29A-888.88"
                });
            }

            foreach (var a in seedAlarms)
            {
                await _alarmRepo.AddAlarmAsync(a);
            }

            items = await _alarmRepo.GetRecentAlarmsAsync(100);
        }

        Alarms.Clear();
        foreach (var a in items)
        {
            Alarms.Add(new AlarmItemViewModel(a));
        }

        ApplyFilter();
        UpdateUnreadCount();
    }

    partial void OnFilterCameraChanged(string value)
    {
        ApplyFilter();
    }

    public void ApplyFilter()
    {
        FilteredAlarms.Clear();
        foreach (var item in Alarms)
        {
            if (FilterCamera == "All Cameras" || item.CameraName == FilterCamera)
            {
                FilteredAlarms.Add(item);
            }
        }
    }

    [RelayCommand]
    public void AcknowledgeSelected()
    {
        if (SelectedAlarm != null && !SelectedAlarm.IsHandled)
        {
            SelectedAlarm.MarkHandled("Security Operator");
            UpdateUnreadCount();
        }
    }

    [RelayCommand]
    public void AcknowledgeAll()
    {
        foreach (var a in Alarms)
        {
            if (!a.IsHandled)
            {
                a.MarkHandled("Security Operator");
            }
        }
        UpdateUnreadCount();
    }

    public void UpdateUnreadCount()
    {
        UnreadCount = Alarms.Count(a => !a.IsHandled);
        StatusSummary = $"{UnreadCount} unhandled alarms • Total: {Alarms.Count} logged events";
    }

    public async Task TriggerAlarmEventAsync(AlarmEvent e)
    {
        await _alarmRepo.AddAlarmAsync(e);
        var vm = new AlarmItemViewModel(e);
        Alarms.Insert(0, vm);
        ApplyFilter();
        UpdateUnreadCount();
    }
}
