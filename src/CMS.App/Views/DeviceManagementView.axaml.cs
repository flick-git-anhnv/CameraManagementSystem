using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using CMS.App.ViewModels;
using CMS.Core.Entities;

namespace CMS.App.Views;

public partial class DeviceManagementView : UserControl
{
    public DeviceManagementView()
    {
        InitializeComponent();

        // ── Khôi phục kích thước các Splitter ──────────────────────────────
        var settings = CMS.App.Services.AppSettingsService.Instance.Current;
        var mainGrid = SplitterConfigForm.Parent as Grid;
        if (mainGrid != null && mainGrid.ColumnDefinitions.Count > 2 &&
            settings.DeviceManagementConfigWidth >= 320 && settings.DeviceManagementConfigWidth <= 550)
        {
            mainGrid.ColumnDefinitions[2].Width = new GridLength(settings.DeviceManagementConfigWidth, GridUnitType.Pixel);
        }

        var leftGrid = SplitterDiscoveredCameras.Parent as Grid;
        if (leftGrid != null && leftGrid.RowDefinitions.Count > 2 &&
            settings.DeviceManagementDiscoveredHeight >= 140 && settings.DeviceManagementDiscoveredHeight <= 500)
        {
            leftGrid.RowDefinitions[2].Height = new GridLength(settings.DeviceManagementDiscoveredHeight, GridUnitType.Pixel);
        }

        // Lắng nghe khi kéo thả Splitter dọc (Form Cấu hình)
        SplitterConfigForm.DragCompleted += (_, _) => SaveConfigFormWidth();

        // Lắng nghe khi kéo thả Splitter ngang (Danh sách ONVIF)
        SplitterDiscoveredCameras.DragCompleted += (_, _) => SaveDiscoveredCamerasHeight();

        this.Unloaded += (_, _) =>
        {
            SaveConfigFormWidth();
            SaveDiscoveredCamerasHeight();
        };
    }

    private void SaveConfigFormWidth()
    {
        var mainGrid = SplitterConfigForm.Parent as Grid;
        if (mainGrid == null || mainGrid.ColumnDefinitions.Count <= 2) return;
        var col = mainGrid.ColumnDefinitions[2];

        double w = col.ActualWidth > 0
            ? col.ActualWidth
            : (col.Width.IsAbsolute ? col.Width.Value : 0);

        if (w >= 320 && w <= 550)
        {
            CMS.App.Services.AppSettingsService.Instance.Update(s => s.DeviceManagementConfigWidth = Math.Round(w));
        }
    }

    private void SaveDiscoveredCamerasHeight()
    {
        var leftGrid = SplitterDiscoveredCameras.Parent as Grid;
        if (leftGrid == null || leftGrid.RowDefinitions.Count <= 2) return;
        var row = leftGrid.RowDefinitions[2];

        double h = row.ActualHeight > 0
            ? row.ActualHeight
            : (row.Height.IsAbsolute ? row.Height.Value : 0);

        if (h >= 140 && h <= 500)
        {
            CMS.App.Services.AppSettingsService.Instance.Update(s => s.DeviceManagementDiscoveredHeight = Math.Round(h));
        }
    }

    public void OnEditCameraClick(object? sender, RoutedEventArgs e)
    {
        e.Handled = true;
        if (DataContext is DeviceManagementViewModel vm &&
            sender is Control { DataContext: CameraDevice cam })
        {
            vm.EditCamera(cam);
        }
    }

    public async void OnDeleteCameraClick(object? sender, RoutedEventArgs e)
    {
        e.Handled = true;
        if (DataContext is DeviceManagementViewModel vm &&
            sender is Control { DataContext: CameraDevice cam })
        {
            await vm.DeleteCameraAsync(cam);
        }
    }

    public async void OnCheckCameraConnectionClick(object? sender, RoutedEventArgs e)
    {
        e.Handled = true;
        if (DataContext is DeviceManagementViewModel vm &&
            sender is Control { DataContext: CameraDevice cam })
        {
            await vm.CheckSingleCameraConnectionAsync(cam);
        }
    }

    public void OnCameraCardPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is DeviceManagementViewModel vm &&
            sender is Control { DataContext: CameraDevice cam })
        {
            vm.EditCamera(cam);
        }
    }

    public void OnSelectDiscoveredDeviceClick(object? sender, RoutedEventArgs e)
    {
        e.Handled = true;
        if (DataContext is DeviceManagementViewModel vm &&
            sender is Control { DataContext: DiscoveredDevice dev })
        {
            vm.SelectDiscoveredDevice(dev);
        }
    }

    public async void OnQuickAddDiscoveredDeviceClick(object? sender, RoutedEventArgs e)
    {
        e.Handled = true;
        if (DataContext is DeviceManagementViewModel vm &&
            sender is Control { DataContext: DiscoveredDevice dev })
        {
            await vm.QuickAddDiscoveredDeviceAsync(dev);
        }
    }

    public void OnDiscoveredCardPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is DeviceManagementViewModel vm &&
            sender is Control { DataContext: DiscoveredDevice dev })
        {
            vm.SelectDiscoveredDevice(dev);
        }
    }

    public void OnDiscoveredCheckBoxClick(object? sender, RoutedEventArgs e)
    {
        e.Handled = true;
        if (DataContext is DeviceManagementViewModel vm)
        {
            vm.NotifyDiscoveredSelectionChanged();
        }
    }
}
