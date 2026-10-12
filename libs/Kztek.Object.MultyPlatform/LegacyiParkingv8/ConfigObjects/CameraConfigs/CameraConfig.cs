using static Kztek.Object.MultyPlatform.ConfigObjects.VirtualLoopType;

namespace iParkingv8.Object.ConfigObjects.CameraConfigs
{
    public class CameraConfig
    {
        public CameraLprConfig? LprConfig { get; set; }
        public CameraLoopConfig? LoopConfig { get; set; }
    }
    public class CameraLprConfig : CameraDetectRegion
    {
        public int RotateAngle { get; set; }
        /// <summary>
        /// Thời gian (giây) lưu lại biển số đã nhận dạng để bỏ qua các lần nhận dạng trùng lặp gần đây.
        /// </summary>
        public int DuplicatePlateSuppressSeconds { get; set; } = 10;
    }
    public class CameraLoopConfig : CameraDetectRegion
    {
        public int FromAlarmLevel { get; set; }
        public int ToAlarmLevel { get; set; }
        public EmVirtualLoopType VirtualLoopType { get; set; }
    }
}
