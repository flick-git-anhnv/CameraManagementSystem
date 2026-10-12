using System;
using Avalonia.Controls;

namespace Kztek.Cameras;

/// <summary>
/// Avalonia port của VideoFileSettingPage (WinForms → Avalonia, task G5.9).
/// <br/>
/// Thay đổi chính: OpenFileDialog.ShowDialog() → IStorageProvider.OpenFilePickerAsync
/// (async, qua BrowseCommand trong ViewModel — ADR §6.2 E22, không dùng async void).
/// <br/>
/// Public API surface giữ nguyên để consumer có thể dùng tương tự bản WinForms.
/// </summary>
public partial class VideoFileSettingPage : UserControl
{
    private readonly VideoFileSettingPageViewModel _vm;

    // ── Static item lists (dùng bởi AXAML via x:Static) ──────────────

    /// <summary>Danh sách kích thước frame (parity với cbxDesiredFrameSize.Items trong WinForms).</summary>
    public static readonly string[] FrameSizeItems =
    {
        "Uncontrolled",
        "320x180",  "320x240",  "352x240",  "352x288",
        "640x360",  "640x480",  "704x480",  "704x576",
        "720x576",  "768x576",  "800x600",  "960x720",
        "1024x768", "1280x720", "1280x960", "1280x1024",
        "1600x1200","1920x1080","2048x1536"
    };

    /// <summary>Danh sách video decoder (parity với cbxVideoDecoder.Items trong WinForms).</summary>
    public static readonly string[] VideoDecoderItems = { "DirectShow", "VLC", "FFMPEG" };

    // ── Events ─────────────────────────────────────────────────────────

    /// <summary>Fires khi VideoSource thay đổi — parity với WinForms StateChanged event.</summary>
    public event EventHandler? StateChanged;

    // ── Constructor ────────────────────────────────────────────────────

    public VideoFileSettingPage()
    {
        _vm = new VideoFileSettingPageViewModel();
        // Proxy StateChanged từ VM ra ngoài (consumer subscribe event trên UserControl)
        _vm.StateChanged += (_, e) => StateChanged?.Invoke(this, e);
        DataContext = _vm;
        InitializeComponent();
        // Behavior parity: completed = (VideoSource != "") — VM init với "" → Completed = false
    }

    // ── IWizardPage-compatible surface ────────────────────────────────

    /// <summary>True khi đường dẫn file đã được nhập.</summary>
    public bool Completed => _vm.Completed;

    /// <summary>Hiển thị trang — focus control (parity với WinForms txtVideoFile.Focus()).</summary>
    public void Display() => Focus();

    /// <summary>Apply — không có validation; luôn trả true (parity với WinForms).</summary>
    public bool Apply() => true;

    // ── Primary data property (UI-bound qua ViewModel) ─────────────────

    /// <summary>Đường dẫn file video đã chọn (bound TwoWay với KzTextBox qua ViewModel).</summary>
    public string VideoSource
    {
        get => _vm.VideoSource;
        set => _vm.VideoSource = value;
    }

    // ── Resolution (maps to cbxDesiredFrameSize) ──────────────────────

    /// <summary>
    /// Kích thước frame đã chọn (VD: "640x480").
    /// Getter: FrameSizeItems[DesiredFrameSizeIndex].
    /// Setter: tìm index trong FrameSizeItems.
    /// </summary>
    public string Resolution
    {
        get
        {
            var idx = _vm.DesiredFrameSizeIndex;
            return (uint)idx < (uint)FrameSizeItems.Length
                ? FrameSizeItems[idx]
                : FrameSizeItems[0];
        }
        set
        {
            var idx = Array.IndexOf(FrameSizeItems, value);
            _vm.DesiredFrameSizeIndex = idx >= 0 ? idx : 0;
        }
    }

    // ── UsingPlugins (maps to cbxVideoDecoder.SelectedIndex) ──────────

    /// <summary>Index decoder đã chọn (0=DirectShow, 1=VLC, 2=FFMPEG). Parity với WinForms.</summary>
    public int UsingPlugins
    {
        get => _vm.VideoDecoderIndex;
        set => _vm.VideoDecoderIndex = value is >= 0 and <= 2 ? value : 0;
    }

    // ── Pass-through properties (không bound UI — chỉ giữ state như WinForms) ──

    private string _login = string.Empty;
    /// <summary>Login (không dùng trong VideoFileSettingPage — giữ parity API với các SettingPage khác).</summary>
    public string Login { get => _login; set => _login = value; }

    private string _password = string.Empty;
    /// <summary>Password (không dùng trong VideoFileSettingPage — giữ parity API).</summary>
    public string Password { get => _password; set => _password = value; }

    private int _streamType = 1;
    /// <summary>Stream type (default 1 — parity với WinForms).</summary>
    public int StreamType { get => _streamType; set => _streamType = value; }

    private int _protocolType = 0;
    /// <summary>Protocol type (default 0 — parity với WinForms).</summary>
    public int ProtocolType { get => _protocolType; set => _protocolType = value; }

    private int _chanel = 1;
    /// <summary>Channel (default 1 — parity với WinForms).</summary>
    public int Chanel { get => _chanel; set => _chanel = value; }

    private string _quality = "Standard";
    /// <summary>Quality (default "Standard" — parity với WinForms).</summary>
    public string Quality { get => _quality; set => _quality = value; }

    private int _frameRate = 30;
    /// <summary>Frame rate (default 30 — parity với WinForms).</summary>
    public int FrameRate { get => _frameRate; set => _frameRate = value; }

    private bool _usingDeinterlaceFilter = false;
    /// <summary>Sử dụng deinterlace filter (default false — parity với WinForms).</summary>
    public bool UsingDeinterlaceFilter { get => _usingDeinterlaceFilter; set => _usingDeinterlaceFilter = value; }

    private string _deinterlaceFilterName = "Deinterlace Filter";
    /// <summary>Tên deinterlace filter (default "Deinterlace Filter" — parity với WinForms).</summary>
    public string DeinterlaceFilterName { get => _deinterlaceFilterName; set => _deinterlaceFilterName = value; }
}
