using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using CMS.App.ViewModels;
using CMS.Core.Entities;
using System.Collections.Specialized;

namespace CMS.App.Views;

public partial class EMapView : UserControl
{
    private const string MapNodeTag = "MapNode";
    private EMapViewModel? _currentVm;
    private string? _previewingCameraId;

    // Drag state for moving camera nodes directly on the canvas
    private CameraMapNode? _draggingNode;
    private Point _dragStartPoint;
    private bool _isDraggingNode;

    public EMapView()
    {
        InitializeComponent();

        // ── Khôi phục độ rộng Splitter cho E-Map ───────────────────────────
        var mainGrid = SplitterEMap.Parent as Grid;
        var savedWidth = CMS.App.Services.AppSettingsService.Instance.Current.EMapSidebarWidth;
        if (mainGrid != null && mainGrid.ColumnDefinitions.Count > 0 && savedWidth >= 240 && savedWidth <= 500)
        {
            mainGrid.ColumnDefinitions[0].Width = new GridLength(savedWidth, GridUnitType.Pixel);
        }

        // Lắng nghe khi kéo thả Splitter E-Map xong -> tự động lưu cấu hình
        SplitterEMap.DragCompleted += (_, _) => SaveEMapSplitterWidth();

        MapCanvas.PointerPressed += OnMapCanvasPointerPressed;
        MapCanvas.SizeChanged += (_, _) => RedrawNodes();
        BtnClosePopup.Click += (_, _) => ClosePopup();
        BtnRemoveFromPopup.Click += OnRemoveFromPopupClick;
        BtnLoadMapImage.Click += OnLoadMapImageClick;
        this.DataContextChanged += OnDataContextChanged;
        this.SizeChanged += (_, _) => RedrawNodes();
        this.Unloaded += (_, _) => SaveEMapSplitterWidth();
    }

    private void SaveEMapSplitterWidth()
    {
        var mainGrid = SplitterEMap.Parent as Grid;
        if (mainGrid == null || mainGrid.ColumnDefinitions.Count == 0) return;
        var col = mainGrid.ColumnDefinitions[0];

        double w = col.ActualWidth > 0
            ? col.ActualWidth
            : (col.Width.IsAbsolute ? col.Width.Value : 0);

        if (w >= 240 && w <= 500)
        {
            CMS.App.Services.AppSettingsService.Instance.Update(s => s.EMapSidebarWidth = Math.Round(w));
        }
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (_currentVm != null)
        {
            _currentVm.PlacedNodes.CollectionChanged -= OnPlacedNodesCollectionChanged;
            _currentVm.NodesChanged -= OnVmNodesChanged;
            _currentVm.BackgroundImageLoaded -= OnBackgroundImageLoaded;
        }

        _currentVm = DataContext as EMapViewModel;

        if (_currentVm != null)
        {
            _currentVm.PlacedNodes.CollectionChanged += OnPlacedNodesCollectionChanged;
            _currentVm.NodesChanged += OnVmNodesChanged;
            _currentVm.BackgroundImageLoaded += OnBackgroundImageLoaded;

            if (!string.IsNullOrEmpty(_currentVm.MapImagePath))
            {
                LoadBackgroundImage(_currentVm.MapImagePath);
            }

            RedrawNodes();
        }
    }

    private void OnBackgroundImageLoaded(string imagePath)
    {
        LoadBackgroundImage(imagePath);
    }

    private void LoadBackgroundImage(string? path)
    {
        if (string.IsNullOrEmpty(path) || !System.IO.File.Exists(path)) return;

        try
        {
            CustomMapImage.Source = new Bitmap(path);
            CustomMapImage.IsVisible = true;
            MapCanvas.Background = Brushes.Transparent;

            foreach (var child in MapCanvas.Children)
            {
                if (child.Tag as string != MapNodeTag)
                {
                    child.IsVisible = false;
                }
            }
        }
        catch
        {
            // Fallback nếu ảnh bị lỗi đường dẫn
        }
    }

    private void OnPlacedNodesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        CheckPopupCameraStillPlaced();
        RedrawNodes();
    }

    private void OnVmNodesChanged()
    {
        CheckPopupCameraStillPlaced();
        RedrawNodes();
    }

    private void CheckPopupCameraStillPlaced()
    {
        if (_previewingCameraId != null && _currentVm != null)
        {
            bool stillPlaced = _currentVm.PlacedNodes.Any(n => n.CameraId == _previewingCameraId);
            if (!stillPlaced)
            {
                ClosePopup();
            }
        }
    }

    private void RedrawNodes()
    {
        if (DataContext is not EMapViewModel vm) return;

        // Xóa các phần tử node động cũ (giữ nguyên các phần tử blueprint nền)
        var oldNodes = MapCanvas.Children.Where(c => c.Tag as string == MapNodeTag).ToList();
        foreach (var old in oldNodes)
        {
            MapCanvas.Children.Remove(old);
        }

        double width = MapCanvas.Bounds.Width;
        double height = MapCanvas.Bounds.Height;

        if (width <= 0 || height <= 0) return;

        foreach (var node in vm.PlacedNodes)
        {
            double x = (node.XPercent / 100.0) * width;
            double y = (node.YPercent / 100.0) * height;
            var cam = vm.AvailableCameras.FirstOrDefault(c => c.Id == node.CameraId);

            // 1. Vẽ quạt quét FOV (Field of view cone)
            var fovCone = new Polygon
            {
                Tag = MapNodeTag,
                Points = new Points { new Point(0, 0), new Point(35, -25), new Point(45, 0), new Point(35, 25) },
                Fill = Brush.Parse("#303B82F6"),
                Stroke = Brush.Parse("#603B82F6"),
                StrokeThickness = 1,
                IsHitTestVisible = false
            };
            Canvas.SetLeft(fovCone, x);
            Canvas.SetTop(fovCone, y);
            MapCanvas.Children.Add(fovCone);

            // 2. Nhãn tên camera bên dưới
            var label = new TextBlock
            {
                Tag = MapNodeTag,
                Text = node.CameraName,
                FontSize = 10,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White,
                Background = Brush.Parse("#B0000000"),
                Padding = new Thickness(5, 2),
                IsHitTestVisible = false
            };
            Canvas.SetLeft(label, x - 24);
            Canvas.SetTop(label, y + 18);
            MapCanvas.Children.Add(label);

            // 3. Nút Camera Pin tương tác (Hỗ trợ kéo-thả để đổi vị trí + nhấp để xem trực tiếp + chuột phải để xóa)
            bool isSelected = vm.SelectedCamera?.Id == node.CameraId;
            var pinBorder = new Border
            {
                Tag = MapNodeTag,
                Background = Brush.Parse(isSelected ? "#2563EB" : "#F05922"),
                BorderBrush = Brushes.White,
                BorderThickness = new Thickness(isSelected ? 2 : 1),
                Padding = new Thickness(7, 4),
                CornerRadius = new CornerRadius(14),
                Cursor = new Cursor(StandardCursorType.Hand),
                Child = new TextBlock
                {
                    Text = "📹",
                    FontSize = 13,
                    Foreground = Brushes.White,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            };
            ToolTip.SetTip(pinBorder, $"Camera: {node.CameraName}\n• Nhấp trái: Xem trực tiếp\n• Nhấn giữ & kéo: Thay đổi vị trí\n• Nhấn nút ✕ hoặc chuột phải: Xóa khỏi bản đồ");

            // Context menu chuột phải trên camera pin
            var contextMenu = new ContextMenu();
            var previewItem = new MenuItem { Header = "📺 Xem trực tiếp" };
            previewItem.Click += (_, _) =>
            {
                if (cam != null) OpenPreview(cam);
            };
            var removeItem = new MenuItem { Header = "🗑️ Xóa camera khỏi bản đồ" };
            removeItem.Click += (_, _) =>
            {
                vm.RemoveNodeByCameraId(node.CameraId, node.CameraName);
            };
            contextMenu.Items.Add(previewItem);
            contextMenu.Items.Add(removeItem);
            pinBorder.ContextMenu = contextMenu;

            // 4. Nút xóa nhanh (✕) gắn góc trên bên phải của Camera Pin
            var deleteBadge = new Border
            {
                Tag = MapNodeTag,
                Width = 16,
                Height = 16,
                CornerRadius = new CornerRadius(8),
                Background = Brush.Parse("#EF4444"),
                BorderBrush = Brushes.White,
                BorderThickness = new Thickness(1),
                Cursor = new Cursor(StandardCursorType.Hand),
                Child = new TextBlock
                {
                    Text = "✕",
                    FontSize = 9,
                    FontWeight = FontWeight.Bold,
                    Foreground = Brushes.White,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            };
            ToolTip.SetTip(deleteBadge, $"Xóa {node.CameraName} khỏi bản đồ");
            deleteBadge.PointerPressed += (_, ev) =>
            {
                if (ev.GetCurrentPoint(deleteBadge).Properties.IsLeftButtonPressed)
                {
                    ev.Handled = true;
                    vm.RemoveNodeByCameraId(node.CameraId, node.CameraName);
                }
            };

            // Xử lý Kéo-Thả (Drag) để đổi vị trí hoặc Nhấp (Click) để xem trực tiếp
            pinBorder.PointerPressed += (_, ev) =>
            {
                if (ev.GetCurrentPoint(MapCanvas).Properties.IsLeftButtonPressed)
                {
                    ev.Handled = true;
                    if (cam != null)
                    {
                        vm.SelectedCamera = cam;
                    }
                    _draggingNode = node;
                    _dragStartPoint = ev.GetPosition(MapCanvas);
                    _isDraggingNode = false;
                    ev.Pointer.Capture(pinBorder);
                }
            };

            pinBorder.PointerMoved += (_, ev) =>
            {
                if (_draggingNode != node) return;

                var currentPos = ev.GetPosition(MapCanvas);
                if (!_isDraggingNode)
                {
                    double dx = currentPos.X - _dragStartPoint.X;
                    double dy = currentPos.Y - _dragStartPoint.Y;
                    if (Math.Sqrt(dx * dx + dy * dy) > 4.0)
                    {
                        _isDraggingNode = true;
                    }
                }

                if (_isDraggingNode)
                {
                    ev.Handled = true;
                    double canvasW = MapCanvas.Bounds.Width;
                    double canvasH = MapCanvas.Bounds.Height;
                    if (canvasW > 0 && canvasH > 0)
                    {
                        double nx = Math.Clamp(currentPos.X, canvasW * 0.03, canvasW * 0.97);
                        double ny = Math.Clamp(currentPos.Y, canvasH * 0.03, canvasH * 0.97);

                        node.XPercent = (nx / canvasW) * 100.0;
                        node.YPercent = (ny / canvasH) * 100.0;

                        Canvas.SetLeft(fovCone, nx);
                        Canvas.SetTop(fovCone, ny);
                        Canvas.SetLeft(pinBorder, nx - 15);
                        Canvas.SetTop(pinBorder, ny - 12);
                        Canvas.SetLeft(deleteBadge, nx + 7);
                        Canvas.SetTop(deleteBadge, ny - 18);
                        Canvas.SetLeft(label, nx - 24);
                        Canvas.SetTop(label, ny + 18);

                        vm.StatusMessage = $"Đang di chuyển {node.CameraName} tới ({node.XPercent:F0}%, {node.YPercent:F0}%)...";
                    }
                }
            };

            pinBorder.PointerReleased += (_, ev) =>
            {
                if (_draggingNode != node) return;

                ev.Pointer.Capture(null);
                ev.Handled = true;

                bool wasDragging = _isDraggingNode;
                _draggingNode = null;
                _isDraggingNode = false;

                if (wasDragging)
                {
                    vm.UpdateNodePosition(node.CameraId, node.XPercent, node.YPercent);
                }
                else if (cam != null)
                {
                    OpenPreview(cam);
                }
            };

            Canvas.SetLeft(pinBorder, x - 15);
            Canvas.SetTop(pinBorder, y - 12);
            MapCanvas.Children.Add(pinBorder);

            Canvas.SetLeft(deleteBadge, x + 7);
            Canvas.SetTop(deleteBadge, y - 18);
            MapCanvas.Children.Add(deleteBadge);
        }
    }

    private void OnMapCanvasPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(MapCanvas).Properties.IsLeftButtonPressed) return;

        if (DataContext is EMapViewModel vm)
        {
            var pos = e.GetPosition(MapCanvas);
            double width = MapCanvas.Bounds.Width;
            double height = MapCanvas.Bounds.Height;

            if (width > 0 && height > 0)
            {
                double xPct = (pos.X / width) * 100.0;
                double yPct = (pos.Y / height) * 100.0;
                vm.PlaceCameraOnMap(xPct, yPct);
            }
        }
    }

    private void OnRemoveFromPopupClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is EMapViewModel vm && !string.IsNullOrEmpty(_previewingCameraId))
        {
            var camId = _previewingCameraId;
            ClosePopup();
            vm.RemoveNodeByCameraId(camId);
        }
    }

    private async void OnLoadMapImageClick(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel?.StorageProvider == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Chọn hình ảnh sơ đồ mặt bằng (E-Map)",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                FilePickerFileTypes.ImageAll
            }
        });

        if (files.Count > 0)
        {
            try
            {
                var localPath = files[0].TryGetLocalPath() ?? files[0].Path.LocalPath;
                await using var stream = await files[0].OpenReadAsync();
                CustomMapImage.Source = new Bitmap(stream);
                CustomMapImage.IsVisible = true;
                MapCanvas.Background = Brushes.Transparent;

                foreach (var child in MapCanvas.Children)
                {
                    if (child.Tag as string != MapNodeTag)
                    {
                        child.IsVisible = false;
                    }
                }

                if (DataContext is EMapViewModel vm)
                {
                    vm.MapImagePath = localPath;
                    vm.MapTitle = $"Sơ đồ mặt bằng: {files[0].Name}";
                    vm.StatusMessage = $"Đã tải sơ đồ mặt bằng \"{files[0].Name}\". Nhấn [Lưu cấu hình] để lưu lại vào hệ thống.";
                }
            }
            catch (Exception ex)
            {
                if (DataContext is EMapViewModel vm)
                {
                    vm.StatusMessage = $"Không thể tải ảnh mặt bằng: {ex.Message}";
                }
            }
        }
    }

    private void OpenPreview(CameraDevice camera)
    {
        _previewingCameraId = camera.Id;
        TxtPopupTitle.Text = $"TRỰC TIẾP: {camera.Name}";
        PopupPreview.IsVisible = true;
        PopupPlayer.PlayCamera(camera, useMainStream: false);
    }

    private void ClosePopup()
    {
        _previewingCameraId = null;
        PopupPlayer.StopCamera();
        PopupPreview.IsVisible = false;
    }
}
