using System.IO;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Kztek.Cameras;

/// <summary>
/// ViewModel for <see cref="frmViewImage"/> — Avalonia port of the WinForms form.
/// Fix (2026-07-20, loại bỏ SkiaSharp): giữ WriteableBitmap xuyên suốt (không còn SKBitmap round-trip
/// PNG để hiển thị — WriteableBitmap đã kế thừa Avalonia Bitmap, bind trực tiếp vào Image.Source).
/// </summary>
public partial class frmViewImageViewModel : ObservableObject
{
    // Mirrored from WinForms frmViewImage.preferPath — static, shared across instances.
    public static string preferPath = @"C:\";

    private readonly Window _owner;
    private WriteableBitmap? _currentBitmap;

    /// <summary>Avalonia Bitmap bound to <c>Image.Source</c> in the view.</summary>
    [ObservableProperty]
    private Bitmap? _displayBitmap;

    /// <summary>Size label text, e.g. "Size: 1920x1080".</summary>
    [ObservableProperty]
    private string _sizeText = string.Empty;

    public frmViewImageViewModel(Window owner)
    {
        _owner = owner;
    }

    /// <summary>
    /// Gets or sets the source image. Setting this updates <see cref="DisplayBitmap"/> and
    /// <see cref="SizeText"/>.
    /// </summary>
    public WriteableBitmap? CurrentBitmap
    {
        get => _currentBitmap;
        set
        {
            _currentBitmap = value;
            UpdateDisplay();
        }
    }

    private void UpdateDisplay()
    {
        // WriteableBitmap kế thừa Bitmap — bind trực tiếp, không cần convert/dispose bản cũ riêng
        // (caller sở hữu vòng đời _currentBitmap, không phải ViewModel này).
        DisplayBitmap = _currentBitmap;

        SizeText = _currentBitmap != null
            ? $"Size: {_currentBitmap.PixelSize.Width}x{_currentBitmap.PixelSize.Height}"
            : string.Empty;
    }

    /// <summary>
    /// Opens SaveFilePickerAsync and saves the current image as JPEG.
    /// Mirrors WinForms btnSave_Click behaviour (ADR E10).
    /// </summary>
    [RelayCommand]
    private async Task SaveAsync()
    {
        if (_currentBitmap == null) return;

        IStorageFolder? startFolder = null;
        try
        {
            startFolder = await _owner.StorageProvider
                .TryGetFolderFromPathAsync(new Uri(preferPath));
        }
        catch
        {
            // preferPath may be invalid on first use — fall back to OS default.
        }

        var file = await _owner.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Lưu hình",
            SuggestedStartLocation = startFolder,
            SuggestedFileName = "image",
            DefaultExtension = "jpeg",
            FileTypeChoices = new[]
            {
                new FilePickerFileType("Image files (*.jpeg, *.jpg)")
                {
                    Patterns = new[] { "*.jpeg", "*.jpg" },
                    MimeTypes = new[] { "image/jpeg" }
                },
                new FilePickerFileType("All files (*.*)")
                {
                    Patterns = new[] { "*.*" }
                }
            }
        });

        if (file == null) return;

        try
        {
            var localPath = file.Path.LocalPath;
            preferPath = Path.GetDirectoryName(localPath) ?? preferPath;

            await using var stream = await file.OpenWriteAsync();
            // quality=95 chọn nhánh encode JPEG của Avalonia (khớp hành vi cũ SKImage Encode Jpeg 95).
            _currentBitmap.Save(stream, 95);
        }
        catch
        {
            // Best-effort save — behavior parity: original also did not show an error dialog.
        }
    }
}
