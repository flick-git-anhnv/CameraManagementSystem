// G4.4: Port từ CameraStreamInfo.cs (WinForms).
// Thay đổi: Size DesiredFrameSize → KzSize (ADR §6.3); xoá using System.Drawing + usings thừa.

namespace Kztek.Cameras
{
    public class CameraStreamInfo
    {
        public string Source { get; set; }

        public int HttpPort { get; set; }

        public int ServerPort { get; set; }

        public string Login { get; set; }

        public string Password { get; set; }

        public int DesiredFrameRate { get; set; }

        /// <summary>
        /// Kích thước frame mong muốn — thay System.Drawing.Size bằng KzSize (cross-platform).
        /// </summary>
        public KzSize DesiredFrameSize { get; set; }

        public int FramesReceived { get; set; }

        public long BytesReceived { get; set; }

        public bool IsRunning { get; }
    }
}
