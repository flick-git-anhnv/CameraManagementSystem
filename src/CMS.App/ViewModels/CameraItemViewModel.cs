using CMS.Core.Entities;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CMS.App.ViewModels;

public partial class CameraItemViewModel : ObservableObject
{
    public CameraDevice Device { get; }

    public string Id => Device.Id;
    public string Name => Device.Name;
    public string IpAddress => Device.IpAddress;
    public bool IsOnline => Device.IsOnline;
    public string Brand => Device.Brand.ToString();
    public bool HasPtz => Device.HasPtz;

    [ObservableProperty]
    private bool _isViewing = false;

    [ObservableProperty]
    private bool _isRecording = false;

    public CameraItemViewModel(CameraDevice device)
    {
        Device = device;
        Device.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(CameraDevice.HasPtz))
            {
                OnPropertyChanged(nameof(HasPtz));
            }
            else if (e.PropertyName == nameof(CameraDevice.Name))
            {
                OnPropertyChanged(nameof(Name));
            }
            else if (e.PropertyName == nameof(CameraDevice.IpAddress))
            {
                OnPropertyChanged(nameof(IpAddress));
            }
            else if (e.PropertyName == nameof(CameraDevice.Status) || e.PropertyName == nameof(CameraDevice.IsOnline))
            {
                OnPropertyChanged(nameof(IsOnline));
            }
        };
    }
}
