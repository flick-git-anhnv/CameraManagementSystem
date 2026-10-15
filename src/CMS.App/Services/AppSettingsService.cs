using System;
using System.IO;
using System.Text.Json;

namespace CMS.App.Services;

/// <summary>
/// Model cấu hình toàn bộ ứng dụng: Giao diện (Theme), kích thước các thanh chia khung (Splitters).
/// Tự động lưu và tải từ tệp app_settings.json.
/// </summary>
public class AppSettings
{
    public AppTheme Theme { get; set; } = AppTheme.Dark;

    // ── Kích thước các thanh chia khung Splitters (đơn vị pixel) ─────────────
    /// <summary>Độ rộng sidebar danh sách camera ở màn hình Live View (Mặc định: 270px)</summary>
    public double LiveViewSidebarWidth { get; set; } = 270;

    /// <summary>Độ rộng sidebar tìm kiếm & đoạn ghi ở màn hình Playback (Mặc định: 320px)</summary>
    public double PlaybackSidebarWidth { get; set; } = 320;

    /// <summary>Độ rộng form cấu hình camera ở màn hình Quản lý thiết bị (Mặc định: 380px)</summary>
    public double DeviceManagementConfigWidth { get; set; } = 380;

    /// <summary>Độ cao danh sách camera ONVIF tìm kiếm ở màn hình Quản lý thiết bị (Mặc định: 220px)</summary>
    public double DeviceManagementDiscoveredHeight { get; set; } = 220;

    /// <summary>Độ rộng sidebar danh sách camera ở màn hình Bản đồ E-Map (Mặc định: 300px)</summary>
    public double EMapSidebarWidth { get; set; } = 300;

    /// <summary>Độ rộng panel chi tiết sự kiện ở Trung tâm cảnh báo Alarm Center (Mặc định: 340px)</summary>
    public double AlarmCenterDetailWidth { get; set; } = 340;
}

public class AppSettingsService
{
    private static readonly Lazy<AppSettingsService> _instance = new(() => new AppSettingsService());
    public static AppSettingsService Instance => _instance.Value;

    private readonly string _settingsFilePath;
    private AppSettings _settings;
    private readonly object _lock = new();

    public AppSettings Current => _settings;

    private AppSettingsService()
    {
        _settingsFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app_settings.json");
        _settings = Load();
    }

    public AppSettings Load()
    {
        lock (_lock)
        {
            try
            {
                if (File.Exists(_settingsFilePath))
                {
                    string json = File.ReadAllText(_settingsFilePath);
                    var settings = JsonSerializer.Deserialize<AppSettings>(json);
                    if (settings != null)
                    {
                        _settings = settings;
                        return _settings;
                    }
                }
            }
            catch
            {
                // Fallback mặc định khi lỗi đọc tệp
            }

            _settings = new AppSettings();
            return _settings;
        }
    }

    public void Save()
    {
        lock (_lock)
        {
            try
            {
                string json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_settingsFilePath, json);
            }
            catch
            {
                // Bỏ qua lỗi ghi trong môi trường quyền hạn hạn chế
            }
        }
    }

    public void Update(Action<AppSettings> updateAction)
    {
        lock (_lock)
        {
            updateAction(_settings);
            Save();
        }
    }
}
