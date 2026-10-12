using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using CMS.Core.Entities;
using CMS.Drivers;
using CMS.MediaServer.Storage;
using Kztek.Cameras;

namespace CMS.App.Controls;

public partial class VideoCellView : UserControl
{
    private CameraDevice? _camera;
    private bool _isUsingMainStream = false;
    private bool _isPlaying = false;
    private bool _isRecording = false;
    private static StorageManager? _storageManager;

    public static StorageManager GlobalStorageManager => _storageManager ??= new StorageManager();

    public event EventHandler? DoubleClicked;
    public event EventHandler? Closed;

    public CameraDevice? CurrentCamera => _camera;
    public bool IsPlaying => _isPlaying;
    public bool IsRecording => _isRecording;

    public VideoCellView()
    {
        InitializeComponent();

        BtnClose.Click += OnBtnCloseClick;
        BtnToggleStream.Click += OnBtnToggleStreamClick;
        BtnSnapshot.Click += OnBtnSnapshotClick;
        BtnRecord.Click += OnBtnRecordClick;
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

            // Kiểm tra trạng thái ghi hình
            UpdateRecordingState(GlobalStorageManager.IsRecording(camera.Id));
        }
        catch (Exception ex)
        {
            TxtPlaceholder.Text = $"Error: {ex.Message}";
            PlaceholderGrid.IsVisible = true;
            StatusDot.Background = Brushes.Crimson;
        }
    }

    public async void ToggleRecording()
    {
        if (_camera == null) return;

        if (_isRecording)
        {
            await GlobalStorageManager.StopRecordingAsync(_camera.Id);
            UpdateRecordingState(false);
        }
        else
        {
            bool ok = await GlobalStorageManager.StartRecordingAsync(_camera);
            UpdateRecordingState(ok);
        }
    }

    private void UpdateRecordingState(bool recording)
    {
        _isRecording = recording;
        RecBadge.IsVisible = recording;
        BtnRecord.Background = recording ? Brushes.Crimson : Brush.Parse("#3F3F46");
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
        UpdateRecordingState(false);
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

    private void OnBtnRecordClick(object? sender, RoutedEventArgs e)
    {
        ToggleRecording();
    }

    private void OnBtnSnapshotClick(object? sender, RoutedEventArgs e)
    {
        if (!_isPlaying) return;
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
