// Avalonia port: ucCameraViewWithName (WinForms UserControl) → UserControl (Avalonia).
//
// Source: Players/ucCameraViewWithName.cs + .Designer.cs (WinForms, 26+60 dòng)
// Target: Players/ucCameraViewWithName.axaml + .axaml.cs (Avalonia)
//
// Behavior parity:
//   1. Constructor nhận Camera — set label tên, start camera nếu chưa chạy,
//      gắn videoSourcePlayer (AnvPlayer) vào visual tree.
//   2. KzLabel (Text) thay WinForms Label (Text) — Foreground="Black" parity ForeColor=Black.
//   3. _playerContainer (Panel) + Stretch thay Controls.Add + DockStyle.Fill.
//   4. Panel overlap trong Grid — KzLabel luôn render trên cùng (parity BringToFront).

using Avalonia.Controls;
using Avalonia.Layout;

namespace Kztek.Cameras
{
    /// <summary>
    /// UserControl hiển thị stream camera với label tên camera phía dưới-trái.
    /// Avalonia port của <c>Players/ucCameraViewWithName</c> (WinForms).
    /// </summary>
    public partial class ucCameraViewWithName : UserControl
    {
        public ucCameraViewWithName(Camera camera)
        {
            InitializeComponent();

            // Parity: lblCameraName.Text = camera.Description
            _lblCameraName.Text = camera.Description;

            // Parity: if (camera.videoSourcePlayer is null) camera.Start(0, 0, 0)
            // Avalonia Camera.Start: (double fromMotion, double toMotion, int interval, ...)
            if (camera.videoSourcePlayer is null)
            {
                camera.Start(0.0, 0.0, 0);
            }

            // Parity: var uc = (Control)camera.videoSourcePlayer;
            //         this.Controls.Add(uc); uc.Dock = DockStyle.Fill;
            //         lblCameraName.BringToFront();
            // → _playerContainer (Panel) + Grid overlap đảm bảo KzLabel luôn ở trên
            if (camera.videoSourcePlayer is Control playerControl)
            {
                playerControl.HorizontalAlignment = HorizontalAlignment.Stretch;
                playerControl.VerticalAlignment = VerticalAlignment.Stretch;
                _playerContainer.Children.Add(playerControl);
            }
        }

        /// <summary>Tên camera hiển thị trên label.</summary>
        public string? CameraName
        {
            get => _lblCameraName.Text;
            set => _lblCameraName.Text = value;
        }
    }
}
