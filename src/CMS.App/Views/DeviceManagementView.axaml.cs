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
