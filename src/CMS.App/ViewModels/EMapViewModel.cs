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
    private string _mapTitle = "Zone 1 - Main Headquarters Ground Floor";

    [ObservableProperty]
    private string _statusMessage = "Click on canvas to place selected camera. Click placed camera to preview.";

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
            StatusMessage = "Please select a camera from the list first before placing on map.";
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

        StatusMessage = $"Placed {SelectedCamera.Name} at ({xPercent:F0}%, {yPercent:F0}%) on map.";
    }
}
