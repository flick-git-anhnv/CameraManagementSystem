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

    public bool IsDragging => _isDragging;

    public TimelineControl()
    {
        InitializeComponent();

        this.SizeChanged += (_, _) => RedrawTimeline();
        this.ActualThemeVariantChanged += (_, _) => RedrawTimeline();
        CMS.App.Services.ThemeService.Instance.ThemeChanged += (_, _) => RedrawTimeline();
    }

    public void SetSegments(IEnumerable<VideoSegment> segments)
    {
        _segments = segments.ToList();
        RedrawTimeline();
    }

    private const double HorizontalPadding = 12.0;

    public void RedrawTimeline()
    {
        TimelineCanvas.Children.Clear();
        double width = this.Bounds.Width;
        double height = this.Bounds.Height;

        if (width <= 2 * HorizontalPadding || height <= 0) return;

        double usableWidth = width - (2 * HorizontalPadding);

        IBrush majorTickBrush = this.TryFindResource("ThemeTimelineTickMajorBrush", out var majorRes) && majorRes is IBrush mb ? mb : Brush.Parse("#52525B");
        IBrush minorTickBrush = this.TryFindResource("ThemeTimelineTickMinorBrush", out var minorRes) && minorRes is IBrush mnb ? mnb : Brush.Parse("#27272A");
        IBrush textBrush = this.TryFindResource("ThemeTimelineTextBrush", out var textRes) && textRes is IBrush tb ? tb : Brush.Parse("#71717A");

        // 1. Vẽ các vạch giờ (0h - 24h)
        for (int h = 0; h <= 24; h++)
        {
            double x = HorizontalPadding + (h / 24.0) * usableWidth;
            bool isMajor = h % 6 == 0;
            bool isEven = h % 2 == 0;

            double tickEndY = isMajor ? height : (isEven ? height * 0.45 : height * 0.25);

            var hourLine = new Line
            {
                StartPoint = new Point(x, 0),
                EndPoint = new Point(x, tickEndY),
                Stroke = isMajor ? majorTickBrush : minorTickBrush,
                StrokeThickness = isMajor ? 1.5 : 1
            };
            TimelineCanvas.Children.Add(hourLine);

            if (isEven && h < 24)
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

            double x = HorizontalPadding + (startSec / totalSeconds) * usableWidth;
            double w = Math.Max(6, (segSec / totalSeconds) * usableWidth);

            IBrush fillBrush = seg.RecordingType switch
            {
                RecordingMode.MotionTriggered => Brush.Parse("#F59E0B"), // Vàng: Motion
                RecordingMode.AiTriggered => Brush.Parse("#EF4444"),     // Đỏ: AI
                _ => Brush.Parse("#10B981")                              // Xanh lá: 24/7
            };

            var rect = new Rectangle
            {
                Width = w,
                Height = height * 0.42,
                Fill = fillBrush,
                RadiusX = 3,
                RadiusY = 3,
                Opacity = 0.9
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
        if (width <= 2 * HorizontalPadding) return;

        double usableWidth = width - (2 * HorizontalPadding);
        double ratio = Math.Clamp(_currentTime.TotalSeconds / 86400.0, 0.0, 1.0);
        double x = HorizontalPadding + ratio * usableWidth;

        PlayheadLine.StartPoint = new Point(x, 0);
        PlayheadLine.EndPoint = new Point(x, this.Bounds.Height);

        Canvas.SetLeft(PlayheadHead, x);
        Canvas.SetTop(PlayheadHead, 0);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        _isDragging = true;
        e.Pointer.Capture(this);
        UpdatePositionFromX(e.GetPosition(this).X);
        e.Handled = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (_isDragging)
        {
            UpdatePositionFromX(e.GetPosition(this).X);
            e.Handled = true;
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (_isDragging)
        {
            _isDragging = false;
            e.Pointer.Capture(null);
            UpdatePositionFromX(e.GetPosition(this).X);
            CommitSelectedTime();
            e.Handled = true;
        }
    }

    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        if (_isDragging)
        {
            _isDragging = false;
            CommitSelectedTime();
        }
    }

    private void UpdatePositionFromX(double x)
    {
        double width = this.Bounds.Width;
        if (width <= 2 * HorizontalPadding) return;

        double usableWidth = width - (2 * HorizontalPadding);
        double clampedX = Math.Clamp(x - HorizontalPadding, 0, usableWidth);
        double ratio = clampedX / usableWidth;
        double targetSeconds = Math.Clamp(ratio * 86400.0, 0, 86399);

        _currentTime = TimeSpan.FromSeconds(targetSeconds);
        TxtCurrentTime.Text = _currentTime.ToString(@"hh\:mm\:ss");
        UpdatePlayheadPosition();
    }

    private void CommitSelectedTime()
    {
        DateTime selectedExactTime = _currentDay.Date.Add(_currentTime);
        TimeSelected?.Invoke(this, selectedExactTime);
    }
}
