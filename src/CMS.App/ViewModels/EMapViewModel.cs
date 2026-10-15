using CMS.Core.Entities;
using CMS.Core.Interfaces;
using CMS.Infrastructure.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace CMS.App.ViewModels;

public partial class EMapViewModel : ObservableObject
{
    private readonly ICameraRepository _cameraRepo;
    private readonly IEmapConfigRepository _emapRepo;
    private bool _isSampleInitialized;

    public event Action? NodesChanged;
    public event Action<string>? BackgroundImageLoaded;

    [ObservableProperty]
    private ObservableCollection<CameraDevice> _availableCameras = new();

    [ObservableProperty]
    private ObservableCollection<CameraMapNode> _placedNodes = new();

    [ObservableProperty]
    private CameraDevice? _selectedCamera;

    [ObservableProperty]
    private string _mapTitle = "Khu vực 1 - Mặt bằng Tầng 1 Tòa nhà Trụ sở";

    [ObservableProperty]
    private string _mapImagePath = string.Empty;

    [ObservableProperty]
    private bool _isSaving;

    [ObservableProperty]
    private string _statusMessage = "Chọn camera rồi nhấp vào bản đồ để đặt/đổi vị trí, kéo thả trực tiếp camera trên bản đồ, hoặc nhấn ✕ để xóa. Nhấn [Lưu cấu hình] để lưu lại.";

    public EMapViewModel(ICameraRepository? cameraRepo = null, IEmapConfigRepository? emapRepo = null)
    {
        _cameraRepo = cameraRepo ?? new SqliteCameraRepository();
        _emapRepo = emapRepo ?? new SqliteEmapConfigRepository();
        _ = LoadCamerasAsync();
    }

    public async Task LoadCamerasAsync()
    {
        var list = await _cameraRepo.GetAllAsync();
        AvailableCameras.Clear();
        foreach (var cam in list)
        {
            AvailableCameras.Add(cam);
        }

        var validDict = AvailableCameras.ToDictionary(c => c.Id, c => c.Name);

        // Nạp cấu hình E-Map đã lưu từ CSDL
        var savedConfig = await _emapRepo.GetConfigAsync();
        if (savedConfig != null && (!string.IsNullOrEmpty(savedConfig.ImagePath) || savedConfig.CameraNodes.Count > 0))
        {
            _isSampleInitialized = true;
            if (!string.IsNullOrEmpty(savedConfig.Name))
            {
                MapTitle = savedConfig.Name;
            }

            if (!string.IsNullOrEmpty(savedConfig.ImagePath))
            {
                MapImagePath = savedConfig.ImagePath;
                BackgroundImageLoaded?.Invoke(MapImagePath);
            }

            PlacedNodes.Clear();
            foreach (var node in savedConfig.CameraNodes)
            {
                if (validDict.TryGetValue(node.CameraId, out var camName))
                {
                    node.CameraName = camName;
                    PlacedNodes.Add(node);
                }
            }

            NodesChanged?.Invoke();
            return;
        }

        // Nếu chưa từng lưu cấu hình, đồng bộ node hiện có hoặc tạo mẫu lần đầu
        var validIds = AvailableCameras.Select(c => c.Id).ToHashSet();
        for (int i = PlacedNodes.Count - 1; i >= 0; i--)
        {
            if (!validIds.Contains(PlacedNodes[i].CameraId))
            {
                PlacedNodes.RemoveAt(i);
            }
            else
            {
                var cam = AvailableCameras.First(c => c.Id == PlacedNodes[i].CameraId);
                PlacedNodes[i].CameraName = cam.Name;
            }
        }

        if (!_isSampleInitialized && PlacedNodes.Count == 0 && AvailableCameras.Count > 0)
        {
            _isSampleInitialized = true;
            InitDefaultNodes();
        }
        else
        {
            NodesChanged?.Invoke();
        }
    }

    private void InitDefaultNodes()
    {
        PlacedNodes.Clear();
        if (AvailableCameras.Count > 0)
        {
            PlacedNodes.Add(new CameraMapNode
            {
                CameraId = AvailableCameras[0].Id,
                CameraName = AvailableCameras[0].Name,
                XPercent = 25,
                YPercent = 35,
                RotationAngle = 45
            });

            if (AvailableCameras.Count > 1)
            {
                PlacedNodes.Add(new CameraMapNode
                {
                    CameraId = AvailableCameras[1].Id,
                    CameraName = AvailableCameras[1].Name,
                    XPercent = 65,
                    YPercent = 55,
                    RotationAngle = 135
                });
            }
        }
        NodesChanged?.Invoke();
    }

    public void PlaceCameraOnMap(double xPercent, double yPercent)
    {
        if (SelectedCamera == null)
        {
            StatusMessage = "Vui lòng chọn camera trong danh sách bên trái trước khi nhấp lên bản đồ, hoặc kéo thả trực tiếp camera trên bản đồ.";
            return;
        }

        xPercent = Math.Clamp(xPercent, 3.0, 97.0);
        yPercent = Math.Clamp(yPercent, 3.0, 97.0);

        var existing = PlacedNodes.FirstOrDefault(n => n.CameraId == SelectedCamera.Id);
        if (existing != null)
        {
            existing.XPercent = xPercent;
            existing.YPercent = yPercent;
            existing.CameraName = SelectedCamera.Name;
            StatusMessage = $"Đã cập nhật vị trí {SelectedCamera.Name} tới tọa độ ({xPercent:F0}%, {yPercent:F0}%).";
        }
        else
        {
            PlacedNodes.Add(new CameraMapNode
            {
                CameraId = SelectedCamera.Id,
                CameraName = SelectedCamera.Name,
                XPercent = xPercent,
                YPercent = yPercent,
                RotationAngle = 0
            });
            StatusMessage = $"Đã gắn {SelectedCamera.Name} tại tọa độ ({xPercent:F0}%, {yPercent:F0}%) trên bản đồ.";
        }

        NodesChanged?.Invoke();
    }

    public void UpdateNodePosition(string cameraId, double xPercent, double yPercent)
    {
        var existing = PlacedNodes.FirstOrDefault(n => n.CameraId == cameraId);
        if (existing == null) return;

        existing.XPercent = Math.Clamp(xPercent, 3.0, 97.0);
        existing.YPercent = Math.Clamp(yPercent, 3.0, 97.0);
        StatusMessage = $"Đã di chuyển {existing.CameraName} tới tọa độ ({existing.XPercent:F0}%, {existing.YPercent:F0}%).";
        NodesChanged?.Invoke();
    }

    [RelayCommand]
    public void RemoveCameraFromMap(CameraDevice? camera)
    {
        var target = camera ?? SelectedCamera;
        if (target == null)
        {
            StatusMessage = "Vui lòng chọn camera cần xóa khỏi bản đồ.";
            return;
        }

        RemoveNodeByCameraId(target.Id, target.Name);
    }

    public void RemoveNodeByCameraId(string cameraId, string? fallbackName = null)
    {
        var existing = PlacedNodes.FirstOrDefault(n => n.CameraId == cameraId);
        if (existing != null)
        {
            var name = existing.CameraName;
            PlacedNodes.Remove(existing);
            StatusMessage = $"Đã xóa camera \"{name}\" khỏi sơ đồ mặt bằng.";
            NodesChanged?.Invoke();
        }
        else if (!string.IsNullOrEmpty(fallbackName))
        {
            StatusMessage = $"Camera \"{fallbackName}\" hiện chưa được đặt trên bản đồ.";
        }
    }

    [RelayCommand]
    public void ResetDefaultPositions()
    {
        _isSampleInitialized = true;
        InitDefaultNodes();
        StatusMessage = "Đã đặt lại vị trí camera mặc định trên sơ đồ mặt bằng.";
    }

    [RelayCommand]
    public void ClearAllNodes()
    {
        if (PlacedNodes.Count == 0)
        {
            StatusMessage = "Bản đồ hiện không có camera nào.";
            return;
        }

        PlacedNodes.Clear();
        StatusMessage = "Đã xóa toàn bộ camera khỏi sơ đồ mặt bằng. Nhấn [Lưu cấu hình] để xác nhận lưu.";
        NodesChanged?.Invoke();
    }

    [RelayCommand]
    public async Task SaveConfigAsync()
    {
        IsSaving = true;
        try
        {
            var config = new EmapConfig
            {
                Id = "default_emap",
                Name = MapTitle,
                ImagePath = MapImagePath,
                CameraNodes = PlacedNodes.ToList()
            };

            bool success = await _emapRepo.SaveConfigAsync(config);
            if (success)
            {
                StatusMessage = "✅ Đã lưu cấu hình sơ đồ mặt bằng và vị trí camera thành công.";
            }
            else
            {
                StatusMessage = "❌ Lưu cấu hình sơ đồ mặt bằng thất bại.";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ Lỗi khi lưu cấu hình: {ex.Message}";
        }
        finally
        {
            IsSaving = false;
        }
    }
}
