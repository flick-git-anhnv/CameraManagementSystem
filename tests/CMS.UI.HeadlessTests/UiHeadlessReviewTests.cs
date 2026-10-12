using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using CMS.App.Controls;
using CMS.App.ViewModels;
using CMS.App.Views;
using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using NavigationPage = CMS.App.ViewModels.NavigationPage;

namespace CMS.UI.HeadlessTests;

public class UiHeadlessReviewTests
{
    private static void EnsureMeasuredAndArranged(Control control, double width = 1200, double height = 800)
    {
        control.Measure(new Size(width, height));
        control.Arrange(new Rect(0, 0, width, height));
    }

    [AvaloniaFact]
    public void Review_MainWindow_Layout_And_Theme_Resources()
    {
        var window = new MainWindow();
        var vm = new MainWindowViewModel();
        window.DataContext = vm;

        EnsureMeasuredAndArranged(window, 1420, 860);

        Assert.NotNull(window);
        Assert.True(window.Bounds.Width > 0 || window.DesiredSize.Width > 0);
        Assert.Equal(NavigationPage.LiveView, vm.CurrentPage);

        // Test navigation through each page headlessly
        vm.Navigate(NavigationPage.Playback);
        Assert.True(vm.IsPlaybackActive);

        vm.Navigate(NavigationPage.DeviceManagement);
        Assert.True(vm.IsDeviceManagementActive);

        vm.Navigate(NavigationPage.AlarmCenter);
        Assert.True(vm.IsAlarmCenterActive);

        vm.Navigate(NavigationPage.StorageSettings);
        Assert.True(vm.IsStorageSettingsActive);

        vm.Navigate(NavigationPage.EMap);
        Assert.True(vm.IsEMapActive);

        vm.Navigate(NavigationPage.AiAnalytics);
        Assert.True(vm.IsAiAnalyticsActive);

        vm.Navigate(NavigationPage.LiveView);
        Assert.True(vm.IsLiveViewActive);

        window.Close();
    }

    [AvaloniaFact]
    public void Review_LiveViewGrid_Layout_And_Cells()
    {
        var grid = new LiveViewGrid();
        EnsureMeasuredAndArranged(grid, 1000, 700);

        Assert.Equal(2, grid.CurrentRows);
        Assert.Equal(2, grid.CurrentCols);
        Assert.Equal(4, grid.TotalCells);
        Assert.Equal(4, grid.Cells.Count);

        // Test changing grid presets headlessly
        grid.SetGridLayout(3, 3);
        Assert.Equal(9, grid.TotalCells);
        Assert.Equal(9, grid.Cells.Count);

        grid.SetGridLayout(1, 1);
        Assert.Equal(1, grid.TotalCells);

        grid.SetGridLayout(2, 4);
        Assert.Equal(8, grid.TotalCells);

        grid.StopAll();
        grid.ToggleRecordAll();
    }

    [AvaloniaFact]
    public void Review_PlaybackView_Layout_And_Controls()
    {
        var view = new PlaybackView();
        var vm = new PlaybackViewModel();
        view.DataContext = vm;

        EnsureMeasuredAndArranged(view, 1200, 800);

        Assert.NotNull(view);
        Assert.NotNull(view.FindControl<TimelineControl>("Timeline"));
        Assert.Contains("tra cứu", vm.PlaybackStatus, StringComparison.OrdinalIgnoreCase);
    }

    [AvaloniaFact]
    public void Review_DeviceManagementView_Layout_And_Inputs()
    {
        var view = new DeviceManagementView();
        var vm = new DeviceManagementViewModel();
        view.DataContext = vm;

        EnsureMeasuredAndArranged(view, 1200, 800);

        Assert.NotNull(view);
        Assert.Equal("192.168.1.100", vm.IpAddress);
        Assert.Equal(554, vm.RtspPort);
    }

    [AvaloniaFact]
    public void Review_AlarmCenterView_Layout_And_Cards()
    {
        var view = new AlarmCenterView();
        var vm = new AlarmCenterViewModel();
        view.DataContext = vm;

        EnsureMeasuredAndArranged(view, 1200, 800);

        Assert.NotNull(view);
        Assert.NotNull(vm.Alarms);
        Assert.NotNull(vm.FilteredAlarms);
    }

    [AvaloniaFact]
    public void Review_StorageConfigView_Layout_And_Policies()
    {
        var view = new StorageConfigView();
        var vm = new StorageConfigViewModel();
        view.DataContext = vm;

        EnsureMeasuredAndArranged(view, 1200, 900);

        Assert.NotNull(view);
        Assert.True(vm.SegmentDurationMinutes > 0);
        Assert.True(vm.MinFreeSpaceGigabytes > 0);
    }

    [AvaloniaFact]
    public void Review_EMapView_Layout_And_Blueprint()
    {
        var view = new EMapView();
        var vm = new EMapViewModel();
        view.DataContext = vm;

        EnsureMeasuredAndArranged(view, 1200, 800);

        Assert.NotNull(view);
        Assert.NotNull(view.FindControl<Canvas>("MapCanvas"));
        Assert.NotNull(view.FindControl<Border>("PopupPreview"));
    }

    [AvaloniaFact]
    public void Review_PtzControlView_Layout_And_DPad()
    {
        var ptz = new PtzControlView();
        EnsureMeasuredAndArranged(ptz, 300, 500);

        Assert.NotNull(ptz);
        Assert.NotNull(ptz.FindControl<Button>("BtnPtzUp"));
        Assert.NotNull(ptz.FindControl<Button>("BtnPtzDown"));
        Assert.NotNull(ptz.FindControl<Button>("BtnPtzStop"));
    }

    [AvaloniaFact]
    public void Review_VideoCellView_Layout_And_OSD()
    {
        var cell = new VideoCellView();
        EnsureMeasuredAndArranged(cell, 400, 300);

        Assert.NotNull(cell);
        Assert.False(cell.IsPlaying);
        Assert.False(cell.IsRecording);

        cell.SetSelected(true);
        cell.SetSelected(false);
    }

    [AvaloniaFact]
    public void Review_TimelineControl_Layout_And_Cursor()
    {
        var timeline = new TimelineControl();
        EnsureMeasuredAndArranged(timeline, 800, 70);

        Assert.NotNull(timeline);
        timeline.CurrentDay = DateTime.Today;
        timeline.CurrentTime = TimeSpan.FromHours(14.5);
        timeline.RedrawTimeline();
        Assert.Equal(TimeSpan.FromHours(14.5), timeline.CurrentTime);
    }
}
