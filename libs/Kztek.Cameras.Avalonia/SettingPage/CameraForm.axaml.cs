namespace Kztek.Cameras;

/// <summary>
/// Avalonia port của CameraForm (WinForms → Avalonia, task G5.13).
/// <br/>
/// Wizard 2 trang:
/// <list type="number">
///   <item><see cref="CameraDescriptionPage"/> — nhập tên camera + chọn loại.</item>
///   <item><see cref="CameraSettingPage"/>     — router IP/VideoCaptureDevice/VideoFile.</item>
/// </list>
/// Kế thừa <see cref="Wizard"/> — toàn bộ chrome điều hướng (Back/Next/Finish/Cancel,
/// descriptionLabel, workContent) được cung cấp bởi Wizard.axaml qua base constructor.
/// <br/>
/// Thay đổi so với WinForms:
/// <list type="bullet">
///   <item><c>this.Text</c> → <c>this.Title</c> (Avalonia Window API).</item>
///   <item><c>this.ActiveControl = page1</c> → bỏ: Wizard.OnWindowOpened gọi SetCurrentPage(0)
///         + Display() tự động sau khi window mở.</item>
///   <item>Kết quả: caller dùng <see cref="Wizard.ShowWizardAsync"/> và đọc
///         <see cref="Camera"/> property sau khi task trả về <c>true</c>.</item>
///   <item><c>page1.CameraType</c> (WinForms) → <c>_page1.CameraTypeIndex</c> (Avalonia):
///         CameraDescriptionPage expose <c>CameraTypeIndex</c> thay vì <c>CameraType</c>
///         để tránh đụng tên với enum <see cref="Kztek.Cameras.CameraType"/>.</item>
/// </list>
/// </summary>
public partial class CameraForm : Wizard
{
    // ── Pages ─────────────────────────────────────────────────────────────────────────────────

    private readonly CameraDescriptionPage _page1 = new CameraDescriptionPage();
    private readonly CameraSettingPage     _page2 = new CameraSettingPage();

    // ── State ─────────────────────────────────────────────────────────────────────────────────

    private Camera _camera = new Camera();

    // ── Constructor ───────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Khởi tạo CameraForm với camera mới (rỗng).
    /// Thêm 2 trang theo đúng thứ tự WinForms gốc.
    /// </summary>
    public CameraForm()
    {
        InitializeComponent();

        // Title — WinForms: this.Text = "Thiết lập Camera"
        this.Title = "Thiết lập Camera";

        // Thêm trang theo thứ tự WinForms: page1 (Description) trước page2 (Settings).
        // Wizard.AddPage subscribe StateChanged + Reset từ mỗi trang.
        this.AddPage(_page1);
        this.AddPage(_page2);

        // Propagate camera object ban đầu vào cả 2 trang.
        // Parity với WinForms constructor: page1.Camera = camera; page2.Camera = camera;
        _page1.Camera = _camera;
        _page2.Camera = _camera;

        // WinForms: this.ActiveControl = page1
        // Avalonia: bỏ — Wizard.OnWindowOpened → SetCurrentPage(0) → Display() tự xử lý.
    }

    // ── Camera property ───────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Camera đang được thiết lập.
    /// <br/>
    /// Set trước <see cref="Wizard.ShowWizardAsync"/> để pre-populate form với camera hiện có.
    /// Đọc sau khi ShowWizardAsync trả về <c>true</c> để lấy camera đã cập nhật.
    /// </summary>
    public Camera Camera
    {
        get => _camera;
        set
        {
            _camera       = value;
            _page1.Camera = _camera;
            _page2.Camera = _camera;
        }
    }

    // ── CheckCameraFunction property ──────────────────────────────────────────────────────────

    /// <summary>
    /// Delegate kiểm tra tên camera bị trùng.
    /// Parity với WinForms: <c>set { page1.CheckCameraFunction = value; }</c>.
    /// </summary>
    public CheckCameraHandler? CheckCameraFunction
    {
        set => _page1.CheckCameraFunction = value;
    }

    // ── Wizard virtual hooks ──────────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    /// <remarks>
    /// Khi chuyển sang trang 1 (CameraSettingPage): đồng bộ CameraType từ trang 0.
    /// <para>
    /// Parity: <c>page2.CameraType = page1.CameraType</c> (WinForms).
    /// Avalonia dùng <see cref="CameraDescriptionPage.CameraTypeIndex"/> (tên khác để tránh
    /// đụng với enum <see cref="Kztek.Cameras.CameraType"/>).
    /// </para>
    /// </remarks>
    protected override void OnPageChanging(int page)
    {
        if (page == 1)
        {
            // Switching to CameraSettingPage — sync loại camera từ CameraDescriptionPage.
            _page2.CameraType = _page1.CameraTypeIndex;
        }

        base.OnPageChanging(page);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Khi trang 0 (CameraDescriptionPage) reset: đặt lại CameraType trên trang 2 về -1.
    /// Parity: <c>page2.CameraType = -1</c> (WinForms OnResetOnPage).
    /// </remarks>
    protected override void OnResetOnPage(int page)
    {
        if (page == 0)
        {
            _page2.CameraType = -1;
        }
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Body rỗng — parity với WinForms OnFinish.
    /// Camera đã được cập nhật bởi CameraSettingPage.Apply() trước khi OnFinish được gọi.
    /// Caller đọc <see cref="Camera"/> property sau khi ShowWizardAsync trả về <c>true</c>.
    /// </remarks>
    protected override void OnFinish()
    {
        // empty — parity với WinForms
    }
}
