using CMS.Core.Entities;
using CMS.Core.Interfaces;
using CMS.Infrastructure.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace CMS.App.ViewModels;

public partial class PlaybackViewModel : ObservableObject
{
    private readonly ICameraRepository _cameraRepo;
    private readonly IVideoSegmentRepository _segmentRepo;

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
    private string _playbackStatus = "Select camera and date to search recorded video.";

    [ObservableProperty]
    private bool _isPlaying = false;

    [ObservableProperty]
    private string _speedLabel = "1.0x";

    public PlaybackViewModel(ICameraRepository? cameraRepo = null, IVideoSegmentRepository? segmentRepo = null)
    {
        _cameraRepo = cameraRepo ?? new SqliteCameraRepository();
        _segmentRepo = segmentRepo ?? new SqliteVideoSegmentRepository();

        _ = LoadCamerasAsync();
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
            PlaybackStatus = "Please select a camera to search.";
            return;
        }

        DateTime dayStart = SelectedDate.Date;
        DateTime dayEnd = dayStart.AddDays(1).AddSeconds(-1);

        PlaybackStatus = $"Searching recordings for {SelectedCamera.Name} on {dayStart:yyyy-MM-dd}...";

        var list = await _segmentRepo.GetSegmentsAsync(SelectedCamera.Id, dayStart, dayEnd);
        AvailableSegments.Clear();
        foreach (var s in list)
        {
            AvailableSegments.Add(s);
        }

        PlaybackStatus = list.Count > 0
            ? $"✅ Found {list.Count} recorded segment(s) on {dayStart:yyyy-MM-dd}."
            : $"ℹ️ No recordings found for {SelectedCamera.Name} on {dayStart:yyyy-MM-dd}.";
    }

    [RelayCommand]
    public void TogglePlayPause()
    {
        IsPlaying = !IsPlaying;
    }

    public VideoSegment? FindSegmentAtTime(DateTime time)
    {
        return AvailableSegments.FirstOrDefault(s => s.StartTime <= time && s.EndTime >= time);
    }
}
