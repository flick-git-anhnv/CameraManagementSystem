using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Media;
using CMS.Core.Entities;
using CMS.Core.Enums;

namespace CMS.App.Controls;

public partial class TimelineControl : UserControl
{
    private DateTime _currentDay = DateTime.Today;
    private TimeSpan _currentTime = TimeSpan.FromHours(12);
    private List<VideoSegment> _segments = new();
    private bool _isDragging = false;

    public event EventHandler<DateTime>? TimeSelected;

    public DateTime CurrentDay
    {
        get => _currentDay;
        set
        {
            _currentDay = value.Date;
            RedrawTimeline();
        }
    }

    public TimeSpan CurrentTime
    {
        get => _currentTime;
        set
        {
            _currentTime = value;
            TxtCurrentTime.Text = _currentTime.ToString(@"hh\:mm\:ss");
            UpdatePlayheadPosition();
        }
    }

    public TimelineControl()
    {
        InitializeComponent();

        TimelineCanvas.PointerPressed += OnCanvasPointerPressed;
        TimelineCanvas.PointerMoved += OnCanvasPointerMoved;
        TimelineCanvas.PointerReleased += OnCanvasPointerReleased;
        this.SizeChanged += (_, _) => RedrawTimeline();
        this.ActualThemeVariantChanged += (_, _) => RedrawTimeline();
        CMS.App.Services.ThemeService.Instance.ThemeChanged += (_, _) => RedrawTimeline();
    }

    public void SetSegments(IEnumerable<VideoSegment> segments)
    {
        _segments = segments.ToList();
        RedrawTimeline();
    }

    public void RedrawTimeline()
    {
        TimelineCanvas.Children.Clear();
        double width = this.Bounds.Width;
        double height = this.Bounds.Height;

        if (width <= 0 || height <= 0) return;

        IBrush majorTickBrush = this.TryFindResource("ThemeTimelineTickMajorBrush", out var majorRes) && majorRes is IBrush mb ? mb : Brush.Parse("#52525B");
        IBrush minorTickBrush = this.TryFindResource("ThemeTimelineTickMinorBrush", out var minorRes) && minorRes is IBrush mnb ? mnb : Brush.Parse("#27272A");
        IBrush textBrush = this.TryFindResource("ThemeTimelineTextBrush", out var textRes) && textRes is IBrush tb ? tb : Brush.Parse("#71717A");

        // 1. Vẽ các vạch giờ (0h - 24h)
        for (int h = 0; h <= 24; h++)
        {
            double x = (h / 24.0) * width;

            var hourLine = new Line
            {
                StartPoint = new Point(x, 0),
                EndPoint = new Point(x, h % 6 == 0 ? height : height * 0.4),
                Stroke = h % 6 == 0 ? majorTickBrush : minorTickBrush,
                StrokeThickness = 1
            };
            TimelineCanvas.Children.Add(hourLine);

            if (h % 2 == 0 && h < 24)
            {
                var label = new TextBlock
                {
                    Text = $"{h:00}:00",
                    FontSize = 10,
                    Foreground = textBrush
                };
                Canvas.SetLeft(label, x + 3);
                Canvas.SetTop(label, 4);
                TimelineCanvas.Children.Add(label);
            }
        }

        // 2. Vẽ các đoạn video có sẵn
        DateTime dayStart = _currentDay.Date;
        DateTime dayEnd = dayStart.AddDays(1);
        double totalSeconds = 86400.0; // 24 giờ = 86400 giây

        foreach (var seg in _segments)
        {
            DateTime start = seg.StartTime < dayStart ? dayStart : seg.StartTime;
            DateTime end = seg.EndTime > dayEnd ? dayEnd : seg.EndTime;

            if (end <= start) continue;

            double startSec = (start - dayStart).TotalSeconds;
            double segSec = (end - start).TotalSeconds;

            double x = (startSec / totalSeconds) * width;
            double w = Math.Max(2, (segSec / totalSeconds) * width);

            IBrush fillBrush = seg.RecordingType switch
            {
                RecordingMode.MotionTriggered => Brush.Parse("#F59E0B"), // Vàng: Motion
                RecordingMode.AiTriggered => Brush.Parse("#EF4444"),     // Đỏ: AI
                _ => Brush.Parse("#10B981")                              // Xanh lá: 24/7
            };

            var rect = new Rectangle
            {
                Width = w,
                Height = height * 0.45,
                Fill = fillBrush,
                RadiusX = 2,
                RadiusY = 2,
                Opacity = 0.85
            };

            Canvas.SetLeft(rect, x);
            Canvas.SetTop(rect, height * 0.5);
            TimelineCanvas.Children.Add(rect);
        }

        UpdatePlayheadPosition();
    }

    private void UpdatePlayheadPosition()
    {
        double width = this.Bounds.Width;
        if (width <= 0) return;

        double ratio = _currentTime.TotalSeconds / 86400.0;
        double x = ratio * width;

        PlayheadLine.StartPoint = new Point(x, 0);
        PlayheadLine.EndPoint = new Point(x, this.Bounds.Height);

        Canvas.SetLeft(PlayheadHead, x);
        Canvas.SetTop(PlayheadHead, 0);
    }

    private void OnCanvasPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        _isDragging = true;
        SeekToPosition(e.GetPosition(TimelineCanvas).X);
    }

    private void OnCanvasPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_isDragging)
        {
            SeekToPosition(e.GetPosition(TimelineCanvas).X);
        }
    }

    private void OnCanvasPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _isDragging = false;
    }

    private void SeekToPosition(double x)
    {
        double width = this.Bounds.Width;
        if (width <= 0) return;

        double clampedX = Math.Clamp(x, 0, width);
        double ratio = clampedX / width;
        double targetSeconds = ratio * 86400.0;

        CurrentTime = TimeSpan.FromSeconds(targetSeconds);
        DateTime selectedExactTime = _currentDay.Date.AddSeconds(targetSeconds);
        TimeSelected?.Invoke(this, selectedExactTime);
    }
}
