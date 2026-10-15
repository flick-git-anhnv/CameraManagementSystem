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

    private AppTheme _currentTheme = AppTheme.Dark;

    public event EventHandler<AppTheme>? ThemeChanged;

    public AppTheme CurrentTheme => _currentTheme;
    public bool IsDarkTheme => _currentTheme == AppTheme.Dark;

    private ThemeService()
    {
    }

    public void Initialize()
    {
        _currentTheme = AppSettingsService.Instance.Current.Theme;
        ApplyTheme(_currentTheme);
    }

    public void SetTheme(AppTheme theme)
    {
        _currentTheme = theme;
        ApplyTheme(theme);
        AppSettingsService.Instance.Update(s => s.Theme = theme);
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
}
