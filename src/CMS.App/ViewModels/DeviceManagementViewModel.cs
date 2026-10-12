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
    private ObservableCollection<CameraDevice> _filteredCameras = new();

    [ObservableProperty]
    private ObservableCollection<DiscoveredDevice> _discoveredDevices = new();

    [ObservableProperty]
    private ObservableCollection<DiscoveredDevice> _filteredDiscoveredDevices = new();

    [ObservableProperty]
    private CameraDevice? _selectedCamera;

    [ObservableProperty]
    private DiscoveredDevice? _selectedDiscoveredDevice;

    // Filter fields for Configured & Discovered lists
    [ObservableProperty] private string _cameraSearchText = "";
    [ObservableProperty] private string _cameraStatusFilter = "Tất cả trạng thái";
    [ObservableProperty] private string _discoveredSearchText = "";

    public IReadOnlyList<string> StatusFilterOptions { get; } = new[]
    {
        "Tất cả trạng thái",
        "Đang kết nối (Online)",
        "Mất kết nối (Offline)"
    };

    // Form fields for adding/editing single camera
    [ObservableProperty] private string _name = "Camera 01";
    [ObservableProperty] private string _ipAddress = "192.168.1.100";
    [ObservableProperty] private int _rtspPort = 554;
    [ObservableProperty] private int _httpPort = 80;
    [ObservableProperty] private string _username = "admin";
    [ObservableProperty] private string _password = "";
    [ObservableProperty] private int _channel = 1;
    [ObservableProperty] private CameraBrand _selectedBrand = CameraBrand.Hikvision;
    [ObservableProperty] private bool _hasPtz = false;
    [ObservableProperty] private string _statusMessage = "";
    [ObservableProperty] private bool _isScanning = false;
    [ObservableProperty] private bool _isTesting = false;
    [ObservableProperty] private bool _isCheckingAll = false;

    // Batch / Bulk Quick-Add fields (Thêm nhanh hàng loạt)
    [ObservableProperty] private string _batchStartIp = "192.168.21.10";
    [ObservableProperty] private string _batchEndIp = "192.168.21.20";
    [ObservableProperty] private string _batchNamePrefix = "Camera";
    [ObservableProperty] private int _batchRtspPort = 554;
    [ObservableProperty] private int _batchHttpPort = 80;
    [ObservableProperty] private int _batchChannel = 1;
    [ObservableProperty] private bool _batchHasPtz = false;
    [ObservableProperty] private string _quickAddIp = "";
    [ObservableProperty] private string _quickAddUsername = "admin";
    [ObservableProperty] private string _quickAddPassword = "";
    [ObservableProperty] private CameraBrand _quickAddBrand = CameraBrand.ONVIF;

    public IEnumerable<CameraBrand> AvailableBrands => Enum.GetValues<CameraBrand>();

    // Summary counters for UI
    public int TotalCamerasCount => Cameras.Count;
    public int OnlineCamerasCount => Cameras.Count(c => c.IsOnline);
    public int OfflineCamerasCount => Cameras.Count(c => c.IsOffline);
    public bool HasConfiguredCameras => Cameras.Count > 0;
    public int DiscoveredCount => DiscoveredDevices.Count;
    public int SelectedDiscoveredCount => DiscoveredDevices.Count(d => d.IsSelected && !d.IsAlreadyAdded);
    public string BatchAddDiscoveredButtonText => SelectedDiscoveredCount > 0
        ? $"⚡ Thêm hàng loạt đã chọn ({SelectedDiscoveredCount})"
        : "⚡ Thêm hàng loạt tất cả";
    public bool HasDiscoveredDevices => DiscoveredDevices.Count > 0;
    public bool IsEditingExisting => SelectedCamera != null;
    public string FormModeTitle => SelectedCamera != null ? $"✏️ CHỈNH SỬA: {SelectedCamera.Name}" : "➕ THÊM MỚI / CẤU HÌNH CHI TIẾT";
    public string SaveButtonText => SelectedCamera != null ? "💾 Cập nhật camera" : "💾 Lưu camera";

    public DeviceManagementViewModel(ICameraRepository? repo = null, IDeviceDiscoveryService? discovery = null)
    {
        _cameraRepo = repo ?? new SqliteCameraRepository();
        _discoveryService = discovery ?? new OnvifDiscoveryService();

        _cameras.CollectionChanged += (_, _) => ApplyCameraFilter();
        _discoveredDevices.CollectionChanged += (_, _) =>
        {
            ApplyDiscoveredFilter();
            NotifyDiscoveredSelectionChanged();
        };

        _ = LoadCamerasAsync();
    }

    partial void OnCameraSearchTextChanged(string value) => ApplyCameraFilter();
    partial void OnCameraStatusFilterChanged(string value) => ApplyCameraFilter();
    partial void OnDiscoveredSearchTextChanged(string value) => ApplyDiscoveredFilter();

    public void ApplyCameraFilter()
    {
        var query = Cameras.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(CameraSearchText))
        {
            string keyword = CameraSearchText.Trim();
            query = query.Where(c =>
                c.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                c.IpAddress.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                c.Brand.ToString().Contains(keyword, StringComparison.OrdinalIgnoreCase));
        }

        if (CameraStatusFilter == "Đang kết nối (Online)")
        {
            query = query.Where(c => c.IsOnline);
        }
        else if (CameraStatusFilter == "Mất kết nối (Offline)")
        {
            query = query.Where(c => c.IsOffline);
        }

        var result = query.ToList();
        FilteredCameras.Clear();
        foreach (var cam in result)
        {
            FilteredCameras.Add(cam);
        }
    }

    public void ApplyDiscoveredFilter()
    {
        var query = DiscoveredDevices.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(DiscoveredSearchText))
        {
            string keyword = DiscoveredSearchText.Trim();
            query = query.Where(d =>
                d.IPAddress.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                d.DisplayModel.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                d.DisplayManufacturer.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                d.DetectedBrand.ToString().Contains(keyword, StringComparison.OrdinalIgnoreCase));
        }

        var result = query.ToList();
        FilteredDiscoveredDevices.Clear();
        foreach (var dev in result)
        {
            FilteredDiscoveredDevices.Add(dev);
        }
    }

    public void NotifyDiscoveredSelectionChanged()
    {
        OnPropertyChanged(nameof(SelectedDiscoveredCount));
        OnPropertyChanged(nameof(BatchAddDiscoveredButtonText));
    }

    partial void OnSelectedCameraChanged(CameraDevice? value)
    {
        foreach (var c in Cameras)
        {
            c.IsBeingEdited = value != null && c.Id == value.Id;
        }

        if (value != null)
        {
            Name = value.Name;
            IpAddress = value.IpAddress;
            RtspPort = value.RtspPort;
            HttpPort = value.HttpPort;
            Username = value.Username;
            Password = value.Password;
            Channel = value.Channel;
            SelectedBrand = value.Brand;
            HasPtz = value.HasPtz;
        }

        OnPropertyChanged(nameof(IsEditingExisting));
        OnPropertyChanged(nameof(FormModeTitle));
        OnPropertyChanged(nameof(SaveButtonText));
    }

    private void NotifySummaryCounts()
    {
        OnPropertyChanged(nameof(TotalCamerasCount));
        OnPropertyChanged(nameof(OnlineCamerasCount));
        OnPropertyChanged(nameof(OfflineCamerasCount));
        OnPropertyChanged(nameof(HasConfiguredCameras));
        OnPropertyChanged(nameof(DiscoveredCount));
        OnPropertyChanged(nameof(SelectedDiscoveredCount));
        OnPropertyChanged(nameof(BatchAddDiscoveredButtonText));
        OnPropertyChanged(nameof(HasDiscoveredDevices));
        ApplyCameraFilter();
        ApplyDiscoveredFilter();
    }

    private void SyncDiscoveredAlreadyAddedState()
    {
        var configuredIps = new HashSet<string>(
            Cameras.Select(c => c.IpAddress.Trim()),
            StringComparer.OrdinalIgnoreCase);

        foreach (var dev in DiscoveredDevices)
        {
            dev.IsAlreadyAdded = configuredIps.Contains(dev.IPAddress.Trim());
        }
        NotifyDiscoveredSelectionChanged();
    }

    [RelayCommand]
    public async Task LoadCamerasAsync()
    {
        string? editingId = SelectedCamera?.Id;
        var list = await _cameraRepo.GetAllAsync();
        Cameras.Clear();
        foreach (var cam in list)
        {
            cam.IsBeingEdited = editingId != null && cam.Id == editingId;
            Cameras.Add(cam);
        }

        SyncDiscoveredAlreadyAddedState();
        NotifySummaryCounts();

        if (Cameras.Count > 0)
        {
            _ = CheckAllConnectionsAsync();
        }
    }

    [RelayCommand]
    public async Task CheckSingleCameraConnectionAsync(CameraDevice? cam)
    {
        if (cam == null || cam.IsChecking) return;

        cam.Status = DeviceStatus.Connecting;
        NotifySummaryCounts();

        try
        {
            var driver = CameraDriverFactory.GetDriver(cam.Brand);
            bool reachable = await driver.TestConnectionAsync(cam);

            if (reachable)
            {
                cam.Status = DeviceStatus.Online;
                cam.LastOnlineTime = DateTime.Now;
                cam.LastError = string.Empty;
            }
            else
            {
                cam.Status = DeviceStatus.Offline;
                cam.LastError = $"Không kết nối được ({cam.IpAddress}:{cam.RtspPort})";
            }

            try
            {
                await _cameraRepo.UpdateAsync(cam);
            }
            catch
            {
                // Ignore if repository was disposed in unit test context
            }
        }
        catch (Exception ex)
        {
            cam.Status = DeviceStatus.Error;
            cam.LastError = $"Lỗi: {ex.Message}";
        }
        finally
        {
            cam.NotifyStatusProperties();
            NotifySummaryCounts();
        }
    }

    [RelayCommand]
    public async Task CheckAllConnectionsAsync()
    {
        if (IsCheckingAll || Cameras.Count == 0) return;

        IsCheckingAll = true;
        try
        {
            var snapshot = Cameras.ToList();
            var tasks = snapshot.Select(CheckSingleCameraConnectionAsync);
            await Task.WhenAll(tasks);
        }
        finally
        {
            IsCheckingAll = false;
            NotifySummaryCounts();
        }
    }

    [RelayCommand]
    public async Task ScanOnvifDevicesAsync()
    {
        if (IsScanning) return;

        IsScanning = true;
        StatusMessage = "🔍 Đang dò quét toàn diện mạng LAN tìm camera (WS-Discovery & Subnet Scanner)...";
        DiscoveredDevices.Clear();
        NotifySummaryCounts();

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            var found = await _discoveryService.AutoDiscoverAllAsync(cts.Token);

            foreach (var dev in found)
            {
                DiscoveredDevices.Add(dev);
            }

            SyncDiscoveredAlreadyAddedState();
            NotifySummaryCounts();

            StatusMessage = found.Count > 0
                ? $"✅ Tìm thấy {found.Count} thiết bị camera trên mạng LAN. Bạn có thể nhấn '⚡ Thêm nhanh' hoặc chọn để cấu hình chi tiết."
                : "⚠️ Không tìm thấy thiết bị ONVIF tự động. Bạn có thể dùng thanh Thêm nhanh bằng IP hoặc nhập thủ công.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Lỗi dò quét: {ex.Message}";
        }
        finally
        {
            IsScanning = false;
            NotifySummaryCounts();
        }
    }

    [RelayCommand]
    public void SelectDiscoveredDevice(DiscoveredDevice? dev)
    {
        if (dev == null) return;

        SelectedDiscoveredDevice = dev;
        SelectedCamera = null;
        IpAddress = dev.IPAddress;
        HttpPort = dev.Port > 0 ? dev.Port : 80;
        RtspPort = 554;
        Name = string.IsNullOrWhiteSpace($"{dev.Manufacturer} {dev.Model}".Trim())
            ? $"Camera {dev.IPAddress}"
            : $"{dev.Manufacturer} {dev.Model}".Trim();
        SelectedBrand = dev.DetectedBrand;
        HasPtz = BatchHasPtz;
        if (!string.IsNullOrWhiteSpace(QuickAddUsername))
            Username = QuickAddUsername;
        if (!string.IsNullOrEmpty(QuickAddPassword))
            Password = QuickAddPassword;

        StatusMessage = $"📝 Đã điền thông tin từ {dev.IPAddress} ({dev.DetectedBrand}). Hãy kiểm tra tài khoản/mật khẩu rồi nhấn 'Lưu camera'.";
    }

    [RelayCommand]
    public async Task QuickAddDiscoveredDeviceAsync(DiscoveredDevice? dev)
    {
        if (dev == null) return;

        var existing = Cameras.FirstOrDefault(c =>
            string.Equals(c.IpAddress.Trim(), dev.IPAddress.Trim(), StringComparison.OrdinalIgnoreCase));

        if (existing != null)
        {
            dev.IsAlreadyAdded = true;
            StatusMessage = $"ℹ️ Camera có địa chỉ IP {dev.IPAddress} đã tồn tại trong hệ thống ('{existing.Name}').";
            return;
        }

        string camName = string.IsNullOrWhiteSpace($"{dev.Manufacturer} {dev.Model}".Trim())
            ? $"Camera {dev.IPAddress}"
            : $"{dev.Manufacturer} {dev.Model} ({dev.IPAddress})".Trim();

        string user = !string.IsNullOrWhiteSpace(QuickAddUsername)
            ? QuickAddUsername.Trim()
            : (!string.IsNullOrWhiteSpace(Username) ? Username.Trim() : "admin");

        string pass = !string.IsNullOrEmpty(QuickAddPassword)
            ? QuickAddPassword
            : Password;

        var newCam = new CameraDevice
        {
            Name = camName,
            IpAddress = dev.IPAddress.Trim(),
            HttpPort = dev.Port > 0 ? dev.Port : 80,
            OnvifPort = dev.Port > 0 ? dev.Port : 80,
            RtspPort = 554,
            Username = user,
            Password = pass,
            Channel = 1,
            Brand = dev.DetectedBrand,
            HasPtz = BatchHasPtz,
            Status = DeviceStatus.Connecting
        };

        bool ok = await _cameraRepo.AddAsync(newCam);
        if (ok)
        {
            Cameras.Add(newCam);
            dev.IsAlreadyAdded = true;
            NotifySummaryCounts();
            StatusMessage = $"⚡ Đã thêm nhanh '{newCam.Name}' ({newCam.IpAddress}). Đang kiểm tra trạng thái kết nối...";
            _ = CheckSingleCameraConnectionAsync(newCam);
        }
        else
        {
            StatusMessage = $"❌ Không thể lưu nhanh camera {dev.IPAddress}.";
        }
    }

    [RelayCommand]
    public void SelectAllDiscovered()
    {
        foreach (var dev in DiscoveredDevices)
        {
            if (!dev.IsAlreadyAdded)
            {
                dev.IsSelected = true;
            }
        }
        NotifyDiscoveredSelectionChanged();
    }

    [RelayCommand]
    public void UnselectAllDiscovered()
    {
        foreach (var dev in DiscoveredDevices)
        {
            dev.IsSelected = false;
        }
        NotifyDiscoveredSelectionChanged();
    }

    [RelayCommand]
    public async Task BatchAddSelectedDiscoveredAsync()
    {
        var selected = DiscoveredDevices.Where(d => d.IsSelected && !d.IsAlreadyAdded).ToList();
        if (selected.Count == 0)
        {
            selected = DiscoveredDevices.Where(d => !d.IsAlreadyAdded).ToList();
        }

        if (selected.Count == 0)
        {
            StatusMessage = "ℹ️ Không có thiết bị ONVIF nào chưa cấu hình để thêm hàng loạt.";
            return;
        }

        int addedCount = 0;
        var newlyAdded = new List<CameraDevice>();

        string user = !string.IsNullOrWhiteSpace(QuickAddUsername) ? QuickAddUsername.Trim() : "admin";
        string pass = QuickAddPassword ?? "";
        string prefix = !string.IsNullOrWhiteSpace(BatchNamePrefix) ? BatchNamePrefix.Trim() : "";
        int rtspPort = BatchRtspPort > 0 ? BatchRtspPort : 554;
        int channel = BatchChannel > 0 ? BatchChannel : 1;

        int index = Cameras.Count + 1;
        foreach (var dev in selected)
        {
            if (Cameras.Any(c => string.Equals(c.IpAddress.Trim(), dev.IPAddress.Trim(), StringComparison.OrdinalIgnoreCase)))
            {
                dev.IsAlreadyAdded = true;
                continue;
            }

            string camName;
            if (!string.IsNullOrWhiteSpace(prefix) && !string.Equals(prefix, "Camera", StringComparison.OrdinalIgnoreCase))
            {
                camName = $"{prefix} {index:D2} ({dev.IPAddress})";
            }
            else
            {
                camName = string.IsNullOrWhiteSpace($"{dev.Manufacturer} {dev.Model}".Trim())
                    ? $"Camera {index:D2} ({dev.IPAddress})"
                    : $"{dev.Manufacturer} {dev.Model} ({dev.IPAddress})".Trim();
            }

            var cam = new CameraDevice
            {
                Name = camName,
                IpAddress = dev.IPAddress.Trim(),
                HttpPort = dev.Port > 0 ? dev.Port : (BatchHttpPort > 0 ? BatchHttpPort : 80),
                OnvifPort = dev.Port > 0 ? dev.Port : (BatchHttpPort > 0 ? BatchHttpPort : 80),
                RtspPort = rtspPort,
                Username = user,
                Password = pass,
                Channel = channel,
                Brand = dev.DetectedBrand,
                HasPtz = BatchHasPtz,
                Status = DeviceStatus.Connecting
            };

            if (await _cameraRepo.AddAsync(cam))
            {
                Cameras.Add(cam);
                newlyAdded.Add(cam);
                dev.IsAlreadyAdded = true;
                addedCount++;
                index++;
            }
        }

        NotifySummaryCounts();
        StatusMessage = $"⚡ Đã thêm hàng loạt {addedCount} camera ONVIF vào hệ thống. Đang kiểm tra kết nối...";

        foreach (var cam in newlyAdded)
        {
            _ = CheckSingleCameraConnectionAsync(cam);
        }
    }

    [RelayCommand]
    public async Task QuickAddAllDiscoveredAsync()
    {
        SelectAllDiscovered();
        await BatchAddSelectedDiscoveredAsync();
    }

    [RelayCommand]
    public async Task BatchAddByIpRangeAsync()
    {
        if (string.IsNullOrWhiteSpace(BatchStartIp))
        {
            StatusMessage = "⚠️ Vui lòng nhập địa chỉ IP bắt đầu (ví dụ: 192.168.21.10).";
            return;
        }

        var targetIps = ParseIpRangeOrList(BatchStartIp, BatchEndIp);
        if (targetIps.Count == 0)
        {
            StatusMessage = "⚠️ Dải địa chỉ IP không hợp lệ. Vui lòng nhập định dạng IPv4 (VD: Từ 192.168.21.10 đến 192.168.21.20).";
            return;
        }

        string user = !string.IsNullOrWhiteSpace(QuickAddUsername) ? QuickAddUsername.Trim() : "admin";
        string pass = QuickAddPassword ?? "";
        string prefix = string.IsNullOrWhiteSpace(BatchNamePrefix) ? "Camera" : BatchNamePrefix.Trim();
        int rtspPort = BatchRtspPort > 0 ? BatchRtspPort : 554;
        int httpPort = BatchHttpPort > 0 ? BatchHttpPort : 80;
        int channel = BatchChannel > 0 ? BatchChannel : 1;

        int addedCount = 0;
        int skippedCount = 0;
        var newlyAdded = new List<CameraDevice>();
        int index = Cameras.Count + 1;

        foreach (string ip in targetIps)
        {
            if (Cameras.Any(c => string.Equals(c.IpAddress.Trim(), ip, StringComparison.OrdinalIgnoreCase)))
            {
                skippedCount++;
                continue;
            }

            var cam = new CameraDevice
            {
                Name = $"{prefix} {index:D2} ({ip})",
                IpAddress = ip,
                RtspPort = rtspPort,
                HttpPort = httpPort,
                OnvifPort = httpPort,
                Username = user,
                Password = pass,
                Channel = channel,
                Brand = QuickAddBrand,
                HasPtz = BatchHasPtz,
                Status = DeviceStatus.Connecting
            };

            if (await _cameraRepo.AddAsync(cam))
            {
                Cameras.Add(cam);
                newlyAdded.Add(cam);
                addedCount++;
                index++;
            }
        }

        SyncDiscoveredAlreadyAddedState();
        NotifySummaryCounts();

        if (addedCount > 0)
        {
            string skipMsg = skippedCount > 0 ? $" (bỏ qua {skippedCount} IP đã tồn tại)" : "";
            StatusMessage = $"⚡ Đã thêm hàng loạt {addedCount} camera theo dải IP{skipMsg}. Đang kiểm tra trạng thái kết nối...";
            foreach (var cam in newlyAdded)
            {
                _ = CheckSingleCameraConnectionAsync(cam);
            }
        }
        else
        {
            StatusMessage = skippedCount > 0
                ? $"ℹ️ Tất cả {skippedCount} địa chỉ IP trong dải đều đã tồn tại trong hệ thống."
                : "❌ Không thể thêm hàng loạt camera.";
        }
    }

    public static List<string> ParseIpRangeOrList(string startInput, string endInput)
    {
        var result = new List<string>();
        if (string.IsNullOrWhiteSpace(startInput)) return result;

        string rawStart = startInput.Trim();

        // Support comma or semicolon separated list of IPs in startInput
        if (rawStart.Contains(',') || rawStart.Contains(';'))
        {
            var tokens = rawStart.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var token in tokens)
            {
                string ip = token.Trim();
                if (!string.IsNullOrEmpty(ip))
                {
                    result.Add(ip);
                }
            }
            return result;
        }

        var startParts = rawStart.Split('.');
        if (startParts.Length != 4 || !int.TryParse(startParts[3], out int startOctet))
        {
            result.Add(rawStart);
            return result;
        }

        string subnetPrefix = $"{startParts[0]}.{startParts[1]}.{startParts[2]}";
        int endOctet = startOctet;

        if (!string.IsNullOrWhiteSpace(endInput))
        {
            string rawEnd = endInput.Trim();
            if (int.TryParse(rawEnd, out int directLastOctet))
            {
                endOctet = directLastOctet;
            }
            else
            {
                var endParts = rawEnd.Split('.');
                if (endParts.Length == 4 && int.TryParse(endParts[3], out int parsedLastOctet))
                {
                    endOctet = parsedLastOctet;
                }
            }
        }

        startOctet = Math.Clamp(startOctet, 1, 254);
        endOctet = Math.Clamp(endOctet, 1, 254);
        if (endOctet < startOctet)
        {
            (startOctet, endOctet) = (endOctet, startOctet);
        }

        // Cap batch at 64 devices at once for safety
        int maxOctet = Math.Min(endOctet, startOctet + 63);
        for (int i = startOctet; i <= maxOctet; i++)
        {
            result.Add($"{subnetPrefix}.{i}");
        }

        return result;
    }

    [RelayCommand]
    public async Task QuickAddByIpAsync()
    {
        if (string.IsNullOrWhiteSpace(QuickAddIp))
        {
            StatusMessage = "⚠️ Vui lòng nhập địa chỉ IP cần thêm nhanh (ví dụ: 192.168.21.100).";
            return;
        }

        string raw = QuickAddIp.Trim();
        string ip = raw;
        int rtspPort = BatchRtspPort > 0 ? BatchRtspPort : 554;

        if (raw.Contains(':'))
        {
            var parts = raw.Split(':', 2);
            ip = parts[0].Trim();
            if (int.TryParse(parts[1].Trim(), out int parsedPort) && parsedPort > 0)
            {
                rtspPort = parsedPort;
            }
        }

        if (Cameras.Any(c => string.Equals(c.IpAddress.Trim(), ip, StringComparison.OrdinalIgnoreCase)))
        {
            StatusMessage = $"ℹ️ Camera có địa chỉ IP {ip} đã tồn tại trong hệ thống.";
            return;
        }

        var cam = new CameraDevice
        {
            Name = $"Camera {ip}",
            IpAddress = ip,
            RtspPort = rtspPort,
            HttpPort = BatchHttpPort > 0 ? BatchHttpPort : 80,
            OnvifPort = BatchHttpPort > 0 ? BatchHttpPort : 80,
            Username = string.IsNullOrWhiteSpace(QuickAddUsername) ? "admin" : QuickAddUsername.Trim(),
            Password = QuickAddPassword ?? "",
            Channel = BatchChannel > 0 ? BatchChannel : 1,
            Brand = QuickAddBrand,
            HasPtz = BatchHasPtz,
            Status = DeviceStatus.Connecting
        };

        if (await _cameraRepo.AddAsync(cam))
        {
            Cameras.Add(cam);
            QuickAddIp = "";
            SyncDiscoveredAlreadyAddedState();
            NotifySummaryCounts();
            StatusMessage = $"⚡ Đã thêm nhanh '{cam.Name}' ({cam.IpAddress}). Đang kiểm tra kết nối...";
            _ = CheckSingleCameraConnectionAsync(cam);
        }
        else
        {
            StatusMessage = "❌ Thêm nhanh camera thất bại.";
        }
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

            if (SelectedCamera != null)
            {
                SelectedCamera.Status = success ? DeviceStatus.Online : DeviceStatus.Offline;
                if (success)
                {
                    SelectedCamera.LastOnlineTime = DateTime.Now;
                    SelectedCamera.LastError = string.Empty;
                }
                else
                {
                    SelectedCamera.LastError = $"Không kết nối được ({IpAddress}:{RtspPort})";
                }
                NotifySummaryCounts();
            }

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

        bool isEditing = SelectedCamera != null;
        var cam = SelectedCamera ?? new CameraDevice();
        cam.Name = string.IsNullOrWhiteSpace(Name) ? $"Camera {IpAddress}" : Name.Trim();
        cam.IpAddress = IpAddress.Trim();
        cam.RtspPort = RtspPort;
        cam.HttpPort = HttpPort;
        cam.Username = Username.Trim();
        cam.Password = Password;
        cam.Channel = Channel;
        cam.Brand = SelectedBrand;
        cam.HasPtz = HasPtz;
        cam.Status = DeviceStatus.Online;

        bool ok = isEditing
            ? await _cameraRepo.UpdateAsync(cam)
            : await _cameraRepo.AddAsync(cam);

        if (ok)
        {
            string savedName = cam.Name;
            string savedIp = cam.IpAddress;
            ResetForm();
            await LoadCamerasAsync();
            StatusMessage = isEditing
                ? $"✅ Đã cập nhật thông tin camera '{savedName}' ({savedIp}) thành công!"
                : $"✅ Đã lưu cấu hình camera '{savedName}' ({savedIp}) thành công!";
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
            StatusMessage = $"🗑️ Đã xóa camera '{cam.Name}'.";
            if (SelectedCamera?.Id == cam.Id)
            {
                ResetForm();
            }
            await LoadCamerasAsync();
        }
    }

    [RelayCommand]
    public void EditCamera(CameraDevice? cam)
    {
        if (cam == null) return;
        SelectedCamera = cam;
        foreach (var c in Cameras)
        {
            c.IsBeingEdited = c.Id == cam.Id;
        }
        Name = cam.Name;
        IpAddress = cam.IpAddress;
        RtspPort = cam.RtspPort;
        HttpPort = cam.HttpPort;
        Username = cam.Username;
        Password = cam.Password;
        Channel = cam.Channel;
        SelectedBrand = cam.Brand;
        HasPtz = cam.HasPtz;
        OnPropertyChanged(nameof(IsEditingExisting));
        OnPropertyChanged(nameof(FormModeTitle));
        OnPropertyChanged(nameof(SaveButtonText));
        StatusMessage = $"✏️ Đang chỉnh sửa camera '{cam.Name}' ({cam.IpAddress}). Thay đổi thông tin bên dưới rồi nhấn 'Cập nhật camera'.";
    }

    [RelayCommand]
    public void ResetForm()
    {
        SelectedCamera = null;
        foreach (var c in Cameras)
        {
            c.IsBeingEdited = false;
        }
        Name = "Camera 01";
        IpAddress = "192.168.1.100";
        RtspPort = 554;
        HttpPort = 80;
        Username = "admin";
        Password = "";
        Channel = 1;
        SelectedBrand = CameraBrand.Hikvision;
        HasPtz = false;
        OnPropertyChanged(nameof(IsEditingExisting));
        OnPropertyChanged(nameof(FormModeTitle));
        OnPropertyChanged(nameof(SaveButtonText));
    }
}
