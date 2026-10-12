using System;
using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace Kztek.Cameras;

/// <summary>
/// Avalonia <see cref="Window"/> — port of WinForms <c>frmViewCamera</c> (F5, ADR §5.1). Task G5.15.
///
/// Hiển thị luồng video từ một <see cref="Camera"/> thông qua <see cref="AnvPlayer"/> nhúng trực tiếp.
/// <see cref="DispatcherTimer"/> 300ms cập nhật nhãn kích thước frame (<c>lblVideoSize</c>).
///
/// Key behavioral differences from WinForms:
/// <list type="bullet">
///   <item><c>pictureBox1</c> (PictureBox + System.Timers.Timer frame-grab) → <c>AnvPlayer</c>
///         (tự quản lý decode pipeline qua Channel consumer; timer chỉ cập nhật lblVideoSize).</item>
///   <item><c>System.Timers.Timer</c> (thread-pool, cần <c>this.Invoke()</c>) →
///         <c>DispatcherTimer</c> (UI thread; không cần Invoke — đã trên UI thread).</item>
///   <item>Timer stop: <c>Unloaded</c> event (KHÔNG chỉ <c>Closing</c>/<c>Closed</c>) để tránh
///         memory leak khi window hide-not-close. (lesson: avalonia-migration-review-checklist)</item>
///   <item><c>AnvPlayer.Stop()</c> gọi trong <c>Closing</c> (decoder thread dừng trước khi
///         window đóng); guard <c>_playerStarted</c> tránh double-stop.</item>
///   <item><c>GetCurrentVideoFrame()</c> (từ 2026-07-20, fix BUG-004 C2) trả về BẢN SAO <c>SKBitmap</c>
///         sở hữu riêng — PHẢI dispose sau khi dùng (khác hợp đồng CŨ trước đó là tham chiếu sống).</item>
/// </list>
///
/// [SupportedOSPlatform("windows")] đã gỡ (2026-07-14) theo yêu cầu — LƯU Ý: nhúng
/// <see cref="AnvPlayer"/> vốn compose <c>AnvPlayerService</c> dùng ReadPL SDK (nhận diện
/// biển số, chỉ chạy Windows) — gỡ attribute không làm window này chạy đúng trên Linux,
/// chỉ tắt cảnh báo CA1416.
/// </summary>
public partial class frmViewCamera : Window
{
    // ── State ────────────────────────────────────────────────────────────────

    private Camera? _currentCamera;
    private int _updateInterval = 300;

    /// <summary>
    /// DispatcherTimer cập nhật <c>lblVideoSize</c> định kỳ (mặc định 300ms).
    /// Fires trên UI thread — không cần <c>Invoke()</c>.
    /// Parity: <c>System.Timers.Timer timer</c> (WinForms).
    /// </summary>
    private readonly DispatcherTimer _timer = new();

    /// <summary>Guard tránh gọi <see cref="AnvPlayer.Stop"/> hai lần.</summary>
    private bool _playerStarted;

    private DateTime _lastFrameTime;
    private bool _isTimeout;

    // ── Properties (parity WinForms) ────────────────────────────────────────

    /// <summary>Camera hiển thị trong window. Set trước khi window mở.</summary>
    public Camera? CurrentCamera
    {
        get => _currentCamera;
        set => _currentCamera = value;
    }

    /// <summary>
    /// Interval (ms) của timer cập nhật frame info. Tối thiểu 300ms (parity WinForms).
    /// </summary>
    public int UpdateInterval
    {
        get => _updateInterval;
        set => _updateInterval = value;
    }

    // ── Constructor ──────────────────────────────────────────────────────────

    public frmViewCamera()
    {
        InitializeComponent();

        // Loaded: khởi động player + timer — parity WinForms frmViewCamera_Load.
        this.Loaded += OnWindowLoaded;

        // Unloaded: dừng timer — BẮT BUỘC per lesson avalonia-migration-review-checklist.
        // Unloaded fires khi visual tree bị detach (bao gồm cả khi window close).
        this.Unloaded += OnWindowUnloaded;

        // Closing: dừng AnvPlayer decoder thread — parity WinForms frmViewCamera_FormClosing.
        this.Closing += OnWindowClosing;
    }

    // ── Window lifecycle ─────────────────────────────────────────────────────

    /// <summary>
    /// Parity: <c>frmViewCamera_Load</c> — cấu hình timer, hiển thị URL camera, khởi động player.
    /// </summary>
    private void OnWindowLoaded(object? sender, RoutedEventArgs e)
    {
        // 1. Cấu hình DispatcherTimer (interval tối thiểu 300ms — parity WinForms).
        int interval = _updateInterval > 300 ? _updateInterval : 300;
        _timer.Interval = TimeSpan.FromMilliseconds(interval);
        _timer.Tick += OnTimerTick;

        // 2. Build URL RTSP trực tiếp thay vì phụ thuộc vào việc camera đã Start()
        if (_currentCamera != null && CameraRtspUrlBuilderFactory.TryBuild(_currentCamera.CameraType, _currentCamera, out var buildResult))
        {
            txtIP.Text = buildResult.Url;
            _player.VideoSource = new CameraStreamInfo { Source = buildResult.Url };
            _player.Start();
            _playerStarted = true;
            _lastFrameTime = DateTime.Now;
        }

        // 3. Start timer cập nhật lblVideoSize và Watchdog.
        _timer.Start();
    }

    /// <summary>
    /// Dừng timer khi window bị unload khỏi visual tree.
    /// BẮT BUỘC dùng <c>Unloaded</c> thay vì chỉ <c>Closing</c> — per lesson
    /// <c>avalonia-migration-review-checklist.md</c>: timer tiếp tục fire nếu window bị hide-not-close.
    /// </summary>
    private void OnWindowUnloaded(object? sender, RoutedEventArgs e)
    {
        _timer.Stop();
        _timer.Tick -= OnTimerTick;
    }

    /// <summary>
    /// Dừng AnvPlayer decoder thread trước khi window đóng.
    /// Parity: <c>frmViewCamera_FormClosing</c> — <c>timer.Stop() + timer.Dispose()</c>.
    /// Belt-and-suspenders: cũng dừng timer ở đây (Unloaded fires after Closing).
    /// </summary>
    private void OnWindowClosing(object? sender, WindowClosingEventArgs e)
    {
        // Dừng AnvPlayer (giải phóng decoder thread / network connection).
        if (_playerStarted)
        {
            _player.Stop();
            _playerStarted = false;
        }

        // Belt-and-suspenders: stop timer (Unloaded sẽ fire sau, nhưng stop sớm là an toàn).
        _timer.Stop();
    }

    // ── Timer tick ───────────────────────────────────────────────────────────

    /// <summary>
    /// Cập nhật <c>lblVideoSize</c> với kích thước frame hiện tại.
    /// DispatcherTimer fires trên UI thread — không cần <c>Invoke()</c>.
    ///
    /// Parity với WinForms <c>timer_Tick</c>:
    ///   - Kiểm tra <c>IsRunning</c> → lấy frame reference → đọc Width/Height.
    ///   - AnvPlayer tự render frame — không cần set <c>pictureBox1.Image</c>.
    ///
    /// Note (cập nhật 2026-07-20, "Start bị gọi nhiều lần/RAM tăng"): dùng
    /// <c>GetCurrentFrameSize()</c> thay <c>GetCurrentVideoFrame()</c> — chỉ đọc width/height từ AVFrame
    /// gốc, KHÔNG decode/convert pixel (không sws_scale, không rent buffer) mỗi 300ms chỉ để hiển thị
    /// label. Xem docs/bugs/BUG-004-mainshell-freeze-after-login.md.
    /// </summary>
    private void OnTimerTick(object? sender, EventArgs e)
    {
        try
        {
            if (_playerStarted && !_isTimeout)
            {
                var size = _player.GetCurrentFrameSize();
                if (size.HasValue)
                {
                    lblVideoSize.Text = $"Video Size: {size.Value.Width}x{size.Value.Height}";
                    _lastFrameTime = DateTime.Now;
                }
                else
                {
                    lblVideoSize.Text = string.Empty;
                    // Watchdog: Nếu không có frame nào trong 8s, stop player để tránh memory leak từ FFmpeg reconnect loop (G007)
                    if ((DateTime.Now - _lastFrameTime).TotalSeconds > 8)
                    {
                        _isTimeout = true;
                        _player.Stop();
                        _playerStarted = false;
                        txtIP.Text = "Camera offline/unreachable. Stopped.";
                    }
                }
            }
            else if (!_isTimeout)
            {
                lblVideoSize.Text = string.Empty;
            }
        }
        catch (Exception)
        {
            // Swallow — parity với WinForms timer_Tick (empty catch block).
        }
    }

    // ── Button handlers ──────────────────────────────────────────────────────

    /// <summary>
    /// Mở webview của camera trong trình duyệt mặc định.
    /// Parity: <c>btnOpenWebview_Click</c> — <c>Process.Start(http://camera.VideoSource)</c>.
    /// </summary>
    private void BtnOpenWebview_Click(object? sender, RoutedEventArgs e)
    {
        if (_currentCamera?.VideoSource is string videoSource && !string.IsNullOrEmpty(videoSource))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "http://" + videoSource,
                UseShellExecute = true
            });
        }
    }
}
