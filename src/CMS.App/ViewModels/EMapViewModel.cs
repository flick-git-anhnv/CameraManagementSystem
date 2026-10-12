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
    private bool _isSampleInitialized;

    public event Action? NodesChanged;

    [ObservableProperty]
    private ObservableCollection<CameraDevice> _availableCameras = new();

    [ObservableProperty]
    private ObservableCollection<CameraMapNode> _placedNodes = new();

    [ObservableProperty]
    private CameraDevice? _selectedCamera;

    [ObservableProperty]
    private string _mapTitle = "Khu vực 1 - Mặt bằng Tầng 1 Tòa nhà Trụ sở";

    [ObservableProperty]
    private string _statusMessage = "Chọn camera rồi nhấp vào bản đồ để đặt/đổi vị trí, kéo thả trực tiếp camera trên bản đồ, hoặc nhấn ✕ để xóa.";

    public EMapViewModel(ICameraRepository? cameraRepo = null)
    {
        _cameraRepo = cameraRepo ?? new SqliteCameraRepository();
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

        // Đồng bộ tên hoặc xóa các node của camera không còn tồn tại
        var validIds = AvailableCameras.Select(c => c.Id).ToHashSet();
        var removedAny = false;
        for (int i = PlacedNodes.Count - 1; i >= 0; i--)
        {
            if (!validIds.Contains(PlacedNodes[i].CameraId))
            {
                PlacedNodes.RemoveAt(i);
                removedAny = true;
            }
            else
            {
                var cam = AvailableCameras.First(c => c.Id == PlacedNodes[i].CameraId);
                PlacedNodes[i].CameraName = cam.Name;
            }
        }

        // Tạo sẵn một số điểm camera mẫu lần đầu tiên
        if (!_isSampleInitialized && PlacedNodes.Count == 0 && AvailableCameras.Count > 0)
        {
            _isSampleInitialized = true;
            InitDefaultNodes();
        }
        else if (removedAny)
        {
            NodesChanged?.Invoke();
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
        StatusMessage = "Đã xóa toàn bộ camera khỏi sơ đồ mặt bằng.";
        NodesChanged?.Invoke();
    }
}
