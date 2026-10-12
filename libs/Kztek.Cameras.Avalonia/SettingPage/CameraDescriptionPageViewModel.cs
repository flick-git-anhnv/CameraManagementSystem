using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Kztek.Cameras;

/// <summary>
/// ViewModel cho <see cref="CameraDescriptionPage"/> — Avalonia port (task G5.10).
/// Giữ trạng thái Name, Description, VideoSourceIndex.
/// Fires StateChanged khi Name/VideoSourceIndex thay đổi (parity ADR §6.2 E19).
/// Fires Reset khi VideoSourceIndex thay đổi (parity WinForms cbxVideoSource_SelectedIndexChanged).
/// </summary>
public partial class CameraDescriptionPageViewModel : ObservableObject
{
    // ── Events ─────────────────────────────────────────────────────────

    /// <summary>Fires khi tên hoặc loại camera thay đổi — parity với WinForms StateChanged.</summary>
    public event EventHandler? StateChanged;

    /// <summary>Fires khi loại camera (VideoSourceIndex) thay đổi — parity với WinForms Reset.</summary>
    public event EventHandler? Reset;

    // ── Observable properties ──────────────────────────────────────────

    /// <summary>Tên camera (bound 2-way với KzTextBox txtName). MaxLength=25 ở AXAML.</summary>
    [ObservableProperty]
    private string _name = string.Empty;

    /// <summary>Ghi chú camera (bound 2-way với KzTextBox txtDescription). MaxLength=255 ở AXAML.</summary>
    [ObservableProperty]
    private string _description = string.Empty;

    /// <summary>
    /// Index loại camera đang chọn (bound 2-way với ComboBox cbxVideoSource).
    /// Mặc định = 0 (Video File) — parity với WinForms constructor.
    /// </summary>
    [ObservableProperty]
    private int _videoSourceIndex;

    // ── Computed property ──────────────────────────────────────────────

    /// <summary>
    /// True khi tên camera không rỗng VÀ loại camera đã chọn.
    /// Parity với WinForms Completed: (txtName.TextLength != 0) &amp;&amp; (cbxVideoSource.SelectedIndex &gt;= 0).
    /// </summary>
    public bool Completed => !string.IsNullOrEmpty(Name) && VideoSourceIndex >= 0;

    // ── Change callbacks ────────────────────────────────────────────────

    partial void OnNameChanged(string value)
    {
        OnPropertyChanged(nameof(Completed));
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    partial void OnVideoSourceIndexChanged(int value)
    {
        OnPropertyChanged(nameof(Completed));
        StateChanged?.Invoke(this, EventArgs.Empty);
        Reset?.Invoke(this, EventArgs.Empty);
    }
}
