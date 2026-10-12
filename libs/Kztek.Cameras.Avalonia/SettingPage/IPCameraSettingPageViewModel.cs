using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Kztek.Cameras;

/// <summary>
/// ViewModel cho <see cref="IPCameraSettingPage"/> — Avalonia port của WinForms IPCameraSettingPage.
/// <br/>
/// Quản lý:
/// <list type="bullet">
///   <item><description>Trạng thái <see cref="Completed"/> + event <see cref="StateChanged"/> (parity WinForms).</description></item>
///   <item><description>Conditional enable: FrameRate/ImageQuality disable khi StreamType ∉ {0,1} (parity <c>cbxStreamType_SelectedIndexChanged</c>).</description></item>
///   <item><description>VideoDecoder auto-select khi StreamType thay đổi (parity WinForms).</description></item>
/// </list>
/// IpAddress được sync từ <c>KzIPTextbox.IPAddressChanged</c> event trong code-behind
/// (KHÔNG bind trực tiếp vì IPAddress của KzIPTextbox là CLR property, không phải AvaloniaProperty).
/// </summary>
public partial class IPCameraSettingPageViewModel : ObservableObject
{
    /// <summary>Fires khi <see cref="IpAddress"/> thay đổi — parity với WinForms StateChanged event.</summary>
    public event EventHandler? StateChanged;

    // ── IP / Login ────────────────────────────────────────────────────
    /// <summary>Địa chỉ IP camera — synced từ KzIPTextbox qua IPAddressChanged event.</summary>
    [ObservableProperty] private string _ipAddress = string.Empty;

    /// <summary>Tên đăng nhập — bound TwoWay tới KzTextBox.Text trong AXAML.</summary>
    [ObservableProperty] private string _login = string.Empty;

    // ── Ports ─────────────────────────────────────────────────────────
    /// <summary>HTTP port (default 80 — parity với WinForms numHttpPort.Value=80).</summary>
    [ObservableProperty] private decimal _httpPort = 80;

    /// <summary>Server port (default 0 — parity với WinForms numServerPort.Value=0).</summary>
    [ObservableProperty] private decimal _serverPort = 0;

    // ── ComboBox selections ───────────────────────────────────────────
    /// <summary>
    /// Stream type index (parity với cbxStreamType.SelectedIndex).
    /// Khởi tạo -1 (không chọn) — khớp WinForms (không set SelectedIndex trong constructor).
    /// </summary>
    [ObservableProperty] private int _streamTypeIndex = -1;

    /// <summary>Protocol index (parity với cbxProtocol.SelectedIndex). Default 0.</summary>
    [ObservableProperty] private int _protocolIndex = 0;

    /// <summary>Kênh video index (parity với cbxChanelVideo.SelectedIndex). Default 0.</summary>
    [ObservableProperty] private int _chanelVideoIndex = 0;

    /// <summary>Kênh ảnh index (parity với cbxChanelImg.SelectedIndex). Default 0.</summary>
    [ObservableProperty] private int _chanelImgIndex = 0;

    /// <summary>
    /// Độ phân giải (SelectedItem string — parity với cbxImageResolution.Text).
    /// Null = không chọn (parity WinForms: không set SelectedIndex trong constructor).
    /// </summary>
    [ObservableProperty] private string? _imageResolution;

    /// <summary>
    /// Chất lượng ảnh (SelectedItem string — parity với cbxImageQuality.Text).
    /// Null = không chọn.
    /// </summary>
    [ObservableProperty] private string? _imageQuality;

    /// <summary>
    /// Frame rate index (parity với cbxFrameRate.SelectedIndex).
    /// Khởi tạo -1 — khớp WinForms (không set SelectedIndex trong constructor).
    /// </summary>
    [ObservableProperty] private int _frameRateIndex = -1;

    /// <summary>Video decoder index (parity với cbxVideoDecoder.SelectedIndex). Default 0.</summary>
    [ObservableProperty] private int _videoDecoderIndex = 0;

    // ── Conditional enable (bound tới IsEnabled của FrameRate + ImageQuality ComboBox) ──
    /// <summary>
    /// True khi StreamTypeIndex == 0 (jpeg) hoặc 1 (mjpeg).
    /// Parity với: <c>cbxFrameRate.Enabled = cbxImageQuality.Enabled = (idx == 0 || idx == 1)</c>.
    /// Khởi tạo true — khớp WinForms default (control enabled trước khi user chọn).
    /// </summary>
    [ObservableProperty] private bool _isStreamDependentEnabled = true;

    // ── Derived ───────────────────────────────────────────────────────
    /// <summary>True khi IP đã nhập — parity với WinForms <c>completed = (txtVideoSource.Text.Trim() != "")</c>.</summary>
    public bool Completed => !string.IsNullOrWhiteSpace(IpAddress);

    // ── Partial callbacks ─────────────────────────────────────────────
    partial void OnIpAddressChanged(string value)
    {
        OnPropertyChanged(nameof(Completed));
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Parity với WinForms <c>cbxStreamType_SelectedIndexChanged</c>:
    /// <list type="bullet">
    ///   <item><description>FrameRate/ImageQuality enable = (idx == 0 || idx == 1).</description></item>
    ///   <item><description>idx == 0 → VideoDecoder = 0 (Default).</description></item>
    ///   <item><description>idx == 2 || idx == 3 → VideoDecoder = 1 (KztekSDK1).</description></item>
    /// </list>
    /// </summary>
    partial void OnStreamTypeIndexChanged(int value)
    {
        IsStreamDependentEnabled = value == 0 || value == 1;
        if (value == 0)
            VideoDecoderIndex = 0;
        else if (value == 2 || value == 3)
            VideoDecoderIndex = 1;
    }
}
