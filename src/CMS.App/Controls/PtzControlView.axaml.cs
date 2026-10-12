using Avalonia.Controls;
using CMS.Core.Entities;
using CMS.Core.Enums;
using CMS.Drivers;

namespace CMS.App.Controls;

public partial class PtzControlView : UserControl
{
    private CameraDevice? _camera;

    public CameraDevice? TargetCamera
    {
        get => _camera;
        set
        {
            _camera = value;
            TxtTargetCamera.Text = _camera != null ? _camera.Name : "Chưa chọn camera";
        }
    }

    public PtzControlView()
    {
        InitializeComponent();

        SliderSpeed.PropertyChanged += (_, e) =>
        {
            if (e.Property.Name == nameof(Slider.Value))
            {
                TxtSpeedVal.Text = SliderSpeed.Value.ToString("F1");
            }
        };

        // Wire PTZ Buttons
        BindPtzButton(BtnPtzUp, PtzCommand.Up);
        BindPtzButton(BtnPtzDown, PtzCommand.Down);
        BindPtzButton(BtnPtzLeft, PtzCommand.Left);
        BindPtzButton(BtnPtzRight, PtzCommand.Right);
        BindPtzButton(BtnPtzUpLeft, PtzCommand.UpLeft);
        BindPtzButton(BtnPtzUpRight, PtzCommand.UpRight);
        BindPtzButton(BtnPtzDownLeft, PtzCommand.DownLeft);
        BindPtzButton(BtnPtzDownRight, PtzCommand.DownRight);
        BindPtzButton(BtnZoomIn, PtzCommand.ZoomIn);
        BindPtzButton(BtnZoomOut, PtzCommand.ZoomOut);
        BindPtzButton(BtnFocusNear, PtzCommand.FocusNear);
        BindPtzButton(BtnFocusFar, PtzCommand.FocusFar);

        BtnPtzStop.Click += async (_, _) => await SendCommandAsync(PtzCommand.Stop);

        BtnCallPreset.Click += async (_, _) =>
        {
            if (_camera == null) return;
            try
            {
                var driver = CameraDriverFactory.GetDriver(_camera.Brand);
                int preset = (int)(NumPreset.Value ?? 1);
                await driver.PresetAsync(_camera, "goto", preset);
            }
            catch { }
        };

        BtnSetPreset.Click += async (_, _) =>
        {
            if (_camera == null) return;
            try
            {
                var driver = CameraDriverFactory.GetDriver(_camera.Brand);
                int preset = (int)(NumPreset.Value ?? 1);
                await driver.PresetAsync(_camera, "set", preset);
            }
            catch { }
        };
    }

    private void BindPtzButton(Button btn, PtzCommand cmd)
    {
        btn.PointerPressed += async (_, _) => await SendCommandAsync(cmd);
        btn.PointerReleased += async (_, _) => await SendCommandAsync(PtzCommand.Stop);
    }

    private async Task SendCommandAsync(PtzCommand cmd)
    {
        if (_camera == null) return;
        try
        {
            var driver = CameraDriverFactory.GetDriver(_camera.Brand);
            double speed = SliderSpeed.Value;
            await driver.SendPtzCommandAsync(_camera, cmd, speed);
        }
        catch
        {
        }
    }
}
