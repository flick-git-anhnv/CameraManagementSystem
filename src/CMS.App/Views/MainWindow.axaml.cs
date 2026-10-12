using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using CMS.App.ViewModels;
using CMS.Core.Entities;
using System;
using System.Linq;

namespace CMS.App.Views;

public partial class MainWindow : Window
{
    private DispatcherTimer? _statusSyncTimer;

    public MainWindow()
    {
        InitializeComponent();

        CameraListBox.DoubleTapped += OnCameraDoubleTapped;
        CameraListBox.SelectionChanged += (_, _) =>
        {
            if (CameraListBox.SelectedItem is CameraItemViewModel item)
            {
                LivePtzControl.TargetCamera = item.Device;
            }
        };
        LiveGrid.BtnAutoFill.Click += (_, _) => OnAutoFillClick();

        LiveGrid.CameraStateChanged += (_, _) => SyncCameraStates();

        this.DataContextChanged += (_, _) =>
        {
            if (DataContext is MainWindowViewModel vm)
            {
                vm.ConfiguredCameras.CollectionChanged += (_, _) => SyncCameraStates();
                SyncCameraStates();
            }
        };

        // Đồng bộ trạng thái định kỳ 1 giây/lần
        _statusSyncTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _statusSyncTimer.Tick += (_, _) => SyncCameraStates();
        _statusSyncTimer.Start();
    }

    public void SyncCameraStates()
    {
        if (DataContext is MainWindowViewModel vm)
        {
            var viewing = LiveGrid.GetViewingCameraIds();
            var recording = LiveGrid.GetRecordingCameraIds();
            vm.UpdateCameraStates(viewing, recording);
        }
    }

    private void OnCameraDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (CameraListBox.SelectedItem is CameraItemViewModel item)
        {
            LiveGrid.PlayCameraOnSelectedCell(item.Device);
        }
        else if (CameraListBox.SelectedItem is CameraDevice cam)
        {
            LiveGrid.PlayCameraOnSelectedCell(cam);
        }
    }

    private void OnAutoFillClick()
    {
        if (DataContext is MainWindowViewModel vm)
        {
            LiveGrid.AutoPlayCameras(vm.ConfiguredCameras.Select(c => c.Device));
        }
    }
}
