using Avalonia.Controls;
using CMS.Core.Entities;
using CMS.Core.Enums;
using CMS.Drivers;
using KztekComponentAvalonia.Theme;
using System.ComponentModel;

namespace CMS.App.Controls;

public partial class PtzControlView : UserControl
{
    private CameraDevice? _camera;

    public event EventHandler<bool>? PtzSupportChanged;

    public bool IsPtzAvailable => _camera != null && _camera.HasPtz;

    public CameraDevice? TargetCamera
    {
        get => _camera;
        set
        {
            if (_camera != null)
            {
                _camera.PropertyChanged -= OnTargetCameraPropertyChanged;
            }

            _camera = value;

            if (_camera != null)
            {
                _camera.PropertyChanged += OnTargetCameraPropertyChanged;
            }

            UpdatePtzState();
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
            if (_camera == null || !_camera.HasPtz) return;
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
            if (_camera == null || !_camera.HasPtz) return;
            try
            {
                var driver = CameraDriverFactory.GetDriver(_camera.Brand);
                int preset = (int)(NumPreset.Value ?? 1);
                await driver.PresetAsync(_camera, "set", preset);
            }
            catch { }
        };

        UpdatePtzState();
    }

    private void OnTargetCameraPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CameraDevice.HasPtz) ||
            e.PropertyName == nameof(CameraDevice.Name) ||
            e.PropertyName == nameof(CameraDevice.IpAddress))
        {
            UpdatePtzState();
        }
    }

    public void UpdatePtzState()
    {
        bool hasPtz = _camera != null && _camera.HasPtz;

        if (TxtTargetCamera != null)
        {
            TxtTargetCamera.Text = _camera != null ? $"{_camera.Name} ({_camera.IpAddress})" : "Chưa chọn camera";
        }

        if (BadgePtzStatus != null)
        {
            BadgePtzStatus.Content = hasPtz ? "🕹️ Có PTZ" : "📷 Cố định";
            BadgePtzStatus.Variant = hasPtz ? KzBadgeVariant.Success : KzBadgeVariant.Neutral;
        }

        if (PtzInteractivePanel != null)
        {
            PtzInteractivePanel.IsEnabled = hasPtz;
            PtzInteractivePanel.Opacity = hasPtz ? 1.0 : 0.4;
        }

        if (PtzUnavailableBanner != null)
        {
            PtzUnavailableBanner.IsVisible = !hasPtz;
        }

        if (TxtPtzUnavailableReason != null)
        {
            TxtPtzUnavailableReason.Text = _camera == null
                ? "Chưa chọn camera để điều khiển PTZ"
                : "📷 Camera cố định — Không sử dụng điều khiển PTZ";
        }

        PtzSupportChanged?.Invoke(this, hasPtz);
    }

    private void BindPtzButton(Button btn, PtzCommand cmd)
    {
        long pressStartTicks = 0;

        btn.PointerPressed += async (_, _) =>
        {
            pressStartTicks = DateTime.UtcNow.Ticks;
            await SendCommandAsync(cmd);
        };

        btn.PointerReleased += async (_, _) =>
        {
            await SendCommandAsync(PtzCommand.Stop);
        };

        btn.PointerCaptureLost += async (_, _) =>
        {
            await SendCommandAsync(PtzCommand.Stop);
        };

        btn.Click += async (_, _) =>
        {
            long elapsedMs = (DateTime.UtcNow.Ticks - pressStartTicks) / TimeSpan.TicksPerMillisecond;
            if (elapsedMs < 200)
            {
                await SendCommandAsync(cmd);
                await Task.Delay(350);
                await SendCommandAsync(PtzCommand.Stop);
            }
        };
    }

    private async Task SendCommandAsync(PtzCommand cmd)
    {
        if (_camera == null || !_camera.HasPtz) return;
        try
        {
            var driver = CameraDriverFactory.GetDriver(_camera.Brand);
            double speed = SliderSpeed.Value;
            bool ok = await driver.SendPtzCommandAsync(_camera, cmd, speed);
            if (cmd != PtzCommand.Stop)
            {
                TxtTargetCamera.Text = $"{_camera.Name} • {(ok ? "✓" : "!")} {cmd}";
            }
            else
            {
                TxtTargetCamera.Text = $"{_camera.Name} ({_camera.IpAddress})";
            }
        }
        catch
        {
        }
    }
}
