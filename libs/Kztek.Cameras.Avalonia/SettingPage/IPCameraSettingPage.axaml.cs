using System;
using Avalonia.Controls;

namespace Kztek.Cameras;

/// <summary>
/// Avalonia port của <c>IPCameraSettingPage</c> (WinForms → Avalonia, task G5.7).
/// <br/>
/// Nhập thông số IP camera: địa chỉ IP, tên đăng nhập, mật khẩu, protocol,
/// kiểu hình ảnh (stream type), port HTTP/Server và các ComboBox phụ.
/// <br/>
/// Thay đổi chính so với WinForms:
/// <list type="bullet">
///   <item><description>
///     <c>KzIPTextbox</c> thay <c>TextBox txtVideoSource</c> — sync qua <c>IPAddressChanged</c>
///     event (không TwoWay bind vì <c>IPAddress</c> là CLR property, không AvaloniaProperty).
///   </description></item>
///   <item><description>
///     <c>KzPasswordTextBox</c> thay <c>TextBox + UseSystemPasswordChar=true</c> — đọc/ghi
///     <c>Text</c> trực tiếp từ code-behind (không có <c>TextChanged</c> event trong Avalonia version).
///   </description></item>
///   <item><description>
///     <c>NumericUpDown.Value</c> là <c>decimal?</c> → cast <c>(int)</c> khi expose qua public API.
///   </description></item>
///   <item><description>
///     FrameRate/ImageQuality enable logic được handle trong <see cref="IPCameraSettingPageViewModel"/>
///     thay vì event handler trực tiếp.
///   </description></item>
/// </list>
/// Public API surface giữ nguyên để consumer dùng tương tự bản WinForms.
/// </summary>
public partial class IPCameraSettingPage : UserControl
{
    private readonly IPCameraSettingPageViewModel _vm;

    // ── Static item lists (dùng bởi AXAML via x:Static) ──────────────

    /// <summary>Danh sách stream type (parity với cbxStreamType.Items trong WinForms).</summary>
    public static readonly string[] StreamTypeItems = { "jpeg", "mjpeg", "mpeg4", "h264", "h265" };

    /// <summary>Danh sách protocol (parity với cbxProtocol.Items trong WinForms).</summary>
    public static readonly string[] ProtocolItems = { "HTTP", "RTSP", "RTP", "TCP", "UDP" };

    /// <summary>
    /// Danh sách kênh 0–32 (parity với cbxChanelVideo.Items + cbxChanelImg.Items trong WinForms).
    /// Dùng chung cho cả 2 ComboBox Kênh Video và Kênh Img.
    /// </summary>
    public static readonly string[] ChanelItems = BuildChanelItems();

    /// <summary>Danh sách độ phân giải (parity với cbxImageResolution.Items trong WinForms).</summary>
    public static readonly string[] ImageResolutionItems =
    {
        "192x144",  "320x180",  "320x240",  "640x360",  "640x480",
        "800x600",  "960x720",  "1024x768", "1280x720", "1280x960",
        "1280x1024","1600x1200","1920x1080","1920x1200","2048x1536",
        "2560x1600","2592x1944","2944x1920","3648x2752"
    };

    /// <summary>Danh sách chất lượng ảnh (parity với cbxImageQuality.Items trong WinForms).</summary>
    public static readonly string[] ImageQualityItems = { "Clarity", "Standard", "Motion" };

    /// <summary>
    /// Danh sách frame rate: "Uncontrolled", "1"–"30"
    /// (parity với cbxFrameRate.Items trong WinForms).
    /// </summary>
    public static readonly string[] FrameRateItems = BuildFrameRateItems();

    /// <summary>Danh sách video decoder (parity với cbxVideoDecoder.Items trong WinForms).</summary>
    public static readonly string[] VideoDecoderItems =
    {
        "Default ( Manufacture)", "KztekSDK1", "KztekSDK2", "KztekSDK3"
    };

    // ── Events ────────────────────────────────────────────────────────

    /// <summary>Fires khi IP address thay đổi — parity với WinForms <c>StateChanged</c> event.</summary>
    public event EventHandler? StateChanged;

    // ── Constructor ───────────────────────────────────────────────────

    public IPCameraSettingPage()
    {
        _vm = new IPCameraSettingPageViewModel();
        // Proxy StateChanged từ VM ra ngoài (consumer subscribe event trên UserControl)
        _vm.StateChanged += (_, e) => StateChanged?.Invoke(this, e);
        DataContext = _vm;
        InitializeComponent();

        // Wire KzIPTextbox event thủ công — IPAddress là CLR property nên TwoWay binding không sync
        // ngược từ control về VM. Event handler này đảm bảo _vm.IpAddress luôn current.
        _txtIP.IPAddressChanged += OnIPAddressChanged;

        // Behavior parity constructor: cbxChanelVideo/Protocol/ChanelImg SelectedIndex = 0
        // (đã set default 0 trong ViewModel — không cần thêm code ở đây)
    }

    // ── KzIPTextbox event handler ─────────────────────────────────────

    private void OnIPAddressChanged(object? sender, EventArgs e)
    {
        // Sync KzIPTextbox → ViewModel (không thể dùng TwoWay binding)
        _vm.IpAddress = _txtIP.IPAddress;
    }

    // ── IWizardPage-compatible surface ────────────────────────────────

    /// <summary>True khi IP đã nhập — parity với WinForms <c>Completed</c> property.</summary>
    public bool Completed => _vm.Completed;

    /// <summary>
    /// Focus IP textbox — parity với WinForms:
    /// <c>txtVideoSource.Focus(); txtVideoSource.SelectionStart = txtVideoSource.TextLength;</c>
    /// </summary>
    public void Display() => _txtIP.Focus();

    /// <summary>Apply — không có validation; luôn trả true (parity với WinForms).</summary>
    public bool Apply() => true;

    // ── VideoSource property ──────────────────────────────────────────

    /// <summary>
    /// Địa chỉ IP + port(s). Parity với WinForms <c>VideoSource</c> property.
    /// <br/>
    /// Getter: ghép IP:HttpPort:ServerPort (bỏ port nếu = 0).
    /// Setter: parse IP, HttpPort, ServerPort từ chuỗi <c>"ip:port1:port2"</c>.
    /// </summary>
    public string VideoSource
    {
        get
        {
            string ret = _vm.IpAddress;
            if (_vm.HttpPort != 0) ret += ":" + (int)_vm.HttpPort;
            if (_vm.ServerPort != 0) ret += ":" + (int)_vm.ServerPort;
            return ret;
        }
        set
        {
            string[] s = value.Split(':');
            if (s.Length <= 1)
            {
                // Chỉ có IP, không có port
                _txtIP.IPAddress = value;
                _vm.IpAddress = value;
            }
            else if (s.Length == 2)
            {
                _txtIP.IPAddress = s[0];
                _vm.IpAddress = s[0];
                if (int.TryParse(s[1], out int httpPort))
                    _vm.HttpPort = httpPort;
            }
            else if (s.Length == 3)
            {
                _txtIP.IPAddress = s[0];
                _vm.IpAddress = s[0];
                if (int.TryParse(s[1], out int httpPort))
                    _vm.HttpPort = httpPort;
                if (int.TryParse(s[2], out int serverPort))
                    _vm.ServerPort = serverPort;
            }
        }
    }

    // ── Primary data properties (delegate to ViewModel) ───────────────

    /// <summary>Tên đăng nhập (TwoWay bound tới KzTextBox qua ViewModel).</summary>
    public string Login
    {
        get => _vm.Login;
        set => _vm.Login = value;
    }

    /// <summary>
    /// Mật khẩu — đọc/ghi trực tiếp từ KzPasswordTextBox.Text (không qua ViewModel).
    /// Lý do: KzPasswordTextBox Avalonia KHÔNG có TextChanged event → không thể reactive sync.
    /// Text luôn current vì đọc tại thời điểm consumer gọi getter.
    /// </summary>
    public string Password
    {
        get => _txtPassword.Text ?? string.Empty;
        set => _txtPassword.Text = value;
    }

    /// <summary>Stream type index (parity với cbxStreamType.SelectedIndex).</summary>
    public int StreamType
    {
        get => _vm.StreamTypeIndex;
        set => _vm.StreamTypeIndex = value;
    }

    /// <summary>Protocol type index (parity với cbxProtocol.SelectedIndex).</summary>
    public int ProtocolType
    {
        get => _vm.ProtocolIndex;
        set => _vm.ProtocolIndex = value;
    }

    /// <summary>Kênh video index (parity với cbxChanelVideo.SelectedIndex).</summary>
    public int Chanel
    {
        get => _vm.ChanelVideoIndex;
        set => _vm.ChanelVideoIndex = value;
    }

    /// <summary>Kênh ảnh index (parity với cbxChanelImg.SelectedIndex).</summary>
    public int ChanelImg
    {
        get => _vm.ChanelImgIndex;
        set => _vm.ChanelImgIndex = value;
    }

    /// <summary>
    /// Độ phân giải (string — parity với WinForms <c>cbxImageResolution.Text</c> get/set).
    /// Getter trả empty string khi chưa chọn.
    /// </summary>
    public string Resolution
    {
        get => _vm.ImageResolution ?? string.Empty;
        set => _vm.ImageResolution = value;
    }

    /// <summary>
    /// Chất lượng ảnh (string — parity với WinForms <c>cbxImageQuality.Text</c> get/set).
    /// Getter trả empty string khi chưa chọn.
    /// </summary>
    public string Quality
    {
        get => _vm.ImageQuality ?? string.Empty;
        set => _vm.ImageQuality = value;
    }

    /// <summary>Frame rate index (parity với cbxFrameRate.SelectedIndex).</summary>
    public int FrameRate
    {
        get => _vm.FrameRateIndex;
        set => _vm.FrameRateIndex = value;
    }

    /// <summary>
    /// Video decoder index (parity với WinForms <c>UsingPlugins</c> setter constraint:
    /// <c>if (value &lt;= 4) SelectedIndex = value; else SelectedIndex = 0</c>).
    /// </summary>
    public int UsingPlugins
    {
        get => _vm.VideoDecoderIndex;
        set => _vm.VideoDecoderIndex = value <= 4 ? value : 0;
    }

    /// <summary>HTTP port (parity với numHttpPort.Value).</summary>
    public int HttpPort
    {
        get => (int)_vm.HttpPort;
        set => _vm.HttpPort = value;
    }

    /// <summary>Server port (parity với numServerPort.Value).</summary>
    public int ServerPort
    {
        get => (int)_vm.ServerPort;
        set => _vm.ServerPort = value;
    }

    // ── Pass-through fields (không có UI binding — giữ API parity với WinForms) ──

    private bool _usingDeinterlaceFilter = false;

    /// <summary>
    /// Sử dụng deinterlace filter (parity với WinForms field — không có UI control tương ứng trong trang này).
    /// </summary>
    public bool UsingDeinterlaceFilter
    {
        get => _usingDeinterlaceFilter;
        set => _usingDeinterlaceFilter = value;
    }

    private string _deinterlaceFilterName = "Deinterlace Filter";

    /// <summary>
    /// Tên deinterlace filter (parity với WinForms field — không có UI control tương ứng trong trang này).
    /// </summary>
    public string DeinterlaceFilterName
    {
        get => _deinterlaceFilterName;
        set => _deinterlaceFilterName = value;
    }

    // ── Private helper methods ────────────────────────────────────────

    private static string[] BuildChanelItems()
    {
        // Parity với WinForms: items "0", "1", ..., "32" (33 items total)
        var items = new string[33];
        for (int i = 0; i <= 32; i++) items[i] = i.ToString();
        return items;
    }

    private static string[] BuildFrameRateItems()
    {
        // Parity với WinForms: "Uncontrolled", "1", "2", ..., "30" (31 items total)
        var items = new string[31];
        items[0] = "Uncontrolled";
        for (int i = 1; i <= 30; i++) items[i] = i.ToString();
        return items;
    }
}
