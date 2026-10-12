using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MsBox.Avalonia;

namespace Kztek.Cameras;

/// <summary>
/// ViewModel for <see cref="VideoCaptureDeviceSettingPage"/> — Avalonia port of the WinForms page.
/// AForge (FilterInfoCollection) device enumeration was already commented in WinForms source
/// → <see cref="DeviceItems"/> stays empty in practice; <see cref="Completed"/> always true after ctor.
/// </summary>
public partial class VideoCaptureDeviceSettingPageViewModel : ObservableObject
{
    // ── Device list ─────────────────────────────────────────────────────────────
    // Populated dynamically (AForge code commented → starts empty in practice).
    // Bound to cbxDevice.ItemsSource in AXAML.
    public ObservableCollection<string> DeviceItems { get; } = new();

    [ObservableProperty]
    private int _deviceIndex;

    /// <summary>
    /// False when no devices found (WinForms: <c>cbxDevice.Enabled = false</c> in catch block).
    /// Always true in practice because AForge device enumeration is commented out.
    /// </summary>
    [ObservableProperty]
    private bool _isDeviceEnabled = true;

    /// <summary>
    /// Parity with WinForms <c>completed</c> field — set true in try block of constructor.
    /// AForge code is commented → always true after ctor (catch block never reached).
    /// </summary>
    public bool Completed { get; internal set; }

    // ── Desired Frame Rate ───────────────────────────────────────────────────────
    [ObservableProperty]
    private int _desiredFrameRateIndex;

    // ── Desired Frame Size ───────────────────────────────────────────────────────
    [ObservableProperty]
    private int _desiredFrameSizeIndex;

    // ── Channel ──────────────────────────────────────────────────────────────────
    [ObservableProperty]
    private int _chanelIndex;

    // ── De-interlace filter ───────────────────────────────────────────────────────

    [ObservableProperty]
    private bool _usingDeinterlaceFilter;

    /// <summary>
    /// Selected deinterlace filter name — parity with WinForms <c>cbxDeinterlaceFilterName.Text</c>.
    /// Null = no item selected (matches WinForms initial state: DropDown style, no SelectedIndex set).
    /// </summary>
    [ObservableProperty]
    private string? _selectedDeinterlaceFilterName;

    // ── Video Decoder ─────────────────────────────────────────────────────────────
    [ObservableProperty]
    private int _videoDecoderIndex;

    // ── Find Filter command ────────────────────────────────────────────────────────

    /// <summary>
    /// Mirrors WinForms <c>btnFindFilter_Click</c> — ADR §6.1 E21.
    /// AForge FilterInfoCollection search was already commented in WinForms source
    /// → <c>notFound</c> stays true → always shows "not found" message.
    /// <c>MessageBox.Show</c> replaced with MsBox.Avalonia (ADR §6.1 mapping).
    /// </summary>
    [RelayCommand]
    private async Task FindFilterAsync()
    {
        bool notFound = true;
        string deinterlaceFilterName = SelectedDeinterlaceFilterName ?? string.Empty;
        if (deinterlaceFilterName != "")
        {
            //FilterInfoCollection filters = new FilterInfoCollection(FilterCategory.LegacyAmFilterCategory);
            //foreach (FilterInfo filterInfo in filters)
            //{
            //    if (filterInfo.Name.StartsWith(deinterlaceFilterName))
            //    {
            //        notFound = false;
            //        break;
            //    }
            //}
        }

        if (!notFound)
        {
            await MessageBoxManager.GetMessageBoxStandard("", "OK").ShowWindowAsync();
        }
        else
        {
            await MessageBoxManager
                .GetMessageBoxStandard("", $"Deinterlace Filter '{deinterlaceFilterName}' not found.")
                .ShowWindowAsync();
        }
    }
}
