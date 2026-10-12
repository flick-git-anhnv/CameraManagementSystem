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

    public List<int> AvailableSegmentDurations { get; } = new() { 5, 10, 15, 30, 60 };
    public List<int> AvailableMinFreeSpaces { get; } = new() { 5, 10, 20, 50, 100 };
    public List<int> AvailableRetentionDays { get; } = new() { 7, 15, 30, 60, 90, 180, 365 };
    public IEnumerable<RecordingMode> AvailableRecordModes => Enum.GetValues<RecordingMode>();

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
        foreach (var cam in list)
        {
            Cameras.Add(cam);
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
                await _cameraRepo.UpdateAsync(cam);
            }

            StatusMessage = "✅ Đã lưu cấu hình lưu trữ và áp dụng vào Động cơ ghi hình!";
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
