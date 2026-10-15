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

        // ── Khôi phục độ rộng Splitter cho Live View ────────────────────────
        var liveGrid = SplitterLiveView.Parent as Grid;
        var savedWidth = CMS.App.Services.AppSettingsService.Instance.Current.LiveViewSidebarWidth;
        if (liveGrid != null && liveGrid.ColumnDefinitions.Count > 0 && savedWidth >= 200 && savedWidth <= 450)
        {
            liveGrid.ColumnDefinitions[0].Width = new GridLength(savedWidth, GridUnitType.Pixel);
        }

        // Lắng nghe khi kéo thả Splitter Live View xong -> tự động lưu cấu hình
        SplitterLiveView.DragCompleted += (_, _) => SaveLiveViewSplitterWidth();

        LivePtzControl.PtzSupportChanged += (_, hasPtz) =>
        {
            if (PtzSidebarBorder != null)
            {
                PtzSidebarBorder.IsVisible = hasPtz;
            }
        };

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
        LiveGrid.SelectedCameraChanged += (_, cam) =>
        {
            LivePtzControl.TargetCamera = cam;
        };

        this.DataContextChanged += (_, _) =>
        {
            if (DataContext is MainWindowViewModel vm)
            {
                vm.ConfiguredCameras.CollectionChanged += (_, _) =>
                {
                    LiveGrid.SyncCameraMetadata(vm.ConfiguredCameras.Select(c => c.Device));
                    SyncCameraStates();
                };
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

        this.Closed += (_, _) =>
        {
            _statusSyncTimer?.Stop();
            try
            {
                SaveLiveViewSplitterWidth();
                LiveGrid.StopAll();
                Controls.VideoCellView.GlobalStorageManager.Dispose();
            }
            catch
            {
            }
        };
    }

    private void SaveLiveViewSplitterWidth()
    {
        var liveGrid = SplitterLiveView.Parent as Grid;
        if (liveGrid == null || liveGrid.ColumnDefinitions.Count == 0) return;
        var col = liveGrid.ColumnDefinitions[0];

        double w = col.ActualWidth > 0
            ? col.ActualWidth
            : (col.Width.IsAbsolute ? col.Width.Value : 0);

        if (w >= 200 && w <= 450)
        {
            CMS.App.Services.AppSettingsService.Instance.Update(s => s.LiveViewSidebarWidth = Math.Round(w));
        }
    }

    public void SyncCameraStates()
    {
        if (DataContext is MainWindowViewModel vm)
        {
            var viewing = LiveGrid.GetViewingCameraIds();
            var recording = LiveGrid.GetRecordingCameraIds();
            vm.UpdateCameraStates(viewing, recording);

            if (LivePtzControl.TargetCamera != null)
            {
                var latest = vm.ConfiguredCameras.FirstOrDefault(c => c.Id == LivePtzControl.TargetCamera.Id);
                if (latest != null)
                {
                    if (LivePtzControl.TargetCamera.HasPtz != latest.Device.HasPtz ||
                        LivePtzControl.TargetCamera.Name != latest.Device.Name)
                    {
                        LivePtzControl.TargetCamera.HasPtz = latest.Device.HasPtz;
                        LivePtzControl.TargetCamera.Name = latest.Device.Name;
                        LivePtzControl.UpdatePtzState();
                    }
                }
                else
                {
                    LivePtzControl.TargetCamera = LiveGrid.SelectedCamera;
                }
            }

            if (PtzSidebarBorder != null)
            {
                PtzSidebarBorder.IsVisible = LivePtzControl.IsPtzAvailable;
            }
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
