using Avalonia.Controls;
using Avalonia.Input;
using CMS.Core.Entities;

namespace CMS.App.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        CameraListBox.DoubleTapped += OnCameraDoubleTapped;
        LiveGrid.BtnAutoFill.Click += (_, _) => OnAutoFillClick();
    }

    private void OnCameraDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (CameraListBox.SelectedItem is CameraDevice cam)
        {
            LiveGrid.PlayCameraOnSelectedCell(cam);
        }
    }

    private void OnAutoFillClick()
    {
        if (DataContext is ViewModels.MainWindowViewModel vm)
        {
            LiveGrid.AutoPlayCameras(vm.ConfiguredCameras);
        }
    }
}
