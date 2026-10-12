// G4.3: Port từ Players/FFMPEG/UserControls/FrameMotionDetector.cs (WinForms).
// Thay đổi:
//   - ProcessFrame(Bitmap frame) → ProcessFrame(SKBitmap frame)
//   - BitmapConverter.ToMat(frame) [OpenCvSharp.Extensions, cần System.Drawing.Bitmap] →
//     new Mat(h, w, MatType.CV_8UC4, frame.GetPixels(), (long)frame.RowBytes) [raw pointer, cross-platform]
//   - ColorConversionCodes.BGR2GRAY → ColorConversionCodes.BGRA2GRAY (SKBitmap Bgra8888 có 4 kênh)
//   - Bỏ: using OpenCvSharp.Extensions; using System.Drawing;
//   - Không thêm [SupportedOSPlatform] — OpenCvSharp4 là cross-platform.
// Behavior parity: thuật toán frame differencing (Absdiff + Threshold + Erode) giữ nguyên hoàn toàn.
// Cần package: OpenCvSharp4 (KHÔNG thêm OpenCvSharp4.Extensions).
using Avalonia.Media.Imaging;
using OpenCvSharp;

namespace Kztek.Cameras
{
    /// <summary>
    /// Phát hiện chuyển động bằng cách so sánh khung hình hiện tại với khung hình liền trước
    /// (frame differencing), thay thế <c>AForge.Vision.Motion.MotionDetector</c> +
    /// <c>TwoFramesDifferenceDetector</c> (AForge đã ngừng maintain từ ~2013).
    ///
    /// Thuật toán: chuyển 2 khung sang grayscale, lấy trị tuyệt đối hiệu (Absdiff), nhị phân hoá
    /// theo <see cref="DifferenceThreshold"/> (giữ nguyên giá trị mặc định 15 của AForge), rồi Erode
    /// 1 lần để khử nhiễu (tương đương tham số <c>suppressNoise: true</c> khi khởi tạo
    /// TwoFramesDifferenceDetector trong code cũ). Không phải port 1:1 bit-identical với AForge,
    /// chỉ tương đương về ý tưởng — do đó <c>MotionAlarmLevel</c> hiện có (mặc định 15, cấu hình theo
    /// hành vi AForge cũ) cần QA xác nhận lại độ nhạy trên camera/video thật trước khi tin tưởng production.
    /// </summary>
    internal sealed class FrameMotionDetector : IDisposable
    {
        private const double DifferenceThreshold = 15;

        private Mat _previousGray;

        /// <summary>
        /// Trả về mức độ chuyển động trong khoảng [0, 1] — tỉ lệ pixel thay đổi so với khung trước.
        /// Khung đầu tiên (chưa có khung trước để so sánh) luôn trả về 0, giống hành vi
        /// TwoFramesDifferenceDetector của AForge ở lần gọi ProcessFrame() đầu tiên.
        ///
        /// C2 fix (RAM leak BUG-004, 2026-07-20): nhận <see cref="WriteableBitmap"/> (Avalonia) thay
        /// SKBitmap — đã xác nhận SKBitmap rò rỉ native memory ở tần suất decode cao; WriteableBitmap
        /// ghi/đọc qua Lock() không leak (xem AvaloniaFramePool.cs). OpenCV chỉ cần con trỏ pixel thô +
        /// stride nên không phụ thuộc loại bitmap cụ thể.<br/>
        /// Do input có 4 kênh (BGRA), dùng BGRA2GRAY thay vì BGR2GRAY để chuyển màu đúng.
        /// </summary>
        public float ProcessFrame(WriteableBitmap frame)
        {
            if (frame == null)
            {
                return 0f;
            }

            // Mat.FromPixelData thay new Mat(h, w, type, nint, long) — constructor đó bị đánh dấu
            // [Obsolete error] trong OpenCvSharp4 v4.13+ do 'nint' gây overload resolution nhầm lẫn.
            // FromPixelData nhận IntPtr data + long step, không sở hữu memory (KHÔNG dispose khi còn dùng frame).
            // Lock() giữ trong suốt scope Mat sử dụng — Mat không sở hữu buffer, chỉ đọc qua con trỏ.
            using var fb = frame.Lock();
            using Mat current = Mat.FromPixelData(frame.PixelSize.Height, frame.PixelSize.Width, MatType.CV_8UC4,
                                                   fb.Address, fb.RowBytes);
            Mat currentGray = new Mat();
            Cv2.CvtColor(current, currentGray, ColorConversionCodes.BGRA2GRAY);

            if (_previousGray == null)
            {
                _previousGray = currentGray;
                return 0f;
            }

            float motionLevel = 0f;
            if (_previousGray.Size() == currentGray.Size())
            {
                using Mat diff = new Mat();
                Cv2.Absdiff(currentGray, _previousGray, diff);
                Cv2.Threshold(diff, diff, DifferenceThreshold, 255, ThresholdTypes.Binary);
                // BUG-004 Fix 6: dispose kernel Mat ngay lập tức thay vì để GC.
                // new Mat() không dispose → finalizer queue nhận ~180 objects/s (6 cameras × 30fps)
                // → GC pressure → intermittent UI pauses.
                using var erodeKernel = new Mat();
                Cv2.Erode(diff, diff, erodeKernel);

                motionLevel = (float)Cv2.CountNonZero(diff) / (diff.Width * diff.Height);
            }

            _previousGray.Dispose();
            _previousGray = currentGray;

            return motionLevel;
        }

        public void Dispose()
        {
            _previousGray?.Dispose();
            _previousGray = null;
        }
    }
}
