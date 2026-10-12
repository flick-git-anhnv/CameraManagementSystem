// Fix (2026-07-20, loại bỏ SkiaSharp toàn thư viện): SKBitmap → Avalonia WriteableBitmap.
// VehicleImage (frame gốc) → WriteableBitmap. LprImage (ảnh biển số crop) → ReadPL.ImageTensorResult
// vì ReadPL 1.1.2 (tensor-based API) trả Plate.Crop kiểu này trực tiếp, không còn SKBitmap.
// GeneralEventArgs giữ nguyên từ Kztek.Object package (D11 ADR — cross-platform).
using System;
using Avalonia.Media.Imaging;
using iParkingv5.Objects.Events;
using ReadPL;

namespace Kztek.Cameras
{
    public class MotionAnalyzingEventArgs : GeneralEventArgs
    {
        public float MotionLevel { get; private set; }
        public WriteableBitmap? VehicleImage { get; set; }
        public ImageTensorResult? LprImage { get; set; }
        public string PlateNumber { get; set; }
        public MotionAnalyzingEventArgs(float motionLevel, WriteableBitmap? vehicleImage = null, ImageTensorResult? lprImage = null, string plateNumber = "")
        {
            MotionLevel = motionLevel;
            VehicleImage = vehicleImage;
            LprImage = lprImage;
            PlateNumber = plateNumber;
        }
    }
    public delegate void MotionAnalyzingEventHandler(object sender, MotionAnalyzingEventArgs eventArgs);
}
