// G4.4: Port từ TrackingObjects/ObjectBox.cs (WinForms).
// Thay đổi: Rectangle BBox → KzRect BBox (ADR §6.3); xoá using System.Drawing.
// KzRect định nghĩa tại Objects/KzRect.cs (G4.1), namespace Kztek.Cameras.

namespace Kztek.Cameras.TrackingObjects
{
    public class ObjectBox
    {
        public string Id { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        /// <summary>
        /// Vùng bounding box — thay System.Drawing.Rectangle bằng KzRect (cross-platform, ADR §6.3).
        /// </summary>
        public KzRect BBox { get; set; }
        public string Name { get; set; } = string.Empty;
        public EmObjectBoxStatus Status { get; set; }
        /// <summary>
        /// ID frame khi AI/LPR phát hiện box này (từ FrameMeta.FrameId).
        /// Dùng để đồng bộ với _currentFrameId trong AnvPlayer: nếu chênh lệch quá lớn thì ẩn box.
        /// Mặc định 0 = chưa gán (box từ luồng không có FrameId tracking).
        /// </summary>
        public long FrameId { get; set; }
        /// <summary>
        /// Environment.TickCount64 khi frame gốc được decode (từ FrameMeta.CapturedAtTicks).
        /// Dùng để tính độ trễ thực tế tính bằng ms.
        /// </summary>
        public long CapturedAtTicks { get; set; }
    }

    public enum EmObjectBoxStatus
    {
        UNKNOWN,
        ENTRY,
        ENTRIED,
        EXIT,
        EXITED,
    }
}
