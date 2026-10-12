using CMS.Core.Entities;
using CMS.Core.Enums;
using CMS.Core.Interfaces;
using CMS.Infrastructure.Data;
using CMS.MediaServer.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace CMS.App.ViewModels;

public class DriveDisplayInfo
{
    public string Name { get; set; } = "";
    public string Label { get; set; } = "";
    public double TotalGb { get; set; }
    public double FreeGb { get; set; }
    public double UsedGb => TotalGb - FreeGb;
    public double UsedPercent => TotalGb > 0 ? (UsedGb / TotalGb) * 100 : 0;
    public string DisplayText => $"{Name} ({Label}) • Còn trống {FreeGb:F1} GB / {TotalGb:F1} GB (Đã dùng {UsedPercent:F0}%)";
}

public class RecordingModeOption
{
    public RecordingMode Mode { get; init; }
    public string DisplayName { get; init; } = "";
    public override string ToString() => DisplayName;
}

public partial class CameraRecordingPolicyItem : ObservableObject
{
    public CameraDevice Device { get; }

    public string Id => Device.Id;
    public string Name => Device.Name;
    public string IpAddress => Device.IpAddress;

    private RecordingModeOption _selectedRecordModeOption;
    public RecordingModeOption SelectedRecordModeOption
    {
        get => _selectedRecordModeOption;
        set
        {
            if (value != null && SetProperty(ref _selectedRecordModeOption, value))
            {
                Device.RecordMode = value.Mode;
                OnPropertyChanged(nameof(RecordMode));
                OnPropertyChanged(nameof(IsScheduleMode));
            }
        }
    }

    public RecordingMode RecordMode
    {
        get => Device.RecordMode;
        set
        {
            if (Device.RecordMode != value)
            {
                Device.RecordMode = value;
                var opt = StorageConfigViewModel.RecordModeOptionsList.FirstOrDefault(x => x.Mode == value)
                          ?? StorageConfigViewModel.RecordModeOptionsList[0];
                SetProperty(ref _selectedRecordModeOption, opt, nameof(SelectedRecordModeOption));
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsScheduleMode));
            }
        }
    }

    public bool IsScheduleMode => Device.RecordMode == RecordingMode.Schedule;

    public bool RecordSubStream
    {
        get => Device.RecordSubStream;
        set
        {
            if (Device.RecordSubStream != value)
            {
                Device.RecordSubStream = value;
                OnPropertyChanged();
            }
        }
    }

    public string ScheduleStartTime
    {
        get => string.IsNullOrWhiteSpace(Device.ScheduleStartTime) ? "07:00" : Device.ScheduleStartTime;
        set
        {
            if (Device.ScheduleStartTime != value && !string.IsNullOrWhiteSpace(value))
            {
                Device.ScheduleStartTime = value;
                OnPropertyChanged();
            }
        }
    }

    public string ScheduleEndTime
    {
        get => string.IsNullOrWhiteSpace(Device.ScheduleEndTime) ? "19:00" : Device.ScheduleEndTime;
        set
        {
            if (Device.ScheduleEndTime != value && !string.IsNullOrWhiteSpace(value))
            {
                Device.ScheduleEndTime = value;
                OnPropertyChanged();
            }
        }
    }

    [ObservableProperty] private bool _isMon = true;
    [ObservableProperty] private bool _isTue = true;
    [ObservableProperty] private bool _isWed = true;
    [ObservableProperty] private bool _isThu = true;
    [ObservableProperty] private bool _isFri = true;
    [ObservableProperty] private bool _isSat = true;
    [ObservableProperty] private bool _isSun = true;
    [ObservableProperty] private bool _isCurrentlyRecording;

    public string RecordingStatusText => IsCurrentlyRecording ? "● Đang ghi" : "○ Đang dừng";
    public string RecordingStatusColor => IsCurrentlyRecording ? "#10B981" : "#71717A";

    partial void OnIsCurrentlyRecordingChanged(bool value)
    {
        OnPropertyChanged(nameof(RecordingStatusText));
        OnPropertyChanged(nameof(RecordingStatusColor));
    }

    partial void OnIsMonChanged(bool value) => SyncDaysToDevice();
    partial void OnIsTueChanged(bool value) => SyncDaysToDevice();
    partial void OnIsWedChanged(bool value) => SyncDaysToDevice();
    partial void OnIsThuChanged(bool value) => SyncDaysToDevice();
    partial void OnIsFriChanged(bool value) => SyncDaysToDevice();
    partial void OnIsSatChanged(bool value) => SyncDaysToDevice();
    partial void OnIsSunChanged(bool value) => SyncDaysToDevice();

    public CameraRecordingPolicyItem(CameraDevice device, bool isCurrentlyRecording = false)
    {
        Device = device;
        _isCurrentlyRecording = isCurrentlyRecording;
        _selectedRecordModeOption = StorageConfigViewModel.RecordModeOptionsList.FirstOrDefault(x => x.Mode == device.RecordMode)
                                    ?? StorageConfigViewModel.RecordModeOptionsList[1];

        LoadDaysFromDevice();
    }

    private bool _isLoadingDays;

    private void LoadDaysFromDevice()
    {
        _isLoadingDays = true;
        var raw = string.IsNullOrWhiteSpace(Device.ScheduleDays) ? "1,2,3,4,5,6,0" : Device.ScheduleDays;
        var set = raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToHashSet();
        IsMon = set.Contains("1");
        IsTue = set.Contains("2");
        IsWed = set.Contains("3");
        IsThu = set.Contains("4");
        IsFri = set.Contains("5");
        IsSat = set.Contains("6");
        IsSun = set.Contains("0");
        _isLoadingDays = false;
    }

    private void SyncDaysToDevice()
    {
        if (_isLoadingDays) return;
        var days = new List<string>();
        if (IsMon) days.Add("1");
        if (IsTue) days.Add("2");
        if (IsWed) days.Add("3");
        if (IsThu) days.Add("4");
        if (IsFri) days.Add("5");
        if (IsSat) days.Add("6");
        if (IsSun) days.Add("0");
        Device.ScheduleDays = string.Join(",", days);
    }
}

public partial class StorageConfigViewModel : ObservableObject
{
    private readonly SqliteStorageConfigRepository _storageRepo;
    private readonly ICameraRepository _cameraRepo;
    private readonly IVideoSegmentRepository _segmentRepo;
    private readonly StorageManager _storageManager;

    [ObservableProperty] private string _rootStoragePath = "";
    [ObservableProperty] private int _segmentDurationMinutes = 15;
    [ObservableProperty] private int _minFreeSpaceGigabytes = 10;
    [ObservableProperty] private int _retentionDays = 30;
    [ObservableProperty] private bool _autoOverwriteOldest = true;
    [ObservableProperty] private string _statusMessage = "";

    // Drive & Usage stats
    [ObservableProperty] private ObservableCollection<DriveDisplayInfo> _availableDrives = new();
    [ObservableProperty] private DriveDisplayInfo? _selectedDrive;
    [ObservableProperty] private long _totalRecordedBytes;
    [ObservableProperty] private string _recordedSizeFormatted = "0 MB";

    // Cameras list with recording policy
    [ObservableProperty] private ObservableCollection<CameraDevice> _cameras = new();
    [ObservableProperty] private ObservableCollection<CameraRecordingPolicyItem> _cameraPolicies = new();

    public static IReadOnlyList<RecordingModeOption> RecordModeOptionsList { get; } = new List<RecordingModeOption>
    {
        new() { Mode = RecordingMode.None, DisplayName = "Không ghi hình" },
        new() { Mode = RecordingMode.Continuous247, DisplayName = "Ghi liên tục 24/7" },
        new() { Mode = RecordingMode.Schedule, DisplayName = "Ghi theo lịch trình" },
        new() { Mode = RecordingMode.MotionTriggered, DisplayName = "Ghi khi có chuyển động" },
        new() { Mode = RecordingMode.AiTriggered, DisplayName = "Ghi khi phát hiện sự kiện AI" }
    };

    public static IReadOnlyList<string> TimeSlotsList { get; } = BuildTimeSlots();

    private static List<string> BuildTimeSlots()
    {
        var list = new List<string>();
        for (int h = 0; h < 24; h++)
        {
            list.Add($"{h:D2}:00");
            list.Add($"{h:D2}:30");
        }
        list.Add("23:59");
        return list;
    }

    public List<int> AvailableSegmentDurations { get; } = new() { 5, 10, 15, 30, 60 };
    public List<int> AvailableMinFreeSpaces { get; } = new() { 5, 10, 20, 50, 100 };
    public List<int> AvailableRetentionDays { get; } = new() { 7, 15, 30, 60, 90, 180, 365 };
    public IEnumerable<RecordingMode> AvailableRecordModes => Enum.GetValues<RecordingMode>();
    public IReadOnlyList<RecordingModeOption> AvailableRecordModeOptions => RecordModeOptionsList;
    public IReadOnlyList<string> AvailableTimeSlots => TimeSlotsList;

    public StorageConfigViewModel(
        SqliteStorageConfigRepository? storageRepo = null,
        ICameraRepository? cameraRepo = null,
        IVideoSegmentRepository? segmentRepo = null,
        StorageManager? storageManager = null)
    {
        _storageRepo = storageRepo ?? new SqliteStorageConfigRepository();
        _cameraRepo = cameraRepo ?? new SqliteCameraRepository();
        _segmentRepo = segmentRepo ?? new SqliteVideoSegmentRepository();
        _storageManager = storageManager ?? Controls.VideoCellView.GlobalStorageManager;

        _ = LoadSettingsAsync();
    }

    [RelayCommand]
    public async Task LoadSettingsAsync()
    {
        RefreshDrives();

        var cfg = await _storageRepo.GetConfigAsync();
        RootStoragePath = cfg.RootStoragePath;
        SegmentDurationMinutes = cfg.SegmentDurationMinutes;
        MinFreeSpaceGigabytes = cfg.MinFreeSpaceGigabytes;
        RetentionDays = cfg.RetentionDays;
        AutoOverwriteOldest = cfg.AutoOverwriteOldest;

        // Thống kê dung lượng video đã ghi
        TotalRecordedBytes = await _segmentRepo.GetTotalStorageUsedBytesAsync();
        RecordedSizeFormatted = FormatBytes(TotalRecordedBytes);

        // Load camera recording policy
        var list = await _cameraRepo.GetAllAsync();
        Cameras.Clear();
        CameraPolicies.Clear();
        foreach (var cam in list)
        {
            Cameras.Add(cam);
            bool isRec = _storageManager.IsRecording(cam.Id);
            CameraPolicies.Add(new CameraRecordingPolicyItem(cam, isRec));
        }
    }

    public void RefreshDrives()
    {
        AvailableDrives.Clear();
        try
        {
            foreach (var d in DriveInfo.GetDrives())
            {
                if (d.IsReady)
                {
                    double total = d.TotalSize / (1024.0 * 1024 * 1024);
                    double free = d.AvailableFreeSpace / (1024.0 * 1024 * 1024);
                    var item = new DriveDisplayInfo
                    {
                        Name = d.Name,
                        Label = string.IsNullOrEmpty(d.VolumeLabel) ? "Ổ đĩa cục bộ" : d.VolumeLabel,
                        TotalGb = total,
                        FreeGb = free
                    };
                    AvailableDrives.Add(item);

                    if (RootStoragePath.StartsWith(d.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        SelectedDrive = item;
                    }
                }
            }
        }
        catch
        {
        }
    }

    [RelayCommand]
    public void SelectDriveAsStorage(DriveDisplayInfo? drive)
    {
        if (drive == null) return;
        RootStoragePath = Path.Combine(drive.Name, "CMS_Recordings");
        StatusMessage = $"Thư mục lưu trữ đã chọn là {RootStoragePath}. Nhấn 'Lưu cài đặt' để áp dụng.";
    }

    [RelayCommand]
    public async Task SaveSettingsAsync()
    {
        if (string.IsNullOrWhiteSpace(RootStoragePath))
        {
            StatusMessage = "Đường dẫn lưu trữ không được để trống.";
            return;
        }

        try
        {
            Directory.CreateDirectory(RootStoragePath);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Không thể tạo thư mục '{RootStoragePath}': {ex.Message}";
            return;
        }

        var cfg = new StorageConfig
        {
            RootStoragePath = RootStoragePath.Trim(),
            SegmentDurationMinutes = SegmentDurationMinutes,
            MinFreeSpaceGigabytes = MinFreeSpaceGigabytes,
            RetentionDays = RetentionDays,
            AutoOverwriteOldest = AutoOverwriteOldest
        };

        bool ok = await _storageRepo.SaveConfigAsync(cfg);
        if (ok)
        {
            _storageManager.UpdateConfiguration(cfg);

            // Lưu cài đặt ghi hình của từng camera
            foreach (var cam in Cameras)
            {
                cam.SegmentMinutes = SegmentDurationMinutes;
                await _cameraRepo.UpdateAsync(cam);
            }

            // Tự động kích hoạt / dừng dịch vụ ghi hình nền ngay lập tức theo chế độ đã chọn
            var (activeCount, stoppedCount) = await _storageManager.ApplyCameraPoliciesAsync(Cameras);

            foreach (var policy in CameraPolicies)
            {
                policy.IsCurrentlyRecording = _storageManager.IsRecording(policy.Id);
            }

            StatusMessage = $"✅ Đã lưu cấu hình lưu trữ & áp dụng chế độ ghi hình! (Đang ghi nền: {activeCount} camera • Đang dừng: {stoppedCount} camera)";
            RefreshDrives();
        }
        else
        {
            StatusMessage = "❌ Không thể lưu cấu hình lưu trữ.";
        }
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes >= 1024L * 1024 * 1024 * 1024)
            return $"{bytes / (1024.0 * 1024 * 1024 * 1024):F2} TB";
        if (bytes >= 1024L * 1024 * 1024)
            return $"{bytes / (1024.0 * 1024 * 1024):F2} GB";
        if (bytes >= 1024L * 1024)
            return $"{bytes / (1024.0 * 1024):F1} MB";
        return $"{bytes / 1024.0:F0} KB";
    }
}
