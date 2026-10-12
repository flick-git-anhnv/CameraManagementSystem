using Avalonia.Controls;
using Avalonia.Interactivity;
using CMS.App.Controls;
using CMS.Core.Entities;

namespace CMS.App.Views;

public partial class LiveViewGrid : UserControl
{
    private readonly List<VideoCellView> _cells = new();
    private int _currentGridSize = 2; // Mặc định 2x2
    private VideoCellView? _maximizedCell = null;
    private int _selectedCellIndex = 0;

    public LiveViewGrid()
    {
        InitializeComponent();

        BtnGrid1x1.Click += (_, _) => SetGridLayout(1);
        BtnGrid2x2.Click += (_, _) => SetGridLayout(2);
        BtnGrid3x3.Click += (_, _) => SetGridLayout(3);
        BtnGrid4x4.Click += (_, _) => SetGridLayout(4);
        BtnStopAll.Click += (_, _) => StopAll();

        SetGridLayout(2);
    }

    public void SetGridLayout(int size)
    {
        _currentGridSize = size;
        _maximizedCell = null;

        // Dừng các cell thừa nếu giảm kích thước lưới
        for (int i = size * size; i < _cells.Count; i++)
        {
            _cells[i].StopCamera();
        }

        VideoGridContainer.RowDefinitions.Clear();
        VideoGridContainer.ColumnDefinitions.Clear();
        VideoGridContainer.Children.Clear();

        for (int i = 0; i < size; i++)
        {
            VideoGridContainer.RowDefinitions.Add(new RowDefinition(GridLength.Star));
            VideoGridContainer.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        }

        // Tạo đủ số cell cần thiết
        int total = size * size;
        while (_cells.Count < total)
        {
            var cell = new VideoCellView();
            int cellIdx = _cells.Count;

            cell.DoubleClicked += (_, _) => OnCellDoubleClicked(cell);
            cell.PointerPressed += (_, _) => _selectedCellIndex = cellIdx;

            _cells.Add(cell);
        }

        // Bố trí cell vào Grid
        for (int r = 0; r < size; r++)
        {
            for (int c = 0; c < size; c++)
            {
                int index = r * size + c;
                var cell = _cells[index];
                cell.IsVisible = true;
                Grid.SetRow(cell, r);
                Grid.SetColumn(cell, c);
                Grid.SetRowSpan(cell, 1);
                Grid.SetColumnSpan(cell, 1);
                VideoGridContainer.Children.Add(cell);
            }
        }
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
            Grid.SetRowSpan(cell, _currentGridSize);
            Grid.SetColumnSpan(cell, _currentGridSize);

            if (cell.CurrentCamera != null && cell.IsPlaying)
            {
                cell.PlayCamera(cell.CurrentCamera, useMainStream: true);
            }
        }
        else
        {
            // Thu nhỏ lại về lưới cũ và đổi về Sub-stream (tiết kiệm CPU)
            _maximizedCell = null;
            SetGridLayout(_currentGridSize);

            if (cell.CurrentCamera != null && cell.IsPlaying)
            {
                cell.PlayCamera(cell.CurrentCamera, useMainStream: false);
            }
        }
    }

    public void PlayCameraOnSelectedCell(CameraDevice camera)
    {
        if (_selectedCellIndex >= 0 && _selectedCellIndex < _cells.Count)
        {
            _cells[_selectedCellIndex].PlayCamera(camera, useMainStream: false);
            _selectedCellIndex = (_selectedCellIndex + 1) % (_currentGridSize * _currentGridSize);
        }
    }

    public void AutoPlayCameras(IEnumerable<CameraDevice> cameras)
    {
        int idx = 0;
        foreach (var cam in cameras)
        {
            if (idx >= _cells.Count) break;
            _cells[idx].PlayCamera(cam, useMainStream: false);
            idx++;
        }
    }

    public void StopAll()
    {
        foreach (var cell in _cells)
        {
            cell.StopCamera();
        }
    }
}
