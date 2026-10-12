using CMS.Core.Entities;
using CMS.Core.Enums;
using CMS.Core.Interfaces;
using CMS.Drivers;
using CMS.Drivers.Onvif;
using CMS.Infrastructure.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace CMS.App.ViewModels;

public partial class DeviceManagementViewModel : ObservableObject
{
    private readonly ICameraRepository _cameraRepo;
    private readonly IDeviceDiscoveryService _discoveryService;

    [ObservableProperty]
    private ObservableCollection<CameraDevice> _cameras = new();

    [ObservableProperty]
    private ObservableCollection<DiscoveredDevice> _discoveredDevices = new();

    [ObservableProperty]
    private CameraDevice? _selectedCamera;

    [ObservableProperty]
    private DiscoveredDevice? _selectedDiscoveredDevice;

    // Form fields for adding/editing camera
    [ObservableProperty] private string _name = "Camera 01";
    [ObservableProperty] private string _ipAddress = "192.168.1.100";
    [ObservableProperty] private int _rtspPort = 554;
    [ObservableProperty] private int _httpPort = 80;
    [ObservableProperty] private string _username = "admin";
    [ObservableProperty] private string _password = "";
    [ObservableProperty] private int _channel = 1;
    [ObservableProperty] private CameraBrand _selectedBrand = CameraBrand.Hikvision;
    [ObservableProperty] private string _statusMessage = "";
    [ObservableProperty] private bool _isScanning = false;
    [ObservableProperty] private bool _isTesting = false;

    public IEnumerable<CameraBrand> AvailableBrands => Enum.GetValues<CameraBrand>();

    public DeviceManagementViewModel(ICameraRepository? repo = null, IDeviceDiscoveryService? discovery = null)
    {
        _cameraRepo = repo ?? new SqliteCameraRepository();
        _discoveryService = discovery ?? new OnvifDiscoveryService();

        _ = LoadCamerasAsync();
    }

    [RelayCommand]
    public async Task LoadCamerasAsync()
    {
        var list = await _cameraRepo.GetAllAsync();
        Cameras.Clear();
        foreach (var cam in list)
        {
            Cameras.Add(cam);
        }
    }

    [RelayCommand]
    public async Task ScanOnvifDevicesAsync()
    {
        if (IsScanning) return;

        IsScanning = true;
        StatusMessage = "🔍 Đang dò quét mạng LAN tìm thiết bị ONVIF (WS-Discovery)...";
        DiscoveredDevices.Clear();

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var found = await _discoveryService.DiscoverOnvifDevicesAsync(TimeSpan.FromSeconds(4), cts.Token);

            foreach (var dev in found)
            {
                DiscoveredDevices.Add(dev);
            }

            StatusMessage = found.Count > 0
                ? $"✅ Tìm thấy {found.Count} thiết bị ONVIF trên mạng LAN."
                : "⚠️ Không tìm thấy thiết bị ONVIF tự động. Bạn có thể thêm thủ công bằng địa chỉ IP.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Lỗi dò quét: {ex.Message}";
        }
        finally
        {
            IsScanning = false;
        }
    }

    [RelayCommand]
    public void SelectDiscoveredDevice(DiscoveredDevice? dev)
    {
        if (dev == null) return;

        IpAddress = dev.IPAddress;
        HttpPort = dev.Port;
        Name = $"{dev.Manufacturer} {dev.Model}".Trim();
        SelectedBrand = dev.DetectedBrand;
        StatusMessage = $"Đã chọn {dev.IPAddress}. Hãy nhập tài khoản/mật khẩu rồi nhấn 'Lưu camera'.";
    }

    [RelayCommand]
    public async Task TestConnectionAsync()
    {
        if (IsTesting) return;
        IsTesting = true;
        StatusMessage = $"Đang kiểm tra kết nối tới {IpAddress}:{RtspPort}...";

        try
        {
            var tempCam = new CameraDevice
            {
                IpAddress = IpAddress,
                RtspPort = RtspPort,
                HttpPort = HttpPort,
                Username = Username,
                Password = Password,
                Channel = Channel,
                Brand = SelectedBrand
            };

            var driver = CameraDriverFactory.GetDriver(SelectedBrand);
            bool success = await driver.TestConnectionAsync(tempCam);

            StatusMessage = success
                ? $"✅ Kết nối THÀNH CÔNG tới camera tại {IpAddress}!"
                : $"❌ Không thể kết nối tới {IpAddress}:{RtspPort}. Vui lòng kiểm tra IP, cổng và tài khoản.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Lỗi kiểm tra: {ex.Message}";
        }
        finally
        {
            IsTesting = false;
        }
    }

    [RelayCommand]
    public async Task SaveCameraAsync()
    {
        if (string.IsNullOrWhiteSpace(IpAddress))
        {
            StatusMessage = "Địa chỉ IP không được để trống.";
            return;
        }

        var cam = SelectedCamera ?? new CameraDevice();
        cam.Name = string.IsNullOrWhiteSpace(Name) ? $"Camera {IpAddress}" : Name;
        cam.IpAddress = IpAddress.Trim();
        cam.RtspPort = RtspPort;
        cam.HttpPort = HttpPort;
        cam.Username = Username.Trim();
        cam.Password = Password;
        cam.Channel = Channel;
        cam.Brand = SelectedBrand;
        cam.Status = DeviceStatus.Online;

        bool ok = await _cameraRepo.AddAsync(cam);
        if (ok)
        {
            StatusMessage = $"✅ Đã lưu cấu hình camera '{cam.Name}' thành công!";
            await LoadCamerasAsync();
            ResetForm();
        }
        else
        {
            StatusMessage = "❌ Lưu camera vào cơ sở dữ liệu thất bại.";
        }
    }

    [RelayCommand]
    public async Task DeleteCameraAsync(CameraDevice? cam)
    {
        if (cam == null) return;
        bool ok = await _cameraRepo.DeleteAsync(cam.Id);
        if (ok)
        {
            StatusMessage = $"Đã xóa camera '{cam.Name}'.";
            await LoadCamerasAsync();
            if (SelectedCamera?.Id == cam.Id)
            {
                ResetForm();
            }
        }
    }

    [RelayCommand]
    public void EditCamera(CameraDevice? cam)
    {
        if (cam == null) return;
        SelectedCamera = cam;
        Name = cam.Name;
        IpAddress = cam.IpAddress;
        RtspPort = cam.RtspPort;
        HttpPort = cam.HttpPort;
        Username = cam.Username;
        Password = cam.Password;
        Channel = cam.Channel;
        SelectedBrand = cam.Brand;
        StatusMessage = $"Đang chỉnh sửa camera '{cam.Name}'.";
    }

    [RelayCommand]
    public void ResetForm()
    {
        SelectedCamera = null;
        Name = "Camera 01";
        IpAddress = "192.168.1.100";
        RtspPort = 554;
        HttpPort = 80;
        Username = "admin";
        Password = "";
        Channel = 1;
        SelectedBrand = CameraBrand.Hikvision;
    }
}
