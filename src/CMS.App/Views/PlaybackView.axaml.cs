using Avalonia.Controls;
using CMS.App.ViewModels;

namespace CMS.App.Views;

public partial class PlaybackView : UserControl
{
    public PlaybackView()
    {
        InitializeComponent();

        Timeline.TimeSelected += OnTimelineTimeSelected;
        this.DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is PlaybackViewModel vm)
        {
            vm.AvailableSegments.CollectionChanged += (_, _) =>
            {
                Timeline.CurrentDay = vm.SelectedDate.Date;
                Timeline.SetSegments(vm.AvailableSegments);
            };
        }
    }

    private void OnTimelineTimeSelected(object? sender, DateTime selectedTime)
    {
        if (DataContext is PlaybackViewModel vm)
        {
            var match = vm.FindSegmentAtTime(selectedTime);
            if (match != null)
            {
                vm.CurrentPlayingSegment = match;
                vm.PlaybackStatus = $"▶ Playing segment from {match.StartTime:HH:mm:ss} to {match.EndTime:HH:mm:ss} (Seek: {selectedTime:HH:mm:ss})";
            }
            else
            {
                vm.PlaybackStatus = $"No recording found at {selectedTime:HH:mm:ss}.";
            }
        }
    }
}
