using Avalonia;
using Avalonia.Styling;
using System.IO;
using System.Text.Json;

namespace CMS.App.Services;

public enum AppTheme
{
    Dark,
    Light
}

public class ThemeSettings
{
    public AppTheme Theme { get; set; } = AppTheme.Dark;
}

public class ThemeService
{
    private static readonly Lazy<ThemeService> _instance = new(() => new ThemeService());
    public static ThemeService Instance => _instance.Value;

    private readonly string _settingsFilePath;
    private AppTheme _currentTheme = AppTheme.Dark;

    public event EventHandler<AppTheme>? ThemeChanged;

    public AppTheme CurrentTheme => _currentTheme;
    public bool IsDarkTheme => _currentTheme == AppTheme.Dark;

    private ThemeService()
    {
        _settingsFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app_settings.json");
    }

    public void Initialize()
    {
        try
        {
            if (File.Exists(_settingsFilePath))
            {
                string json = File.ReadAllText(_settingsFilePath);
                var settings = JsonSerializer.Deserialize<ThemeSettings>(json);
                if (settings != null)
                {
                    _currentTheme = settings.Theme;
                }
            }
        }
        catch
        {
            _currentTheme = AppTheme.Dark;
        }

        ApplyTheme(_currentTheme);
    }

    public void SetTheme(AppTheme theme)
    {
        _currentTheme = theme;
        ApplyTheme(theme);
        SaveSettings();
        ThemeChanged?.Invoke(this, theme);
    }

    public void ToggleTheme()
    {
        SetTheme(_currentTheme == AppTheme.Dark ? AppTheme.Light : AppTheme.Dark);
    }

    private void ApplyTheme(AppTheme theme)
    {
        if (Application.Current != null)
        {
            Application.Current.RequestedThemeVariant = theme == AppTheme.Dark
                ? ThemeVariant.Dark
                : ThemeVariant.Light;
        }
    }

    private void SaveSettings()
    {
        try
        {
            var settings = new ThemeSettings { Theme = _currentTheme };
            string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_settingsFilePath, json);
        }
        catch
        {
            // Ignore settings save errors on write-protected environments
        }
    }
}
