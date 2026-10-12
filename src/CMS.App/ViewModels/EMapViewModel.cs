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

    [ObservableProperty]
    private ObservableCollection<CameraDevice> _availableCameras = new();

    [ObservableProperty]
    private ObservableCollection<CameraMapNode> _placedNodes = new();

    [ObservableProperty]
    private CameraDevice? _selectedCamera;

    [ObservableProperty]
    private string _mapTitle = "Khu vực 1 - Mặt bằng Tầng 1 Tòa nhà Trụ sở";

    [ObservableProperty]
    private string _statusMessage = "Chọn camera trong danh sách, sau đó nhấn vào sơ đồ mặt bằng để gán vị trí. Nhấn vào camera đã đặt để xem trực tiếp.";

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

        // Tạo sẵn một số điểm camera mẫu nếu chưa có
        if (PlacedNodes.Count == 0 && AvailableCameras.Count > 0)
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
    }

    public void PlaceCameraOnMap(double xPercent, double yPercent)
    {
        if (SelectedCamera == null)
        {
            StatusMessage = "Vui lòng chọn camera trong danh sách trước khi đặt lên bản đồ.";
            return;
        }

        var existing = PlacedNodes.FirstOrDefault(n => n.CameraId == SelectedCamera.Id);
        if (existing != null)
        {
            existing.XPercent = xPercent;
            existing.YPercent = yPercent;
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
        }

        StatusMessage = $"Đã gắn {SelectedCamera.Name} tại tọa độ ({xPercent:F0}%, {yPercent:F0}%) trên bản đồ.";
    }
}
