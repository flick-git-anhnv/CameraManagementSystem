using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using CMS.Core.Entities;
using CMS.Core.Enums;
using CMS.Drivers;
using Kztek.Cameras;

namespace CMS.App.Controls;

public partial class VideoCellView : UserControl
{
    private CameraDevice? _camera;
    private bool _isUsingMainStream = false;
    private bool _isPlaying = false;

    public event EventHandler? DoubleClicked;
    public event EventHandler? Closed;

    public CameraDevice? CurrentCamera => _camera;
    public bool IsPlaying => _isPlaying;

    public VideoCellView()
    {
        InitializeComponent();

        BtnClose.Click += OnBtnCloseClick;
        BtnToggleStream.Click += OnBtnToggleStreamClick;
        BtnSnapshot.Click += OnBtnSnapshotClick;
        this.DoubleTapped += OnCellDoubleTapped;
        this.Unloaded += OnCellUnloaded;
    }

    public void PlayCamera(CameraDevice camera, bool useMainStream = false)
    {
        StopCamera();

        _camera = camera;
        _isUsingMainStream = useMainStream;
        TxtCameraName.Text = camera.Name;

        var driver = CameraDriverFactory.GetDriver(camera.Brand);
        var endpoints = driver.BuildStreamUrls(camera);

        string streamUrl = useMainStream ? endpoints.MainStreamUrl : endpoints.SubStreamUrl;
        TxtStreamType.Text = useMainStream ? "MAIN" : "SUB";
        StreamBadge.Background = useMainStream ? Brushes.ForestGreen : Brushes.RoyalBlue;

        try
        {
            Player.VideoSource = new CameraStreamInfo
            {
                Source = streamUrl,
                Login = camera.Username,
                Password = camera.Password
            };

            Player.Start();
            _isPlaying = true;

            PlaceholderGrid.IsVisible = false;
            StatusDot.Background = Brushes.LimeGreen;
        }
        catch (Exception ex)
        {
            TxtPlaceholder.Text = $"Error: {ex.Message}";
            PlaceholderGrid.IsVisible = true;
            StatusDot.Background = Brushes.Crimson;
        }
    }

    public void ToggleStreamResolution()
    {
        if (_camera == null || !_isPlaying) return;
        PlayCamera(_camera, !_isUsingMainStream);
    }

    public void StopCamera()
    {
        if (_isPlaying)
        {
            try
            {
                Player.Stop();
            }
            catch
            {
            }
            _isPlaying = false;
        }

        _camera = null;
        PlaceholderGrid.IsVisible = true;
        StatusDot.Background = Brushes.Crimson;
        TxtCameraName.Text = "Empty Slot";
    }

    private void OnBtnCloseClick(object? sender, RoutedEventArgs e)
    {
        StopCamera();
        Closed?.Invoke(this, EventArgs.Empty);
    }

    private void OnBtnToggleStreamClick(object? sender, RoutedEventArgs e)
    {
        ToggleStreamResolution();
    }

    private void OnBtnSnapshotClick(object? sender, RoutedEventArgs e)
    {
        if (!_isPlaying) return;
        // Chụp nhanh từ frame hiện tại của player
        try
        {
            var frame = Player.GetCurrentVideoFrame();
            if (frame != null)
            {
                string folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Snapshots");
                Directory.CreateDirectory(folder);
                string filename = Path.Combine(folder, $"{_camera?.Name ?? "Cam"}_{DateTime.Now:yyyyMMdd_HHmmss}.jpg");
                using var stream = File.OpenWrite(filename);
                frame.Save(stream);
            }
        }
        catch
        {
        }
    }

    private void OnCellDoubleTapped(object? sender, TappedEventArgs e)
    {
        DoubleClicked?.Invoke(this, EventArgs.Empty);
    }

    private void OnCellUnloaded(object? sender, RoutedEventArgs e)
    {
        StopCamera();
    }
}
