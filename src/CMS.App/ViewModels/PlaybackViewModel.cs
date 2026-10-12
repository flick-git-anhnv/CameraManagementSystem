using CMS.Core.Entities;
using CMS.Core.Interfaces;
using CMS.Infrastructure.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Globalization;

namespace CMS.App.ViewModels;

public partial class PlaybackViewModel : ObservableObject
{
    private static readonly double[] SupportedSpeeds = [0.5, 1.0, 2.0, 4.0];

    private readonly ICameraRepository _cameraRepo;
    private readonly IVideoSegmentRepository _segmentRepo;
    private bool _suppressAutoPlayOnSegmentChange;

    [ObservableProperty]
    private ObservableCollection<CameraDevice> _cameras = new();

    [ObservableProperty]
    private CameraDevice? _selectedCamera;

    [ObservableProperty]
    private DateTimeOffset _selectedDate = DateTimeOffset.Now;

    [ObservableProperty]
    private ObservableCollection<VideoSegment> _availableSegments = new();

    [ObservableProperty]
    private VideoSegment? _currentPlayingSegment;

    [ObservableProperty]
    private string _playbackStatus = "Chọn camera và ngày cần tra cứu bản ghi video.";

    [ObservableProperty]
    private bool _isPlaying = false;

    [ObservableProperty]
    private bool _hasActiveVideo = false;

    [ObservableProperty]
    private double _playbackSpeed = 1.0;

    [ObservableProperty]
    private string _speedLabel = "1.0x";

    [ObservableProperty]
    private string _currentTimeDisplay = "00:00:00";

    public string PlayPauseButtonText => IsPlaying ? "⏸️ Tạm dừng" : "▶️ Phát / Dừng";

    public event EventHandler<(VideoSegment Segment, double OffsetSeconds)>? SegmentPlaybackRequested;
    public event EventHandler<bool>? PlayPauseStateChanged;
    public event EventHandler<double>? RelativeSeekRequested;
    public event EventHandler<double>? PlaybackSpeedChanged;
    public event EventHandler? SnapshotRequested;

    public PlaybackViewModel(ICameraRepository? cameraRepo = null, IVideoSegmentRepository? segmentRepo = null)
    {
        _cameraRepo = cameraRepo ?? new SqliteCameraRepository();
        _segmentRepo = segmentRepo ?? new SqliteVideoSegmentRepository();

        _ = LoadCamerasAsync();
    }

    partial void OnIsPlayingChanged(bool value)
    {
        OnPropertyChanged(nameof(PlayPauseButtonText));
    }

    partial void OnCurrentPlayingSegmentChanged(VideoSegment? value)
    {
        if (_suppressAutoPlayOnSegmentChange) return;

        if (value != null)
        {
            HasActiveVideo = true;
            IsPlaying = true;
            CurrentTimeDisplay = value.StartTime.ToString("HH:mm:ss");
            PlaybackStatus = $"▶ Đang phát đoạn video: {value.StartTime:HH:mm:ss} - {value.EndTime:HH:mm:ss}";
            SegmentPlaybackRequested?.Invoke(this, (value, 0));
        }
        else
        {
            HasActiveVideo = false;
            IsPlaying = false;
        }
    }

    public void PlaySegmentAtOffset(VideoSegment segment, double offsetSeconds)
    {
        _suppressAutoPlayOnSegmentChange = true;
        CurrentPlayingSegment = segment;
        _suppressAutoPlayOnSegmentChange = false;

        HasActiveVideo = true;
        IsPlaying = true;
        DateTime targetTime = segment.StartTime.AddSeconds(Math.Max(0, offsetSeconds));
        CurrentTimeDisplay = targetTime.ToString("HH:mm:ss");
        PlaybackStatus = $"▶ Đang phát từ {targetTime:HH:mm:ss} ({segment.StartTime:HH:mm:ss} - {segment.EndTime:HH:mm:ss})";
        SegmentPlaybackRequested?.Invoke(this, (segment, Math.Max(0, offsetSeconds)));
    }

    public async Task LoadCamerasAsync()
    {
        var list = await _cameraRepo.GetAllAsync();
        Cameras.Clear();
        foreach (var cam in list)
        {
            Cameras.Add(cam);
        }

        if (SelectedCamera == null && Cameras.Count > 0)
        {
            SelectedCamera = Cameras[0];
            await SearchRecordingsAsync();
        }
    }

    [RelayCommand]
    public async Task SearchRecordingsAsync()
    {
        if (SelectedCamera == null)
        {
            PlaybackStatus = "Vui lòng chọn camera để tra cứu bản ghi.";
            return;
        }

        DateTime dayStart = SelectedDate.Date;
        DateTime dayEnd = dayStart.AddDays(1).AddSeconds(-1);

        PlaybackStatus = $"Đang tìm kiếm bản ghi cho {SelectedCamera.Name} ngày {dayStart:yyyy-MM-dd}...";

        var list = await _segmentRepo.GetSegmentsAsync(SelectedCamera.Id, dayStart, dayEnd);
        AvailableSegments.Clear();
        foreach (var s in list)
        {
            AvailableSegments.Add(s);
        }

        PlaybackStatus = list.Count > 0
            ? $"✅ Tìm thấy {list.Count} đoạn video ghi hình trong ngày {dayStart:yyyy-MM-dd}. Nhấn vào bản ghi để phát."
            : $"ℹ️ Không có bản ghi nào cho {SelectedCamera.Name} trong ngày {dayStart:yyyy-MM-dd}.";
    }

    [RelayCommand]
    public void TogglePlayPause()
    {
        if (CurrentPlayingSegment == null && AvailableSegments.Count > 0)
        {
            CurrentPlayingSegment = AvailableSegments[0];
            return;
        }

        IsPlaying = !IsPlaying;
        if (CurrentPlayingSegment != null)
        {
            PlaybackStatus = IsPlaying
                ? $"▶ Đang phát đoạn video: {CurrentPlayingSegment.StartTime:HH:mm:ss} - {CurrentPlayingSegment.EndTime:HH:mm:ss}"
                : $"⏸ Đã tạm dừng tại {CurrentTimeDisplay}";
            PlayPauseStateChanged?.Invoke(this, IsPlaying);
        }
    }

    [RelayCommand]
    public void StepBackward()
    {
        if (CurrentPlayingSegment == null) return;
        RelativeSeekRequested?.Invoke(this, -10.0);
    }

    [RelayCommand]
    public void StepForward()
    {
        if (CurrentPlayingSegment == null) return;
        RelativeSeekRequested?.Invoke(this, 10.0);
    }

    [RelayCommand]
    public void SlowDown()
    {
        int idx = Array.IndexOf(SupportedSpeeds, PlaybackSpeed);
        int nextIdx = idx > 0 ? idx - 1 : 0;
        SetSpeed(SupportedSpeeds[nextIdx]);
    }

    [RelayCommand]
    public void SpeedUp()
    {
        int idx = Array.IndexOf(SupportedSpeeds, PlaybackSpeed);
        int nextIdx = idx >= 0 && idx < SupportedSpeeds.Length - 1 ? idx + 1 : SupportedSpeeds.Length - 1;
        SetSpeed(SupportedSpeeds[nextIdx]);
    }

    private void SetSpeed(double speed)
    {
        PlaybackSpeed = speed;
        SpeedLabel = $"{speed.ToString("0.0", CultureInfo.InvariantCulture)}x";
        PlaybackSpeedChanged?.Invoke(this, speed);
    }

    [RelayCommand]
    public void TakeSnapshot()
    {
        if (CurrentPlayingSegment == null)
        {
            PlaybackStatus = "⚠️ Vui lòng chọn và phát một đoạn video trước khi chụp ảnh.";
            return;
        }
        SnapshotRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    public void ExportSegment()
    {
        if (CurrentPlayingSegment == null || string.IsNullOrWhiteSpace(CurrentPlayingSegment.FilePath))
        {
            PlaybackStatus = "⚠️ Vui lòng chọn một đoạn video trong danh sách để xuất file.";
            return;
        }

        try
        {
            if (!File.Exists(CurrentPlayingSegment.FilePath))
            {
                PlaybackStatus = $"❌ Không tìm thấy file gốc: {CurrentPlayingSegment.FilePath}";
                return;
            }

            string exportDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Exports");
            Directory.CreateDirectory(exportDir);

            string safeCamName = string.Join("_", (SelectedCamera?.Name ?? "Camera").Split(Path.GetInvalidFileNameChars()));
            string destFile = Path.Combine(
                exportDir,
                $"{safeCamName}_{CurrentPlayingSegment.StartTime:yyyyMMdd_HHmmss}_{CurrentPlayingSegment.EndTime:HHmmss}.mp4");

            File.Copy(CurrentPlayingSegment.FilePath, destFile, overwrite: true);
            PlaybackStatus = $"✅ Đã xuất video thành công: {destFile}";
        }
        catch (Exception ex)
        {
            PlaybackStatus = $"❌ Lỗi xuất video: {ex.Message}";
        }
    }

    public VideoSegment? FindSegmentAtTime(DateTime time)
    {
        return AvailableSegments.FirstOrDefault(s => s.StartTime <= time && s.EndTime >= time);
    }
}
