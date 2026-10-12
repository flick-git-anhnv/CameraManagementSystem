using System;
using Avalonia.Controls;
using MsBox.Avalonia;
using MessageBox.Avalonia.Enums;    // ButtonEnum (legacy namespace trong 3.0.0-rc2)
using MsBox.Avalonia.Enums;         // Icon

namespace Kztek.Cameras;

/// <summary>
/// Delegate kiểm tra camera có bị trùng tên không.
/// Parity với WinForms <c>public delegate bool CheckCameraHandler(Camera camera)</c>.
/// </summary>
public delegate bool CheckCameraHandler(Camera camera);

/// <summary>
/// Avalonia port của CameraDescriptionPage (WinForms → Avalonia, task G5.10).
/// <br/>
/// Trang đầu của Wizard — cho phép nhập tên camera và chọn loại camera.
/// Implements <see cref="IWizardPage"/> — dùng lại IWizardPage.cs đã có sẵn trong project.
/// <br/>
/// Thay đổi chính so với WinForms:
/// <list type="bullet">
///   <item>TextBox/Label/ComboBox → KzTextBox, TextBlock, ComboBox Avalonia native (ADR §6.1)</item>
///   <item>txtName_TextChanged + cbxVideoSource_SelectedIndexChanged
///         → <see cref="CameraDescriptionPageViewModel"/> [ObservableProperty] + partial OnXxxChanged (ADR §6.2 E19)</item>
///   <item>MessageBox.Show() → MsBox.Avalonia async fire-and-forget (IWizardPage.Apply() là sync — không thể await)</item>
///   <item>txtName.BackColor = LightCoral → bỏ (UX minor — Apply() trả false là đủ signal cho Wizard)</item>
/// </list>
/// </summary>
public partial class CameraDescriptionPage : UserControl, IWizardPage
{
    private readonly CameraDescriptionPageViewModel _vm;

    // ── Static item list (dùng bởi AXAML via x:Static) ────────────────────

    /// <summary>
    /// Danh sách loại camera (parity với cbxVideoSource.Items trong WinForms Designer).
    /// Index khớp trực tiếp với <see cref="Kztek.Cameras.CameraType"/> enum.
    /// </summary>
    public static readonly string[] VideoSourceItems =
    {
        "Video File",
        "Video Capture Device",
        "HIKVISION network camera / DVR / NVR",
        "ENSTER network camera",
        "HANSE network camera",
        "TIANDY network camera",
        "DAHUA network camera",
        "HIKVISION 2 network camera"
    };

    // ── Events (IWizardPage) ────────────────────────────────────────────────

    /// <inheritdoc/>
    public event EventHandler? StateChanged;

    /// <inheritdoc/>
    public event EventHandler? Reset;

    // ── Constructor ────────────────────────────────────────────────────────

    public CameraDescriptionPage()
    {
        _vm = new CameraDescriptionPageViewModel();

        // Proxy events từ ViewModel ra ngoài (consumer subscribe trên UserControl)
        _vm.StateChanged += (_, e) => StateChanged?.Invoke(this, e);
        _vm.Reset        += (_, e) => Reset?.Invoke(this, e);

        DataContext = _vm;
        InitializeComponent();

        // Parity: cbxVideoSource.SelectedIndex = 0 trong WinForms constructor
        _vm.VideoSourceIndex = 0;
    }

    // ── Camera property ─────────────────────────────────────────────────────

    private Camera? _camera;

    /// <summary>
    /// Camera đang chỉnh sửa.
    /// Setter điền dữ liệu vào form (parity với WinForms Camera property setter:
    /// txtName.Text, txtDescription.Text, cbxVideoSource.SelectedIndex).
    /// </summary>
    public Camera? Camera
    {
        get => _camera;
        set
        {
            if (value != null)
            {
                _camera = value;
                _vm.Name            = _camera.Name;
                _vm.Description     = _camera.Description;
                _vm.VideoSourceIndex = (int)_camera.CameraType;
            }
        }
    }

    // ── CheckCameraFunction property ────────────────────────────────────────

    private CheckCameraHandler? _checkCameraFunction;

    /// <summary>
    /// Delegate kiểm tra tên camera bị trùng — set bởi caller (CameraForm).
    /// Parity với WinForms CheckCameraFunction property.
    /// </summary>
    public CheckCameraHandler? CheckCameraFunction
    {
        set => _checkCameraFunction = value;
    }

    // ── IWizardPage implementation ──────────────────────────────────────────

    /// <inheritdoc/>
    public string PageName => "Tên và chủng loại Camera";

    /// <inheritdoc/>
    public string PageDescription => "1. Chọn loại Camera - Nhập tên Camera";

    /// <inheritdoc/>
    public bool Completed => _vm.Completed;

    /// <inheritdoc/>
    /// <remarks>
    /// Parity với WinForms: focus vào txtName.
    /// Avalonia UserControl.Focus() đặt focus lên control đầu tiên có thể focus.
    /// </remarks>
    public void Display() => Focus();

    /// <summary>
    /// Validate và cập nhật camera. Trả về false nếu validation thất bại.
    /// <br/>
    /// Nếu tên camera bị trùng → hiển thị MessageBox (async fire-and-forget do
    /// <see cref="IWizardPage.Apply"/> là synchronous) rồi trả về false.
    /// <br/>
    /// Khác WinForms: không set txtName.BackColor = LightCoral (Avalonia không có BackColor API
    /// trực tiếp; trả false đã đủ để Wizard giữ nguyên trang — behavior parity ở mức chức năng).
    /// </summary>
    public bool Apply()
    {
        if (_camera == null) return false;

        string name = _vm.Name.Trim();

        if (_checkCameraFunction != null)
        {
            var tmpCamera = new Camera();
            tmpCamera.ID   = _camera.ID;
            tmpCamera.Name = name;

            if (!_checkCameraFunction(tmpCamera))
            {
                // MsBox.Avalonia async — fire-and-forget vì Apply() là sync theo IWizardPage contract.
                // Dialog vẫn hiển thị bình thường; chỉ khác là caller không block chờ user đóng dialog.
                _ = MessageBoxManager
                    .GetMessageBoxStandard("Thông báo", "Đã tồn tại Camera với tên này!", ButtonEnum.Ok, Icon.Stop)
                    .ShowAsync();
                return false;
            }
        }

        _camera.Name        = name;
        _camera.Description = _vm.Description;
        _camera.CameraType  = (Kztek.Cameras.CameraType)_vm.VideoSourceIndex;

        return true;
    }

    // ── Additional public API (parity bản WinForms) ─────────────────────────

    /// <summary>
    /// Loại camera đang được chọn (SelectedIndex của cbxVideoSource).
    /// Parity với WinForms <c>public int CameraType { get; }</c>.
    /// </summary>
    public int CameraTypeIndex => _vm.VideoSourceIndex;
}
