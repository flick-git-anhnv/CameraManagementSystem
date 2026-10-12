using System;
using Avalonia.Controls;

namespace Kztek.Cameras;

/// <summary>
/// Avalonia port của VideoCaptureDeviceSettingPage (WinForms → Avalonia, task G5.8).
/// <br/>
/// AForge (FilterInfoCollection) device enumeration đã bị comment trong WinForms gốc
/// → <see cref="VideoCaptureDeviceSettingPageViewModel.DeviceItems"/> luôn rỗng;
/// <see cref="Completed"/> luôn true sau constructor (catch block không được kích hoạt).
/// <br/>
/// chkUsingDeinterlaceFilter_CheckedChanged → IsEnabled binding trong AXAML (ADR §6.1 E21).
/// btnFindFilter_Click (MessageBox.Show) → FindFilterCommand + MsBox.Avalonia (ADR §6.1).
/// <br/>
/// Public API surface giữ nguyên để consumer có thể dùng tương tự bản WinForms.
/// </summary>
public partial class VideoCaptureDeviceSettingPage : UserControl
{
    //FilterInfoCollection videoDevices;
    // state changed event
    public event EventHandler? StateChanged;

    private readonly VideoCaptureDeviceSettingPageViewModel _vm;

    // ── Static item lists (dùng bởi AXAML via x:Static) ─────────────────────────

    /// <summary>Danh sách kênh (parity với cbxChanel.Items trong WinForms — "0".."16").</summary>
    public static readonly string[] ChanelItems =
    {
        "0","1","2","3","4","5","6","7","8",
        "9","10","11","12","13","14","15","16"
    };

    /// <summary>
    /// Danh sách frame rate mong muốn
    /// (parity với cbxDesiredFrameRate.Items trong WinForms — "Uncontrolled", "1".."30").
    /// </summary>
    public static readonly string[] DesiredFrameRateItems =
    {
        "Uncontrolled",
        "1","2","3","4","5","6","7","8","9","10",
        "11","12","13","14","15","16","17","18","19","20",
        "21","22","23","24","25","26","27","28","29","30"
    };

    /// <summary>Danh sách kích thước frame (parity với cbxDesiredFrameSize.Items trong WinForms).</summary>
    public static readonly string[] DesiredFrameSizeItems =
    {
        "Uncontrolled",
        "320x180",  "320x240",  "352x240",  "352x288",
        "640x360",  "640x480",  "704x480",  "704x576",
        "720x576",  "768x576",  "800x600",  "960x720",
        "1024x768", "1280x720", "1280x960", "1280x1024",
        "1600x1200","1920x1080","2048x1536"
    };

    /// <summary>Danh sách deinterlace filter (parity với cbxDeinterlaceFilterName.Items trong WinForms).</summary>
    public static readonly string[] DeinterlaceFilterNameItems =
    {
        "Deinterlace Filter","blend","bob","discard",
        "linear","mean","x","yadif","yadif2x"
    };

    /// <summary>Danh sách video decoder (parity với cbxVideoDecoder.Items trong WinForms).</summary>
    public static readonly string[] VideoDecoderItems = { "DirectShow", "VLC", "VLC Player" };

    // ── Constructor ───────────────────────────────────────────────────────────────

    public VideoCaptureDeviceSettingPage()
    {
        _vm = new VideoCaptureDeviceSettingPageViewModel();
        DataContext = _vm;
        InitializeComponent();

        // show device list
        try
        {
            // enumerate video devices
            //videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);

            //if (videoDevices.Count == 0)
            //    throw new ApplicationException();

            //// add all devices to combo
            //foreach (FilterInfo device in videoDevices)
            //{
            //    cbxDevice.Items.Add(device.Name);
            //}
            _vm.Completed = true;
        }
        catch (ApplicationException)
        {
            _vm.DeviceItems.Add("No local capture devices");
            _vm.IsDeviceEnabled = false;
        }

        _vm.DeviceIndex = 0;
        _vm.DesiredFrameRateIndex = 0;
        _vm.DesiredFrameSizeIndex = 0;
        _vm.ChanelIndex = 0;
    }

    // ── IWizardPage-compatible surface ────────────────────────────────────────────

    // Completed property
    public bool Completed => _vm.Completed;

    // Show the page
    public void Display() => Focus();

    // Apply the page
    public bool Apply() => true;

    // ── Primary data properties ───────────────────────────────────────────────────

    public string VideoSource
    {
        get
        {
            return "";
            //return videoDevices[cbxDevice.SelectedIndex].MonikerString;
        }
        set
        {
            //for (int i = 0; i < videoDevices.Count; i++)
            //{
            //    if (videoDevices[i].MonikerString == value)
            //    {
            //        cbxDevice.SelectedIndex = i;
            //        break;
            //    }
            //}
        }
    }

    // Login property
    private string login = "";
    public string Login
    {
        get { return login; }
        set { login = value; }
    }

    // Password property
    private string password = "";
    public string Password
    {
        get { return password; }
        set { password = value; }
    }

    private int streamType = 1;
    public int StreamType
    {
        get { return streamType; }
        set { streamType = value; }
    }

    private int protocolType = 0;
    public int ProtocolType
    {
        get { return protocolType; }
        set { protocolType = value; }
    }

    // chanel property
    public int Chanel
    {
        get { return _vm.ChanelIndex; }
        set { _vm.ChanelIndex = value; }
    }

    // Resolution property
    public string Resolution
    {
        get
        {
            var idx = _vm.DesiredFrameSizeIndex;
            return (uint)idx < (uint)DesiredFrameSizeItems.Length
                ? DesiredFrameSizeItems[idx]
                : DesiredFrameSizeItems[0];
        }
        set
        {
            var idx = Array.IndexOf(DesiredFrameSizeItems, value);
            _vm.DesiredFrameSizeIndex = idx >= 0 ? idx : 0;
        }
    }

    // Quality property
    private string quality = "Standard";
    public string Quality
    {
        get { return quality; }
        set { quality = value; }
    }

    public int FrameRate
    {
        get { return _vm.DesiredFrameRateIndex; }
        set { _vm.DesiredFrameRateIndex = value; }
    }

    public bool UsingDeinterlaceFilter
    {
        get { return _vm.UsingDeinterlaceFilter; }
        set { _vm.UsingDeinterlaceFilter = value; }
    }

    public string DeinterlaceFilterName
    {
        get { return _vm.SelectedDeinterlaceFilterName ?? string.Empty; }
        set { _vm.SelectedDeinterlaceFilterName = value; }
    }

    public int UsingPlugins
    {
        get { return _vm.VideoDecoderIndex; }
        set
        {
            if (value <= 2)
                _vm.VideoDecoderIndex = value;
            else
                _vm.VideoDecoderIndex = 0;
        }
    }
}
