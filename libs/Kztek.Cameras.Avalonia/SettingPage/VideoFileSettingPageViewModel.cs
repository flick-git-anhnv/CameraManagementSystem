using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Kztek.Cameras;

/// <summary>
/// ViewModel for <see cref="VideoFileSettingPage"/> — Avalonia port of the WinForms page.
/// Holds the selected video file path + decoder/frame-size selection, and handles the
/// Browse command via IStorageProvider.OpenFilePickerAsync (ADR §6.2 E22 — replaces
/// OpenFileDialog.ShowDialog()).
/// </summary>
public partial class VideoFileSettingPageViewModel : ObservableObject
{
    /// <summary>Fires whenever <see cref="VideoSource"/> changes — parity with WinForms StateChanged event.</summary>
    public event EventHandler? StateChanged;

    [ObservableProperty]
    private string _videoSource = string.Empty;

    [ObservableProperty]
    private int _desiredFrameSizeIndex;

    [ObservableProperty]
    private int _videoDecoderIndex;

    /// <summary>True once a video file path has been entered/selected — parity with WinForms Completed.</summary>
    public bool Completed => !string.IsNullOrEmpty(VideoSource);

    partial void OnVideoSourceChanged(string value)
    {
        OnPropertyChanged(nameof(Completed));
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Opens OpenFilePickerAsync and sets <see cref="VideoSource"/> to the picked file path.
    /// Mirrors WinForms btnOpen_Click (OpenFileDialog.ShowDialog()) — ADR §6.2 "OpenFileDialog → IStorageProvider.OpenFilePickerAsync".
    /// </summary>
    [RelayCommand]
    private async Task BrowseAsync(Visual? owner)
    {
        var topLevel = owner != null ? TopLevel.GetTopLevel(owner) : null;
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Chọn file video",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Video files (*.avi, *.mp4, *.mkv, *.wmv)")
                {
                    Patterns = new[] { "*.avi", "*.mp4", "*.mkv", "*.wmv" }
                },
                new FilePickerFileType("All files (*.*)")
                {
                    Patterns = new[] { "*.*" }
                }
            }
        });

        if (files.Count == 0) return;

        VideoSource = files[0].Path.LocalPath;
    }
}
