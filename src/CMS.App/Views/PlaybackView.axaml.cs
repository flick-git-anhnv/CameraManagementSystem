using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using CMS.App.ViewModels;
using CMS.Core.Entities;
using Kztek.Cameras;

namespace CMS.App.Views;

public partial class PlaybackView : UserControl
{
    private PlaybackViewModel? _boundVm;
    private DispatcherTimer? _playbackTimer;

    public PlaybackView()
    {
        InitializeComponent();

        Timeline.TimeSelected += OnTimelineTimeSelected;
        this.DataContextChanged += OnDataContextChanged;
        this.Unloaded += OnViewUnloaded;

        _playbackTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(250)
        };
        _playbackTimer.Tick += OnPlaybackTimerTick;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (_boundVm != null)
        {
            _boundVm.SegmentPlaybackRequested -= OnSegmentPlaybackRequested;
            _boundVm.PlayPauseStateChanged -= OnPlayPauseStateChanged;
            _boundVm.RelativeSeekRequested -= OnRelativeSeekRequested;
            _boundVm.PlaybackSpeedChanged -= OnPlaybackSpeedChanged;
            _boundVm.SnapshotRequested -= OnSnapshotRequested;
        }

        _boundVm = DataContext as PlaybackViewModel;
        if (_boundVm != null)
        {
            _boundVm.AvailableSegments.CollectionChanged += (_, _) =>
            {
                Timeline.CurrentDay = _boundVm.SelectedDate.Date;
                Timeline.SetSegments(_boundVm.AvailableSegments);
                if (_boundVm.AvailableSegments.Count > 0 && _boundVm.CurrentPlayingSegment == null)
                {
                    Timeline.CurrentTime = _boundVm.AvailableSegments[0].StartTime.TimeOfDay;
                }
            };

            _boundVm.SegmentPlaybackRequested += OnSegmentPlaybackRequested;
            _boundVm.PlayPauseStateChanged += OnPlayPauseStateChanged;
            _boundVm.RelativeSeekRequested += OnRelativeSeekRequested;
            _boundVm.PlaybackSpeedChanged += OnPlaybackSpeedChanged;
            _boundVm.SnapshotRequested += OnSnapshotRequested;

            Timeline.CurrentDay = _boundVm.SelectedDate.Date;
            Timeline.SetSegments(_boundVm.AvailableSegments);
        }
    }

    private void OnSegmentPlaybackRequested(object? sender, (VideoSegment Segment, double OffsetSeconds) args)
    {
        if (_boundVm == null) return;

        var seg = args.Segment;
        if (string.IsNullOrWhiteSpace(seg.FilePath) || !File.Exists(seg.FilePath))
        {
            _boundVm.HasActiveVideo = false;
            _boundVm.IsPlaying = false;
            _boundVm.PlaybackStatus = $"❌ Không tìm thấy file video trên ổ đĩa: {seg.FilePath}";
            return;
        }

        try
        {
            PlaybackPlayer.Stop();
            PlaybackPlayer.PlaybackSpeed = _boundVm.PlaybackSpeed;
            PlaybackPlayer.IsPaused = false;
            PlaybackPlayer.VideoSource = new CameraStreamInfo
            {
                Source = seg.FilePath
            };
            PlaybackPlayer.Start();

            if (args.OffsetSeconds > 0.5)
            {
                PlaybackPlayer.SeekSeconds(args.OffsetSeconds);
            }

            Timeline.CurrentDay = seg.StartTime.Date;
            Timeline.CurrentTime = seg.StartTime.TimeOfDay.Add(TimeSpan.FromSeconds(args.OffsetSeconds));
            _playbackTimer?.Start();
        }
        catch (Exception ex)
        {
            _boundVm.HasActiveVideo = false;
            _boundVm.IsPlaying = false;
            _boundVm.PlaybackStatus = $"❌ Lỗi phát file video: {ex.Message}";
        }
    }

    private void OnPlayPauseStateChanged(object? sender, bool isPlaying)
    {
        PlaybackPlayer.IsPaused = !isPlaying;
        if (isPlaying)
        {
            _playbackTimer?.Start();
        }
        else
        {
            _playbackTimer?.Stop();
        }
    }

    private void OnRelativeSeekRequested(object? sender, double deltaSeconds)
    {
        if (_boundVm?.CurrentPlayingSegment == null) return;

        var seg = _boundVm.CurrentPlayingSegment;
        double maxDuration = seg.DurationSeconds > 0 ? seg.DurationSeconds : Math.Max(0, PlaybackPlayer.DurationSeconds);
        double currentPos = PlaybackPlayer.CurrentPositionSeconds;
        double targetPos = Math.Clamp(currentPos + deltaSeconds, 0, Math.Max(0, maxDuration - 0.5));

        PlaybackPlayer.SeekSeconds(targetPos);
        DateTime targetClock = seg.StartTime.AddSeconds(targetPos);
        Timeline.CurrentTime = targetClock.TimeOfDay;
        _boundVm.CurrentTimeDisplay = targetClock.ToString("HH:mm:ss");
        _boundVm.PlaybackStatus = $"⏩ Đã tua đến {targetClock:HH:mm:ss} ({(deltaSeconds >= 0 ? "+" : "")}{deltaSeconds:F0}s)";
    }

    private void OnPlaybackSpeedChanged(object? sender, double speed)
    {
        PlaybackPlayer.PlaybackSpeed = speed;
        if (_boundVm != null)
        {
            _boundVm.PlaybackStatus = $"⚡ Tốc độ phát lại: {_boundVm.SpeedLabel}";
        }
    }

    private void OnSnapshotRequested(object? sender, EventArgs e)
    {
        if (_boundVm == null) return;

        try
        {
            var frame = PlaybackPlayer.GetCurrentVideoFrame();
            if (frame != null)
            {
                string folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Snapshots");
                Directory.CreateDirectory(folder);
                string safeCam = string.Join("_", (_boundVm.SelectedCamera?.Name ?? "Playback").Split(Path.GetInvalidFileNameChars()));
                string filename = Path.Combine(folder, $"Playback_{safeCam}_{DateTime.Now:yyyyMMdd_HHmmss}.jpg");
                using (var stream = File.OpenWrite(filename))
                {
                    frame.Save(stream);
                }
                frame.Dispose();
                _boundVm.PlaybackStatus = $"📸 Đã chụp ảnh khung hình lưu tại: {filename}";
            }
            else
            {
                _boundVm.PlaybackStatus = "⚠️ Chưa có khung hình giải mã để chụp ảnh.";
            }
        }
        catch (Exception ex)
        {
            _boundVm.PlaybackStatus = $"❌ Lỗi chụp ảnh: {ex.Message}";
        }
    }

    private void OnPlaybackTimerTick(object? sender, EventArgs e)
    {
        if (_boundVm?.CurrentPlayingSegment == null || !_boundVm.IsPlaying)
            return;

        var seg = _boundVm.CurrentPlayingSegment;
        double posSec = PlaybackPlayer.CurrentPositionSeconds;
        DateTime currentClock = seg.StartTime.AddSeconds(posSec);
        if (currentClock > seg.EndTime && seg.DurationSeconds > 1)
        {
            currentClock = seg.EndTime;
        }

        _boundVm.CurrentTimeDisplay = currentClock.ToString("HH:mm:ss");
        Timeline.CurrentTime = currentClock.TimeOfDay;
    }

    private void OnTimelineTimeSelected(object? sender, DateTime selectedTime)
    {
        if (_boundVm == null) return;

        var match = _boundVm.FindSegmentAtTime(selectedTime);

        // Nếu người dùng click gần vạch video ngắn trên thanh 24h (±5 phút), tự động bắt vào đoạn gần nhất
        if (match == null && _boundVm.AvailableSegments.Count > 0)
        {
            match = _boundVm.AvailableSegments
                .Select(s => new
                {
                    Segment = s,
                    Distance = Math.Min(
                        Math.Abs((selectedTime - s.StartTime).TotalSeconds),
                        Math.Abs((selectedTime - s.EndTime).TotalSeconds))
                })
                .Where(x => x.Distance <= 300)
                .OrderBy(x => x.Distance)
                .FirstOrDefault()?.Segment;
        }

        if (match != null)
        {
            double offsetSeconds = Math.Clamp((selectedTime - match.StartTime).TotalSeconds, 0, Math.Max(0, match.DurationSeconds - 0.5));
            _boundVm.PlaySegmentAtOffset(match, offsetSeconds);
        }
        else
        {
            _boundVm.PlaybackStatus = $"ℹ️ Không có bản ghi tại thời điểm {selectedTime:HH:mm:ss}.";
        }
    }

    private void OnViewUnloaded(object? sender, RoutedEventArgs e)
    {
        _playbackTimer?.Stop();
        try
        {
            PlaybackPlayer.Stop();
        }
        catch
        {
        }
    }
}
