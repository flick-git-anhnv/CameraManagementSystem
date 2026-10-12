using CMS.Core.Enums;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CMS.Core.Entities;

/// <summary>
/// Thiết bị camera được phát hiện qua mạng nội bộ (ONVIF WS-Discovery / Broadcast)
/// </summary>
public class DiscoveredDevice : INotifyPropertyChanged
{
    private bool _isAlreadyAdded;
    private bool _isSelected = true;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string IPAddress { get; set; } = string.Empty;
    public int Port { get; set; } = 80;
    public string ServiceUrl { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;
    public string MacAddress { get; set; } = string.Empty;
    public CameraBrand DetectedBrand { get; set; } = CameraBrand.ONVIF;
    public DateTime DiscoveredAt { get; set; } = DateTime.Now;

    public bool IsSelected
    {
        get => _isSelected && !_isAlreadyAdded;
        set
        {
            if (_isSelected != value)
            {
                _isSelected = value;
                OnPropertyChanged();
            }
        }
    }

    public bool IsAlreadyAdded
    {
        get => _isAlreadyAdded;
        set
        {
            if (_isAlreadyAdded != value)
            {
                _isAlreadyAdded = value;
                if (value)
                {
                    _isSelected = false;
                }
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsSelected));
                OnPropertyChanged(nameof(CanQuickAdd));
                OnPropertyChanged(nameof(QuickAddButtonText));
            }
        }
    }

    public bool CanQuickAdd => !IsAlreadyAdded;
    public string QuickAddButtonText => IsAlreadyAdded ? "✓ Đã cấu hình" : "⚡ Thêm";

    public string DisplayModel => string.IsNullOrWhiteSpace(Model) ? $"{DetectedBrand} IPC / NVR" : Model;
    public string DisplayManufacturer => string.IsNullOrWhiteSpace(Manufacturer) ? DetectedBrand.ToString() : Manufacturer;
    public string EndpointDisplay => Port > 0 ? $"{IPAddress}:{Port}" : IPAddress;

    public override string ToString() => $"{Manufacturer} {Model} ({IPAddress}:{Port})";

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
