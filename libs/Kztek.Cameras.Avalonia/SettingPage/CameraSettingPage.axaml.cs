using System;
using Avalonia.Controls;

namespace Kztek.Cameras;

/// <summary>
/// Avalonia port của CameraSettingPage (WinForms → Avalonia, task G5.11).
/// <br/>
/// Router <see cref="UserControl"/> — chuyển đổi hiển thị giữa 3 sub-page tuỳ
/// <see cref="CameraType"/> được set từ bên ngoài (thường từ CameraForm qua CameraDescriptionPage):
/// <list type="bullet">
///   <item><see cref="Kztek.Cameras.CameraType.VideoFile"/>          → <see cref="VideoFileSettingPage"/></item>
///   <item><see cref="Kztek.Cameras.CameraType.VideoCaptureDevice"/> → <see cref="VideoCaptureDeviceSettingPage"/></item>
///   <item>Mọi loại khác (IP cameras)                                → <see cref="IPCameraSettingPage"/></item>
/// </list>
/// Implements <see cref="IWizardPage"/> bằng cách proxy Completed / Display() / Apply() / StateChanged
/// tới sub-page đang active.
/// <br/>
/// Thay đổi so với WinForms: logic router trong WinForms bị comment sẵn (sourcePage luôn null);
/// Avalonia port implement thật theo ý định thiết kế gốc, dùng <see cref="ContentControl"/> thay
/// Panel + Dock=Fill để switch sub-page — parity với Wizard.axaml.cs dùng workContent.Content.
/// </summary>
public partial class CameraSettingPage : UserControl, IWizardPage
{
    // ── Nội bộ: phân biệt sub-page đang active ───────────────────────

    private enum PageKind { None, IP, VC, VF }

    // ── State ─────────────────────────────────────────────────────────

    private Camera?  _camera;
    private int      _cameraType  = -1;
    private PageKind _activeKind  = PageKind.None;

    // Lazy-created sub-page instances — reuse across CameraType switches
    private IPCameraSettingPage?           _ipPage;
    private VideoCaptureDeviceSettingPage? _vcPage;
    private VideoFileSettingPage?          _vfPage;

    // ── IWizardPage events ────────────────────────────────────────────

    /// <inheritdoc/>
    public event EventHandler? StateChanged;

    /// <inheritdoc/>
    /// <remarks>
    /// Tồn tại theo <see cref="IWizardPage"/> contract.
    /// Không có trigger nội bộ trong implementation này — sub-page không fire Reset.
    /// Consumer (Wizard) subscribe nhưng event không được raise từ CameraSettingPage.
    /// </remarks>
#pragma warning disable CS0067 // IWizardPage contract — sub-page không fire Reset
    public event EventHandler? Reset;
#pragma warning restore CS0067

    // ── Constructor ───────────────────────────────────────────────────

    public CameraSettingPage()
    {
        InitializeComponent();
    }

    // ── Camera property ───────────────────────────────────────────────

    /// <summary>
    /// Camera đang được thiết lập.
    /// <br/>
    /// Setter lưu camera và populate sub-page đang active (nếu đã tạo).
    /// Parity với WinForms Camera setter (trước đây populate sourcePage từ camera).
    /// </summary>
    public Camera? Camera
    {
        get => _camera;
        set
        {
            if (value != null)
            {
                _camera = value;
                if (_activeKind != PageKind.None)
                    PopulateActivePage();
            }
        }
    }

    // ── CameraType router property ────────────────────────────────────

    /// <summary>
    /// Index loại camera — ánh xạ sang <see cref="CameraTypes.GetType(int)"/>.
    /// <br/>
    /// Setter: chuyển đổi sang sub-page tương ứng khi giá trị thay đổi.
    /// Parity với WinForms CameraType setter.
    /// </summary>
    public int CameraType
    {
        get => _cameraType;
        set
        {
            if (_cameraType == -1 || _cameraType != value)
            {
                _cameraType = value;
                if (_cameraType > -1)
                    SwitchSubPage(CameraTypes.GetType(_cameraType));
            }
        }
    }

    // ── IWizardPage implementation ────────────────────────────────────

    /// <inheritdoc/>
    public string PageName => "Thông số Camera";

    /// <inheritdoc/>
    public string PageDescription
    {
        get
        {
            var str = "2. Thiết lập thông số Camera\n";
            if (_cameraType > -1)
                str += "             " + CameraTypes.GetType(_cameraType).ToString();
            return str;
        }
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Proxy tới sub-page active; trả false khi chưa có sub-page (CameraType chưa được set).
    /// </remarks>
    public bool Completed => _activeKind switch
    {
        PageKind.IP => _ipPage?.Completed ?? false,
        PageKind.VC => _vcPage?.Completed ?? false,
        PageKind.VF => _vfPage?.Completed ?? false,
        _           => false
    };

    /// <inheritdoc/>
    /// <remarks>Proxy Display() tới sub-page active.</remarks>
    public void Display()
    {
        switch (_activeKind)
        {
            case PageKind.IP: _ipPage?.Display(); break;
            case PageKind.VC: _vcPage?.Display(); break;
            case PageKind.VF: _vfPage?.Display(); break;
        }
    }

    /// <summary>
    /// Validate và áp dụng thay đổi từ sub-page active vào Camera.
    /// <br/>
    /// Trả false khi: chưa có sub-page, Camera null, hoặc sub-page Apply() = false.
    /// Khi Apply() thành công — copy các field có trong Camera model từ sub-page về camera object.
    /// </summary>
    public bool Apply()
    {
        if (_camera == null || _activeKind == PageKind.None) return false;

        // Gọi Apply() trên sub-page active
        bool subResult = _activeKind switch
        {
            PageKind.IP => _ipPage?.Apply() ?? false,
            PageKind.VC => _vcPage?.Apply() ?? false,
            PageKind.VF => _vfPage?.Apply() ?? false,
            _           => false
        };

        if (!subResult) return false;

        // Copy dữ liệu từ sub-page về Camera (chỉ copy properties có trong Camera model)
        CopyToCamera();
        return true;
    }

    // ── Router helpers ────────────────────────────────────────────────

    private void SwitchSubPage(Kztek.Cameras.CameraType cameraType)
    {
        // Unsubscribe StateChanged từ page cũ
        UnsubscribeActivePage();

        // Xác định PageKind mới
        _activeKind = cameraType switch
        {
            Kztek.Cameras.CameraType.VideoFile          => PageKind.VF,
            Kztek.Cameras.CameraType.VideoCaptureDevice => PageKind.VC,
            _                                           => PageKind.IP
        };

        // Lazy-create sub-page nếu chưa có
        switch (_activeKind)
        {
            case PageKind.IP: _ipPage ??= new IPCameraSettingPage();           break;
            case PageKind.VC: _vcPage ??= new VideoCaptureDeviceSettingPage(); break;
            case PageKind.VF: _vfPage ??= new VideoFileSettingPage();          break;
        }

        // Subscribe StateChanged từ page mới
        SubscribeActivePage();

        // Populate sub-page với camera data nếu đã có camera
        if (_camera != null)
            PopulateActivePage();

        // Hiển thị sub-page trong ContentControl (parity WinForms: add control + Dock=Fill)
        pageContent.Content = (Control?)(_activeKind switch
        {
            PageKind.IP => (object?)_ipPage,
            PageKind.VC => (object?)_vcPage,
            PageKind.VF => (object?)_vfPage,
            _           => null
        });

        // Thông báo state changed — Completed có thể thay đổi sau khi switch sub-page
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    private void SubscribeActivePage()
    {
        switch (_activeKind)
        {
            case PageKind.IP: if (_ipPage != null) _ipPage.StateChanged += OnSubPageStateChanged; break;
            case PageKind.VC: if (_vcPage != null) _vcPage.StateChanged += OnSubPageStateChanged; break;
            case PageKind.VF: if (_vfPage != null) _vfPage.StateChanged += OnSubPageStateChanged; break;
        }
    }

    private void UnsubscribeActivePage()
    {
        switch (_activeKind)
        {
            case PageKind.IP: if (_ipPage != null) _ipPage.StateChanged -= OnSubPageStateChanged; break;
            case PageKind.VC: if (_vcPage != null) _vcPage.StateChanged -= OnSubPageStateChanged; break;
            case PageKind.VF: if (_vfPage != null) _vfPage.StateChanged -= OnSubPageStateChanged; break;
        }
    }

    /// <summary>
    /// Điền dữ liệu từ Camera vào sub-page đang active.
    /// Parity với WinForms Camera setter + CameraType setter populate sourcePage từ camera.
    /// Chỉ điền properties thực sự có trong Camera model.
    /// </summary>
    private void PopulateActivePage()
    {
        if (_camera == null) return;

        switch (_activeKind)
        {
            case PageKind.IP when _ipPage != null:
                _ipPage.VideoSource   = _camera.VideoSource;
                _ipPage.Login         = _camera.Login;
                _ipPage.Password      = _camera.Password;
                _ipPage.StreamType    = (int)_camera.StreamType;
                _ipPage.ProtocolType  = (int)_camera.ProtocolType;
                _ipPage.Chanel        = _camera.Chanel;
                _ipPage.Resolution    = _camera.Resolution;
                break;

            case PageKind.VC when _vcPage != null:
                _vcPage.VideoSource  = _camera.VideoSource;
                _vcPage.Login        = _camera.Login;
                _vcPage.Password     = _camera.Password;
                _vcPage.StreamType   = (int)_camera.StreamType;
                _vcPage.ProtocolType = (int)_camera.ProtocolType;
                _vcPage.Chanel       = _camera.Chanel;
                _vcPage.Resolution   = _camera.Resolution;
                break;

            case PageKind.VF when _vfPage != null:
                _vfPage.VideoSource  = _camera.VideoSource;
                _vfPage.Login        = _camera.Login;
                _vfPage.Password     = _camera.Password;
                _vfPage.StreamType   = (int)_camera.StreamType;
                _vfPage.ProtocolType = (int)_camera.ProtocolType;
                _vfPage.Chanel       = _camera.Chanel;
                _vfPage.Resolution   = _camera.Resolution;
                break;
        }
    }

    /// <summary>
    /// Copy dữ liệu từ sub-page active về Camera — gọi sau khi sub-page Apply() thành công.
    /// Chỉ copy properties thực sự có trong Camera model
    /// (FrameRate, Quality, UsingPlugins, UsingDeinterlaceFilter, DeinterlaceFilterName
    ///  không có trong Camera model — bỏ qua, parity với WinForms gốc đã comment sẵn).
    /// </summary>
    private void CopyToCamera()
    {
        if (_camera == null) return;

        switch (_activeKind)
        {
            case PageKind.IP when _ipPage != null:
                _camera.VideoSource  = _ipPage.VideoSource;
                _camera.Login        = _ipPage.Login;
                _camera.Password     = _ipPage.Password;
                _camera.StreamType   = (StreamType)_ipPage.StreamType;
                _camera.ProtocolType = (ProtocolType)_ipPage.ProtocolType;
                _camera.Chanel       = _ipPage.Chanel;
                _camera.Resolution   = _ipPage.Resolution;
                break;

            case PageKind.VC when _vcPage != null:
                _camera.VideoSource  = _vcPage.VideoSource;
                _camera.Login        = _vcPage.Login;
                _camera.Password     = _vcPage.Password;
                _camera.StreamType   = (StreamType)_vcPage.StreamType;
                _camera.ProtocolType = (ProtocolType)_vcPage.ProtocolType;
                _camera.Chanel       = _vcPage.Chanel;
                _camera.Resolution   = _vcPage.Resolution;
                break;

            case PageKind.VF when _vfPage != null:
                _camera.VideoSource  = _vfPage.VideoSource;
                _camera.Login        = _vfPage.Login;
                _camera.Password     = _vfPage.Password;
                _camera.StreamType   = (StreamType)_vfPage.StreamType;
                _camera.ProtocolType = (ProtocolType)_vfPage.ProtocolType;
                _camera.Chanel       = _vfPage.Chanel;
                _camera.Resolution   = _vfPage.Resolution;
                break;
        }
    }

    private void OnSubPageStateChanged(object? sender, EventArgs e)
        => StateChanged?.Invoke(this, e);
}
