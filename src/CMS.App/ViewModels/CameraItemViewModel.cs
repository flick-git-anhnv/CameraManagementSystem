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

    [ObservableProperty]
    private bool _isViewing = false;

    [ObservableProperty]
    private bool _isRecording = false;

    public CameraItemViewModel(CameraDevice device)
    {
        Device = device;
    }
}
