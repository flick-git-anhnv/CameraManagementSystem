using System;
using Avalonia.Controls;

namespace CMS.App.Views;

public partial class AlarmCenterView : UserControl
{
    public AlarmCenterView()
    {
        InitializeComponent();

        // ── Khôi phục kích thước Splitter cho Alarm Center ─────────────────
        var mainGrid = SplitterAlarmCenter.Parent as Grid;
        var savedWidth = CMS.App.Services.AppSettingsService.Instance.Current.AlarmCenterDetailWidth;
        if (mainGrid != null && mainGrid.ColumnDefinitions.Count > 2 && savedWidth >= 280 && savedWidth <= 550)
        {
            mainGrid.ColumnDefinitions[2].Width = new GridLength(savedWidth, GridUnitType.Pixel);
        }

        // Lắng nghe khi kéo thả Splitter Alarm Center xong -> tự động lưu cấu hình
        SplitterAlarmCenter.DragCompleted += (_, _) => SaveAlarmCenterSplitterWidth();

        this.Unloaded += (_, _) => SaveAlarmCenterSplitterWidth();
    }

    private void SaveAlarmCenterSplitterWidth()
    {
        var mainGrid = SplitterAlarmCenter.Parent as Grid;
        if (mainGrid == null || mainGrid.ColumnDefinitions.Count <= 2) return;
        var col = mainGrid.ColumnDefinitions[2];

        double w = col.ActualWidth > 0
            ? col.ActualWidth
            : (col.Width.IsAbsolute ? col.Width.Value : 0);

        if (w >= 280 && w <= 550)
        {
            CMS.App.Services.AppSettingsService.Instance.Update(s => s.AlarmCenterDetailWidth = Math.Round(w));
        }
    }
}
