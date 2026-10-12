using Avalonia;
using Avalonia.Styling;
using CMS.Core.Entities;
using CMS.Core.Interfaces;
using CMS.Infrastructure.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace CMS.App.ViewModels;

public enum NavigationPage
{
    LiveView,
    Playback,
    AiAnalytics,
    EMap,
    AlarmCenter,
    DeviceManagement,
    StorageSettings
}

public partial class MainWindowViewModel : ObservableObject
{
    private readonly ICameraRepository _cameraRepo;

    [ObservableProperty]
    private string _title = "KZTEK Camera Management System (CMS / VMS)";

    [ObservableProperty]
    private string _systemStatus = "Ready - Multi-brand VMS & Edge AI";

    [ObservableProperty]
    private NavigationPage _currentPage = NavigationPage.LiveView;

    [ObservableProperty]
    private ObservableCollection<CameraItemViewModel> _configuredCameras = new();

    [ObservableProperty]
    private ObservableCollection<CameraItemViewModel> _filteredCameras = new();

    [ObservableProperty]
    private CameraItemViewModel? _selectedLiveCamera;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private string _themeIcon = "🌙";

    [ObservableProperty]
    private string _themeName = "Dark";

    public bool IsLiveViewActive => CurrentPage == NavigationPage.LiveView;
    public bool IsPlaybackActive => CurrentPage == NavigationPage.Playback;
    public bool IsAiAnalyticsActive => CurrentPage == NavigationPage.AiAnalytics;
    public bool IsEMapActive => CurrentPage == NavigationPage.EMap;
    public bool IsAlarmCenterActive => CurrentPage == NavigationPage.AlarmCenter;
    public bool IsDeviceManagementActive => CurrentPage == NavigationPage.DeviceManagement;
    public bool IsStorageSettingsActive => CurrentPage == NavigationPage.StorageSettings;

    public DeviceManagementViewModel DeviceManagementVM { get; }
    public PlaybackViewModel PlaybackVM { get; }
    public StorageConfigViewModel StorageConfigVM { get; }
    public EMapViewModel EMapVM { get; }
    public AlarmCenterViewModel AlarmCenterVM { get; }

    public MainWindowViewModel()
    {
        _cameraRepo = new SqliteCameraRepository();
        DeviceManagementVM = new DeviceManagementViewModel(_cameraRepo);
        PlaybackVM = new PlaybackViewModel(_cameraRepo);
        StorageConfigVM = new StorageConfigViewModel(null, _cameraRepo);
        EMapVM = new EMapViewModel(_cameraRepo);
        AlarmCenterVM = new AlarmCenterViewModel(null, _cameraRepo);

        _ = RefreshCameraListAsync();
    }

    [RelayCommand]
    public void Navigate(NavigationPage page)
    {
        CurrentPage = page;
        OnPropertyChanged(nameof(IsLiveViewActive));
        OnPropertyChanged(nameof(IsPlaybackActive));
        OnPropertyChanged(nameof(IsAiAnalyticsActive));
        OnPropertyChanged(nameof(IsEMapActive));
        OnPropertyChanged(nameof(IsAlarmCenterActive));
        OnPropertyChanged(nameof(IsDeviceManagementActive));
        OnPropertyChanged(nameof(IsStorageSettingsActive));

        if (page == NavigationPage.LiveView)
        {
            _ = RefreshCameraListAsync();
        }
        else if (page == NavigationPage.Playback)
        {
            _ = PlaybackVM.LoadCamerasAsync();
        }
        else if (page == NavigationPage.StorageSettings)
        {
            _ = StorageConfigVM.LoadSettingsAsync();
        }
        else if (page == NavigationPage.EMap)
        {
            _ = EMapVM.LoadCamerasAsync();
        }
        else if (page == NavigationPage.AlarmCenter)
        {
            _ = AlarmCenterVM.LoadAlarmsAsync();
        }
    }

    [RelayCommand]
    public void ToggleTheme()
    {
        if (Application.Current != null)
        {
            if (Application.Current.RequestedThemeVariant == ThemeVariant.Light)
            {
                Application.Current.RequestedThemeVariant = ThemeVariant.Dark;
                ThemeIcon = "🌙";
                ThemeName = "Dark";
            }
            else
            {
                Application.Current.RequestedThemeVariant = ThemeVariant.Light;
                ThemeIcon = "☀️";
                ThemeName = "Light";
            }
        }
    }

    public async Task RefreshCameraListAsync()
    {
        var list = await _cameraRepo.GetAllAsync();
        ConfiguredCameras.Clear();
        foreach (var cam in list)
        {
            ConfiguredCameras.Add(new CameraItemViewModel(cam));
        }

        if (SelectedLiveCamera == null && ConfiguredCameras.Count > 0)
        {
            SelectedLiveCamera = ConfiguredCameras[0];
        }

        ApplyCameraFilter();
        SystemStatus = $"Connected • {ConfiguredCameras.Count} camera(s) registered in system";
    }

    partial void OnSearchTextChanged(string value)
    {
        ApplyCameraFilter();
    }

    public void ApplyCameraFilter()
    {
        FilteredCameras.Clear();
        var q = SearchText?.Trim() ?? string.Empty;
        foreach (var item in ConfiguredCameras)
        {
            if (string.IsNullOrEmpty(q) ||
                item.Name.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                item.IpAddress.Contains(q, StringComparison.OrdinalIgnoreCase))
            {
                FilteredCameras.Add(item);
            }
        }
    }

    public void UpdateCameraStates(ISet<string> viewingCameraIds, ISet<string> recordingCameraIds)
    {
        foreach (var item in ConfiguredCameras)
        {
            item.IsViewing = viewingCameraIds.Contains(item.Id);
            item.IsRecording = recordingCameraIds.Contains(item.Id);
        }
    }
}
