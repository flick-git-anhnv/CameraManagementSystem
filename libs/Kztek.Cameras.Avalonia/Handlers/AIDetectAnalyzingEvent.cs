// Fix (2026-07-20, loại bỏ SkiaSharp toàn thư viện): SKBitmap → Avalonia WriteableBitmap.
using System;
using Avalonia.Media.Imaging;
using Kztek.Cameras.AiDetection;

namespace Kztek.Cameras.Handlers
{
    public class AIDetectAnalyzingEventArgs : EventArgs
    {
        public PredictionResult status { get; private set; }
        public string boxName { get; private set; }
        public string cameraId { get; private set; }
        /// <summary>
        /// ID frame khi AI phát hiện sự kiện (từ FrameMeta.FrameId).
        /// Dùng để đồng bộ với _currentFrameId trong AnvPlayer ở bước 2.3.
        /// Mặc định 0 = chưa gán (backward compatible với constructor cũ không truyền FrameId).
        /// </summary>
        public long FrameId { get; private set; }
        /// <summary>
        /// Environment.TickCount64 khi frame gốc được decode (từ FrameMeta.CapturedAtTicks).
        /// Dùng để tính độ trễ thực tế tính bằng ms ở bước 2.3.
        /// </summary>
        public long CapturedAtTicks { get; private set; }

        public AIDetectAnalyzingEventArgs(PredictionResult status, string boxName, string cameraId,
            long frameId = 0, long capturedAtTicks = 0)
        {
            this.status = status;
            this.boxName = boxName;
            this.cameraId = cameraId;
            this.FrameId = frameId;
            this.CapturedAtTicks = capturedAtTicks;
        }
    }

    public delegate void AIDetectAnalyzingEventHandler(object sender, AIDetectAnalyzingEventArgs e, WriteableBitmap? eventImage);
}
