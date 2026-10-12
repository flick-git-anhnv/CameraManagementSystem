using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using CMS.App.Controls;
using CMS.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CMS.App.Views;

public partial class LiveViewGrid : UserControl
{
    private readonly List<VideoCellView> _cells = new();
    private int _currentRows = 2;
    private int _currentCols = 2;
    private VideoCellView? _maximizedCell = null;
    private int _selectedCellIndex = 0;
    private bool _isUpdatingInputs = false;

    public int CurrentRows => _currentRows;
    public int CurrentCols => _currentCols;
    public int TotalCells => _currentRows * _currentCols;
    public IReadOnlyList<VideoCellView> Cells => _cells;

    public event EventHandler? CameraStateChanged;
    public event EventHandler<CameraDevice?>? SelectedCameraChanged;
    public CameraDevice? SelectedCamera => (_selectedCellIndex >= 0 && _selectedCellIndex < _cells.Count) ? _cells[_selectedCellIndex].CurrentCamera : null;

    public LiveViewGrid()
    {
        InitializeComponent();

        BtnGrid1x1.Click += (_, _) => SetGridLayout(1, 1);
        BtnGrid2x2.Click += (_, _) => SetGridLayout(2, 2);
        BtnGrid3x3.Click += (_, _) => SetGridLayout(3, 3);
        BtnGrid4x4.Click += (_, _) => SetGridLayout(4, 4);
        BtnGrid2x3.Click += (_, _) => SetGridLayout(2, 3);
        BtnGrid2x4.Click += (_, _) => SetGridLayout(2, 4);
        BtnGrid3x4.Click += (_, _) => SetGridLayout(3, 4);

        BtnApplyCustomGrid.Click += (_, _) => ApplyCustomGrid();
        NumRows.KeyDown += OnNumKeyDown;
        NumCols.KeyDown += OnNumKeyDown;

        BtnStopAll.Click += (_, _) => StopAll();
        BtnRecordAll.Click += (_, _) => ToggleRecordAll();

        SetGridLayout(2, 2);
    }

    private void OnNumKeyDown(object? sender, Avalonia.Input.KeyEventArgs e)
    {
        if (e.Key == Avalonia.Input.Key.Enter)
        {
            ApplyCustomGrid();
        }
    }

    private void ApplyCustomGrid()
    {
        int rows = (int)(NumRows.Value ?? 2);
        int cols = (int)(NumCols.Value ?? 2);
        SetGridLayout(rows, cols);
    }

    public void SetGridLayout(int size) => SetGridLayout(size, size);

    public void SetGridLayout(int rows, int cols)
    {
        if (rows < 1) rows = 1;
        if (cols < 1) cols = 1;
        if (rows > 10) rows = 10;
        if (cols > 10) cols = 10;

        _currentRows = rows;
        _currentCols = cols;
        _maximizedCell = null;

        int total = rows * cols;

        // Cập nhật giá trị trên các NumericUpDown nếu không đang trong quá trình update
        if (!_isUpdatingInputs)
        {
            _isUpdatingInputs = true;
            NumRows.Value = rows;
            NumCols.Value = cols;
            _isUpdatingInputs = false;
        }

        // Dừng các cell thừa nếu giảm kích thước lưới
        for (int i = total; i < _cells.Count; i++)
        {
            _cells[i].StopCamera();
        }

        VideoGridContainer.RowDefinitions.Clear();
        VideoGridContainer.ColumnDefinitions.Clear();
        VideoGridContainer.Children.Clear();

        for (int r = 0; r < rows; r++)
        {
            VideoGridContainer.RowDefinitions.Add(new RowDefinition(GridLength.Star));
        }
        for (int c = 0; c < cols; c++)
        {
            VideoGridContainer.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        }

        // Tạo đủ số cell cần thiết
        while (_cells.Count < total)
        {
            var cell = new VideoCellView();
            int cellIdx = _cells.Count;

            cell.DoubleClicked += (_, _) => OnCellDoubleClicked(cell);
            cell.AddHandler(Avalonia.Input.InputElement.PointerPressedEvent, (_, _) => SelectCell(cellIdx), Avalonia.Interactivity.RoutingStrategies.Tunnel | Avalonia.Interactivity.RoutingStrategies.Bubble, handledEventsToo: true);
            cell.PlaybackStateChanged += (_, _) =>
            {
                CameraStateChanged?.Invoke(this, EventArgs.Empty);
                if (cellIdx == _selectedCellIndex)
                {
                    SelectedCameraChanged?.Invoke(this, SelectedCamera);
                }
            };
            cell.RecordingStateChanged += (_, _) => CameraStateChanged?.Invoke(this, EventArgs.Empty);
            cell.Closed += (_, _) =>
            {
                CameraStateChanged?.Invoke(this, EventArgs.Empty);
                if (cellIdx == _selectedCellIndex)
                {
                    SelectedCameraChanged?.Invoke(this, SelectedCamera);
                }
            };

            _cells.Add(cell);
        }

        // Bố trí cell vào Grid
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                int index = r * cols + c;
                var cell = _cells[index];
                cell.IsVisible = true;
                Grid.SetRow(cell, r);
                Grid.SetColumn(cell, c);
                Grid.SetRowSpan(cell, 1);
                Grid.SetColumnSpan(cell, 1);
                VideoGridContainer.Children.Add(cell);
            }
        }

        if (_selectedCellIndex >= total)
        {
            _selectedCellIndex = 0;
        }

        UpdatePresetButtonStyles();
        UpdateSelectedCellHighlight();
        CameraStateChanged?.Invoke(this, EventArgs.Empty);
    }

    private void SelectCell(int index)
    {
        int total = _currentRows * _currentCols;
        if (index < 0 || index >= total || index >= _cells.Count) return;
        _selectedCellIndex = index;
        UpdateSelectedCellHighlight();
        SelectedCameraChanged?.Invoke(this, _cells[_selectedCellIndex].CurrentCamera);
    }

    private void UpdateSelectedCellHighlight()
    {
        int total = _currentRows * _currentCols;
        for (int i = 0; i < total && i < _cells.Count; i++)
        {
            _cells[i].SetSelected(i == _selectedCellIndex);
        }
    }

    private void UpdatePresetButtonStyles()
    {
        var primaryBrush = Brush.Parse("#F05922");
        var secondaryBg = this.FindResource("ThemeButtonSecondaryBackgroundBrush") as IBrush ?? Brush.Parse("#26223B");
        var secondaryFg = this.FindResource("ThemeButtonSecondaryForegroundBrush") as IBrush ?? Brush.Parse("#E4E4E7");

        void SetBtnStyle(Button btn, bool isActive)
        {
            btn.Background = isActive ? primaryBrush : secondaryBg;
            btn.Foreground = isActive ? Brushes.White : secondaryFg;
            btn.BorderThickness = isActive ? new Thickness(0) : new Thickness(1);
        }

        SetBtnStyle(BtnGrid1x1, _currentRows == 1 && _currentCols == 1);
        SetBtnStyle(BtnGrid2x2, _currentRows == 2 && _currentCols == 2);
        SetBtnStyle(BtnGrid3x3, _currentRows == 3 && _currentCols == 3);
        SetBtnStyle(BtnGrid4x4, _currentRows == 4 && _currentCols == 4);
        SetBtnStyle(BtnGrid2x3, _currentRows == 2 && _currentCols == 3);
        SetBtnStyle(BtnGrid2x4, _currentRows == 2 && _currentCols == 4);
        SetBtnStyle(BtnGrid3x4, _currentRows == 3 && _currentCols == 4);

        bool isCustom = !((_currentRows == 1 && _currentCols == 1) ||
                          (_currentRows == 2 && _currentCols == 2) ||
                          (_currentRows == 3 && _currentCols == 3) ||
                          (_currentRows == 4 && _currentCols == 4) ||
                          (_currentRows == 2 && _currentCols == 3) ||
                          (_currentRows == 2 && _currentCols == 4) ||
                          (_currentRows == 3 && _currentCols == 4));

        SetBtnStyle(BtnApplyCustomGrid, isCustom);
    }

    private void OnCellDoubleClicked(VideoCellView cell)
    {
        if (_maximizedCell == null)
        {
            // Phóng to ô này chiếm toàn bộ lưới và đổi sang Main-stream (Full HD)
            _maximizedCell = cell;

            foreach (var c in _cells)
            {
                c.IsVisible = (c == cell);
            }

            Grid.SetRow(cell, 0);
            Grid.SetColumn(cell, 0);
            Grid.SetRowSpan(cell, _currentRows);
            Grid.SetColumnSpan(cell, _currentCols);

            if (cell.CurrentCamera != null && cell.IsPlaying)
            {
                cell.PlayCamera(cell.CurrentCamera, useMainStream: true);
            }
        }
        else
        {
            // Thu nhỏ lại về lưới cũ và đổi về Sub-stream (tiết kiệm CPU)
            _maximizedCell = null;
            SetGridLayout(_currentRows, _currentCols);

            if (cell.CurrentCamera != null && cell.IsPlaying)
            {
                cell.PlayCamera(cell.CurrentCamera, useMainStream: false);
            }
        }
    }

    public void PlayCameraOnSelectedCell(CameraDevice camera)
    {
        int total = _currentRows * _currentCols;
        if (_selectedCellIndex >= 0 && _selectedCellIndex < total && _selectedCellIndex < _cells.Count)
        {
            int targetIdx = _selectedCellIndex;
            if (_cells[targetIdx].IsPlaying && _cells[targetIdx].CurrentCamera?.Id != camera.Id)
            {
                int emptyIdx = _cells.Take(total).ToList().FindIndex(c => !c.IsPlaying);
                if (emptyIdx >= 0)
                {
                    targetIdx = emptyIdx;
                }
            }

            _cells[targetIdx].PlayCamera(camera, useMainStream: false);
            SelectCell(targetIdx);
            CameraStateChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public void AutoPlayCameras(IEnumerable<CameraDevice> cameras)
    {
        int total = _currentRows * _currentCols;
        int idx = 0;
        foreach (var cam in cameras)
        {
            if (idx >= total || idx >= _cells.Count) break;
            _cells[idx].PlayCamera(cam, useMainStream: false);
            idx++;
        }
        CameraStateChanged?.Invoke(this, EventArgs.Empty);
        SelectedCameraChanged?.Invoke(this, SelectedCamera);
    }

    public void SyncCameraMetadata(IEnumerable<CameraDevice> latestCameras)
    {
        var map = latestCameras.ToDictionary(c => c.Id, c => c);
        foreach (var cell in _cells)
        {
            if (cell.CurrentCamera != null && map.TryGetValue(cell.CurrentCamera.Id, out var updated))
            {
                cell.CurrentCamera.Name = updated.Name;
                cell.CurrentCamera.IpAddress = updated.IpAddress;
                cell.CurrentCamera.HasPtz = updated.HasPtz;
                cell.CurrentCamera.Brand = updated.Brand;
                cell.CurrentCamera.HttpPort = updated.HttpPort;
                cell.CurrentCamera.RtspPort = updated.RtspPort;
                cell.CurrentCamera.Username = updated.Username;
                cell.CurrentCamera.Password = updated.Password;
                cell.CurrentCamera.Channel = updated.Channel;
            }
        }
        SelectedCameraChanged?.Invoke(this, SelectedCamera);
    }

    public void ToggleRecordAll()
    {
        int total = _currentRows * _currentCols;
        var activeCells = _cells.Take(total).Where(c => c.CurrentCamera != null).ToList();
        bool anyRecording = activeCells.Any(c => c.IsRecording);

        foreach (var cell in activeCells)
        {
            if (anyRecording && cell.IsRecording)
            {
                cell.ToggleRecording();
            }
            else if (!anyRecording && !cell.IsRecording)
            {
                cell.ToggleRecording();
            }
        }

        BtnRecordAll.Content = anyRecording ? "⏺️ Ghi tất cả" : "⏹️ Dừng ghi";
        BtnRecordAll.Background = anyRecording ? Avalonia.Media.Brush.Parse("#DC2626") : Avalonia.Media.Brushes.ForestGreen;
        CameraStateChanged?.Invoke(this, EventArgs.Empty);
    }

    public void StopAll()
    {
        foreach (var cell in _cells)
        {
            cell.StopCamera();
        }
        CameraStateChanged?.Invoke(this, EventArgs.Empty);
        SelectedCameraChanged?.Invoke(this, SelectedCamera);
    }

    public HashSet<string> GetViewingCameraIds()
    {
        int total = _currentRows * _currentCols;
        return _cells.Take(total)
                     .Where(c => c.IsPlaying && c.CurrentCamera != null)
                     .Select(c => c.CurrentCamera!.Id)
                     .ToHashSet();
    }

    public HashSet<string> GetRecordingCameraIds()
    {
        int total = _currentRows * _currentCols;
        foreach (var cell in _cells.Take(total))
        {
            cell.RefreshRecordingIndicator();
        }

        var set = _cells.Take(total)
                        .Where(c => c.IsRecording && c.CurrentCamera != null)
                        .Select(c => c.CurrentCamera!.Id)
                        .ToHashSet();

        foreach (var id in VideoCellView.GlobalStorageManager.GetActiveRecordingCameraIds())
        {
            set.Add(id);
        }
        return set;
    }
}
