using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Media;
using CMS.App.ViewModels;
using CMS.Core.Entities;

namespace CMS.App.Views;

public partial class EMapView : UserControl
{
    public EMapView()
    {
        InitializeComponent();

        MapCanvas.PointerPressed += OnMapCanvasPointerPressed;
        BtnClosePopup.Click += (_, _) => ClosePopup();
        this.DataContextChanged += OnDataContextChanged;
        this.SizeChanged += (_, _) => RedrawNodes();
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is EMapViewModel vm)
        {
            vm.PlacedNodes.CollectionChanged += (_, _) => RedrawNodes();
            RedrawNodes();
        }
    }

    private void RedrawNodes()
    {
        if (DataContext is not EMapViewModel vm) return;

        // Xóa các node cũ (giữ lại các blueprint background cố định ở 6 phần tử đầu)
        while (MapCanvas.Children.Count > 6)
        {
            MapCanvas.Children.RemoveAt(MapCanvas.Children.Count - 1);
        }

        double width = MapCanvas.Bounds.Width;
        double height = MapCanvas.Bounds.Height;

        if (width <= 0 || height <= 0) return;

        foreach (var node in vm.PlacedNodes)
        {
            double x = (node.XPercent / 100.0) * width;
            double y = (node.YPercent / 100.0) * height;

            // 1. Vẽ quạt quét FOV (Field of view cone)
            var fovCone = new Polygon
            {
                Points = new Points { new Point(0, 0), new Point(35, -25), new Point(45, 0), new Point(35, 25) },
                Fill = Brush.Parse("#303B82F6"),
                Stroke = Brush.Parse("#603B82F6"),
                StrokeThickness = 1
            };
            Canvas.SetLeft(fovCone, x);
            Canvas.SetTop(fovCone, y);
            MapCanvas.Children.Add(fovCone);

            // 2. Vẽ nút Camera Pin tương tác
            var pinButton = new Button
            {
                Content = "📹",
                Background = Brush.Parse("#F05922"),
                Foreground = Brushes.White,
                Padding = new Thickness(6, 4),
                CornerRadius = new CornerRadius(14),
                Cursor = new Cursor(StandardCursorType.Hand)
            };
            ToolTip.SetTip(pinButton, $"Camera: {node.CameraName} (Nhấn để xem trực tiếp)");

            var cam = vm.AvailableCameras.FirstOrDefault(c => c.Id == node.CameraId);

            pinButton.Click += (_, _) =>
            {
                if (cam != null)
                {
                    OpenPreview(cam);
                }
            };

            Canvas.SetLeft(pinButton, x - 15);
            Canvas.SetTop(pinButton, y - 12);
            MapCanvas.Children.Add(pinButton);

            // 3. Nhãn tên camera bên dưới
            var label = new TextBlock
            {
                Text = node.CameraName,
                FontSize = 10,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White,
                Background = Brush.Parse("#A0000000"),
                Padding = new Thickness(4, 1)
            };
            Canvas.SetLeft(label, x - 20);
            Canvas.SetTop(label, y + 16);
            MapCanvas.Children.Add(label);
        }
    }

    private void OnMapCanvasPointerPressed(object? sender, PointerPressedEventArgs e)
    {
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

    private void OpenPreview(CameraDevice camera)
    {
        TxtPopupTitle.Text = $"TRỰC TIẾP: {camera.Name}";
        PopupPreview.IsVisible = true;
        PopupPlayer.PlayCamera(camera, useMainStream: false);
    }

    private void ClosePopup()
    {
        PopupPlayer.StopCamera();
        PopupPreview.IsVisible = false;
    }
}
