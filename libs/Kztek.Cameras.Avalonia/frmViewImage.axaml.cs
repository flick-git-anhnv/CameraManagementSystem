using Avalonia.Controls;
using Avalonia.Media.Imaging;

namespace Kztek.Cameras;

/// <summary>
/// Avalonia Window — port of WinForms <c>frmViewImage</c> (U3, ADR §5.1).
/// Displays a single image with an option to save as JPEG.
/// </summary>
public partial class frmViewImage : Window
{
    private readonly frmViewImageViewModel _vm;

    /// <summary>
    /// Mirrors WinForms <c>frmViewImage.preferPath</c>; delegates to ViewModel static field.
    /// </summary>
    public static string preferPath
    {
        get => frmViewImageViewModel.preferPath;
        set => frmViewImageViewModel.preferPath = value;
    }

    public frmViewImage()
    {
        InitializeComponent();
        _vm = new frmViewImageViewModel(this);
        DataContext = _vm;
    }

    /// <summary>
    /// Gets or sets the image to display.
    /// Fix (2026-07-20, loại bỏ SkiaSharp): SKBitmap → Avalonia WriteableBitmap — hiển thị trực tiếp
    /// (WriteableBitmap kế thừa Bitmap), không cần convert tại UI boundary nữa.
    /// </summary>
    public WriteableBitmap? CurrentImage
    {
        get => _vm.CurrentBitmap;
        set => _vm.CurrentBitmap = value;
    }
}
