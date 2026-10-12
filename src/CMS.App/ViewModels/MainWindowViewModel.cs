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
    DeviceManagement
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
    private ObservableCollection<CameraDevice> _configuredCameras = new();

    public DeviceManagementViewModel DeviceManagementVM { get; }
    public PlaybackViewModel PlaybackVM { get; }

    public MainWindowViewModel()
    {
        _cameraRepo = new SqliteCameraRepository();
        DeviceManagementVM = new DeviceManagementViewModel(_cameraRepo);
        PlaybackVM = new PlaybackViewModel(_cameraRepo);

        _ = RefreshCameraListAsync();
    }

    [RelayCommand]
    public void Navigate(NavigationPage page)
    {
        CurrentPage = page;
        if (page == NavigationPage.LiveView)
        {
            _ = RefreshCameraListAsync();
        }
        else if (page == NavigationPage.Playback)
        {
            _ = PlaybackVM.LoadCamerasAsync();
        }
    }

    public async Task RefreshCameraListAsync()
    {
        var list = await _cameraRepo.GetAllAsync();
        ConfiguredCameras.Clear();
        foreach (var cam in list)
        {
            ConfiguredCameras.Add(cam);
        }
        SystemStatus = $"Connected • {ConfiguredCameras.Count} camera(s) registered in system";
    }
}
