using CMS.App.ViewModels;
using CMS.Core.Entities;
using CMS.Core.Enums;
using CMS.Infrastructure.Data;
using CMS.MediaServer.Storage;
using Xunit;

namespace CMS.Tests;

public class ViewModelTests : IDisposable
{
    private readonly string _tempDbPath;
    private readonly SqliteCameraRepository _cameraRepo;
    private readonly SqliteVideoSegmentRepository _segmentRepo;
    private readonly SqliteStorageConfigRepository _storageRepo;
    private readonly SqliteAlarmRepository _alarmRepo;

    public ViewModelTests()
    {
        _tempDbPath = Path.Combine(Path.GetTempPath(), $"cms_vm_test_{Guid.NewGuid():N}.db");
        _cameraRepo = new SqliteCameraRepository(_tempDbPath);
        _segmentRepo = new SqliteVideoSegmentRepository(_tempDbPath);
        _storageRepo = new SqliteStorageConfigRepository(_tempDbPath);
        _alarmRepo = new SqliteAlarmRepository(_tempDbPath);
    }

    public void Dispose()
    {
        try
        {
            if (File.Exists(_tempDbPath))
            {
                File.Delete(_tempDbPath);
            }
        }
        catch
        {
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 1. DeviceManagementViewModel Tests
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task DeviceManagementVM_SaveCamera_InsertsCameraIntoRepository()
    {
        var vm = new DeviceManagementViewModel(_cameraRepo);
        vm.Name = "Entrance Cam";
        vm.IpAddress = "192.168.1.200";
        vm.RtspPort = 554;
        vm.HttpPort = 80;
        vm.Username = "admin";
        vm.Password = "pass";
        vm.Channel = 1;
        vm.SelectedBrand = CameraBrand.Hikvision;

        await vm.SaveCameraAsync();

        var cameras = await _cameraRepo.GetAllAsync();
        Assert.Single(cameras);
        Assert.Equal("Entrance Cam", cameras[0].Name);
        Assert.Equal("192.168.1.200", cameras[0].IpAddress);
        Assert.Equal(CameraBrand.Hikvision, cameras[0].Brand);
    }

    [Fact]
    public void DeviceManagementVM_SelectDiscoveredDevice_FillsFormFields()
    {
        var vm = new DeviceManagementViewModel(_cameraRepo);
        var dev = new DiscoveredDevice
        {
            IPAddress = "192.168.1.88",
            Port = 8080,
            Manufacturer = "Dahua",
            Model = "IPC-HDBW4433R-ZS",
            DetectedBrand = CameraBrand.Dahua
        };

        vm.SelectDiscoveredDevice(dev);

        Assert.Equal("192.168.1.88", vm.IpAddress);
        Assert.Equal(8080, vm.HttpPort);
        Assert.Equal(CameraBrand.Dahua, vm.SelectedBrand);
        Assert.Contains("Dahua", vm.Name);
    }

    [Fact]
    public async Task DeviceManagementVM_DeleteCamera_RemovesCamera()
    {
        var cam = new CameraDevice { Id = "cam-del", Name = "Delete Me", IpAddress = "1.2.3.4" };
        await _cameraRepo.AddAsync(cam);

        var vm = new DeviceManagementViewModel(_cameraRepo);
        await vm.LoadCamerasAsync();
        Assert.Single(vm.Cameras);

        vm.SelectedCamera = vm.Cameras[0];
        await vm.DeleteCameraAsync(vm.SelectedCamera);

        var remaining = await _cameraRepo.GetAllAsync();
        Assert.Empty(remaining);
        Assert.Empty(vm.Cameras);
    }

    [Fact]
    public async Task DeviceManagementVM_EditCamera_UpdatesExistingCamera()
    {
        var cam = new CameraDevice
        {
            Id = "cam-edit-01",
            Name = "Camera 01",
            IpAddress = "192.168.21.182",
            Brand = CameraBrand.Tiandy,
            RtspPort = 554,
            HttpPort = 80
        };
        await _cameraRepo.AddAsync(cam);

        var vm = new DeviceManagementViewModel(_cameraRepo);
        await vm.LoadCamerasAsync();
        Assert.Single(vm.Cameras);

        // Select/Edit camera
        vm.EditCamera(vm.Cameras[0]);
        Assert.True(vm.IsEditingExisting);
        Assert.Equal("Camera 01", vm.Name);
        Assert.Equal("192.168.21.182", vm.IpAddress);
        Assert.Equal(CameraBrand.Tiandy, vm.SelectedBrand);

        // Modify fields
        vm.Name = "Camera Cổng Chính";
        vm.IpAddress = "192.168.21.190";
        vm.SelectedBrand = CameraBrand.Hikvision;
        vm.RtspPort = 8554;
        await vm.SaveCameraAsync();

        var updatedList = await _cameraRepo.GetAllAsync();
        Assert.Single(updatedList);
        Assert.Equal("cam-edit-01", updatedList[0].Id);
        Assert.Equal("Camera Cổng Chính", updatedList[0].Name);
        Assert.Equal("192.168.21.190", updatedList[0].IpAddress);
        Assert.Equal(CameraBrand.Hikvision, updatedList[0].Brand);
        Assert.Equal(8554, updatedList[0].RtspPort);
    }

    [Fact]
    public async Task DeviceManagementVM_QuickAdd_AddsDiscoveredAndByIp()
    {
        var vm = new DeviceManagementViewModel(_cameraRepo);
        await vm.LoadCamerasAsync();

        var dev = new DiscoveredDevice
        {
            IPAddress = "192.168.21.10",
            Port = 80,
            Manufacturer = "Dahua",
            Model = "IPC-HDW",
            DetectedBrand = CameraBrand.Dahua
        };
        vm.DiscoveredDevices.Add(dev);

        await vm.QuickAddDiscoveredDeviceAsync(dev);
        Assert.True(dev.IsAlreadyAdded);
        Assert.Single(vm.Cameras);
        Assert.Equal("192.168.21.10", vm.Cameras[0].IpAddress);

        vm.QuickAddIp = "192.168.21.45:554";
        vm.QuickAddBrand = CameraBrand.Hikvision;
        await vm.QuickAddByIpAsync();

        Assert.Equal(2, vm.Cameras.Count);
        Assert.Contains(vm.Cameras, c => c.IpAddress == "192.168.21.45" && c.Brand == CameraBrand.Hikvision);
    }

    [Fact]
    public async Task DeviceManagementVM_BatchAddByIpRange_And_Filter_Works()
    {
        var vm = new DeviceManagementViewModel(_cameraRepo);
        await vm.LoadCamerasAsync();

        vm.BatchStartIp = "192.168.21.101";
        vm.BatchEndIp = "192.168.21.103";
        vm.BatchNamePrefix = "Camera Kho";
        vm.QuickAddBrand = CameraBrand.Hikvision;
        vm.BatchRtspPort = 554;
        vm.QuickAddUsername = "admin";
        vm.QuickAddPassword = "secret";

        await vm.BatchAddByIpRangeAsync();

        Assert.Equal(3, vm.Cameras.Count);
        Assert.Equal(3, vm.FilteredCameras.Count);
        Assert.Contains(vm.Cameras, c => c.IpAddress == "192.168.21.101" && c.Name.Contains("Camera Kho"));
        Assert.Contains(vm.Cameras, c => c.IpAddress == "192.168.21.102");
        Assert.Contains(vm.Cameras, c => c.IpAddress == "192.168.21.103");

        // Test filtering by IP
        vm.CameraSearchText = "102";
        Assert.Single(vm.FilteredCameras);
        Assert.Equal("192.168.21.102", vm.FilteredCameras[0].IpAddress);

        vm.CameraSearchText = "";
        Assert.Equal(3, vm.FilteredCameras.Count);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 2. PlaybackViewModel Tests
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task PlaybackVM_SearchRecordings_FindsSegmentsForSelectedDate()
    {
        var cam = new CameraDevice { Id = "c-play", Name = "Playback Cam", IpAddress = "192.168.1.50" };
        await _cameraRepo.AddAsync(cam);

        var today = DateTime.Today;
        await _segmentRepo.AddSegmentAsync(new VideoSegment
        {
            Id = "s1",
            CameraId = "c-play",
            StartTime = today.AddHours(8),
            EndTime = today.AddHours(8).AddMinutes(15),
            FilePath = "s1.mp4",
            FileSizeBytes = 1000
        });

        var vm = new PlaybackViewModel(_cameraRepo, _segmentRepo);
        await vm.LoadCamerasAsync();

        vm.SelectedCamera = cam;
        vm.SelectedDate = new DateTimeOffset(today);
        await vm.SearchRecordingsAsync();

        Assert.Single(vm.AvailableSegments);
        Assert.Equal("s1", vm.AvailableSegments[0].Id);
    }

    [Fact]
    public void PlaybackVM_FindSegmentAtTime_ReturnsCorrectSegment()
    {
        var vm = new PlaybackViewModel(_cameraRepo, _segmentRepo);
        var baseTime = DateTime.Today.AddHours(10);

        var seg = new VideoSegment
        {
            Id = "target-seg",
            CameraId = "cam1",
            StartTime = baseTime,
            EndTime = baseTime.AddMinutes(30),
            FilePath = "video.mp4"
        };
        vm.AvailableSegments.Add(seg);

        // Within range
        var found = vm.FindSegmentAtTime(baseTime.AddMinutes(10));
        Assert.NotNull(found);
        Assert.Equal("target-seg", found.Id);

        // Outside range
        var notFound = vm.FindSegmentAtTime(baseTime.AddHours(1));
        Assert.Null(notFound);
    }

    [Fact]
    public void PlaybackVM_TogglePlayPause_FlipsPlayingState()
    {
        var vm = new PlaybackViewModel(_cameraRepo, _segmentRepo);
        Assert.False(vm.IsPlaying);

        vm.TogglePlayPause();
        Assert.True(vm.IsPlaying);

        vm.TogglePlayPause();
        Assert.False(vm.IsPlaying);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 3. AlarmCenterViewModel Tests
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task AlarmCenterVM_LoadAlarms_CalculatesUnreadCountAndSorts()
    {
        await _alarmRepo.AddAlarmAsync(new AlarmEvent
        {
            Id = "al-1",
            CameraId = "c1",
            CameraName = "Cam 1",
            EventType = AlarmEventType.MotionDetected,
            Level = AlarmLevel.Warning,
            Timestamp = DateTime.UtcNow.AddMinutes(-10),
            IsHandled = false
        });

        await _alarmRepo.AddAlarmAsync(new AlarmEvent
        {
            Id = "al-2",
            CameraId = "c1",
            CameraName = "Cam 1",
            EventType = AlarmEventType.FaceBlacklist,
            Level = AlarmLevel.Critical,
            Timestamp = DateTime.UtcNow,
            IsHandled = false
        });

        var vm = new AlarmCenterViewModel(_alarmRepo, _cameraRepo);
        await vm.LoadAlarmsAsync();

        Assert.Equal(2, vm.Alarms.Count);
        Assert.Equal(2, vm.UnreadCount);
        Assert.Equal("al-2", vm.Alarms[0].Id); // Most recent
    }

    [Fact]
    public void AlarmCenterVM_MarkAlarmHandled_UpdatesStatusAndDecrementsUnread()
    {
        var alarm = new AlarmEvent
        {
            Id = "al-test",
            CameraId = "c1",
            CameraName = "Cam 1",
            EventType = AlarmEventType.Intrusion,
            Level = AlarmLevel.Critical,
            Timestamp = DateTime.UtcNow,
            IsHandled = false
        };

        var vm = new AlarmCenterViewModel(_alarmRepo, _cameraRepo);
        var itemVM = new AlarmItemViewModel(alarm);
        vm.Alarms.Add(itemVM);
        vm.FilteredAlarms.Add(itemVM);
        vm.SelectedAlarm = itemVM;
        vm.UnreadCount = 1;

        vm.AcknowledgeSelected();

        Assert.True(itemVM.IsHandled);
        Assert.Equal("Nhân viên an ninh", itemVM.HandledBy);
        Assert.Equal(0, vm.UnreadCount);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 4. EMapViewModel Tests
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task EMapVM_PlaceCameraOnMap_AddsAndUpdatesCoordinates()
    {
        var cam = new CameraDevice { Id = "c-map", Name = "Lobby Camera", IpAddress = "192.168.1.10" };
        await _cameraRepo.AddAsync(cam);

        var vm = new EMapViewModel(_cameraRepo);
        await vm.LoadCamerasAsync();

        vm.SelectedCamera = cam;
        vm.PlaceCameraOnMap(30.5, 45.0);

        Assert.Contains(vm.PlacedNodes, n => n.CameraId == "c-map" && n.XPercent == 30.5 && n.YPercent == 45.0);

        // Place same camera at updated location
        vm.PlaceCameraOnMap(50.0, 60.0);
        var node = vm.PlacedNodes.First(n => n.CameraId == "c-map");
        Assert.Equal(50.0, node.XPercent);
        Assert.Equal(60.0, node.YPercent);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 5. MainWindowViewModel Navigation Tests
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void MainWindowVM_Navigate_SwitchesPagesCorrectly()
    {
        var vm = new MainWindowViewModel();

        vm.Navigate(NavigationPage.Playback);
        Assert.True(vm.IsPlaybackActive);
        Assert.False(vm.IsLiveViewActive);

        vm.Navigate(NavigationPage.DeviceManagement);
        Assert.True(vm.IsDeviceManagementActive);
        Assert.False(vm.IsPlaybackActive);

        vm.Navigate(NavigationPage.AlarmCenter);
        Assert.True(vm.IsAlarmCenterActive);

        vm.Navigate(NavigationPage.EMap);
        Assert.True(vm.IsEMapActive);

        vm.Navigate(NavigationPage.StorageSettings);
        Assert.True(vm.IsStorageSettingsActive);
    }
}
