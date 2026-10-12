// Fix (2026-07-20, loại bỏ SkiaSharp toàn thư viện): SKBitmap → Avalonia WriteableBitmap.
// Frame hiển thị/first-frame-detect nội bộ decoder giờ là WriteableBitmap xuyên suốt (đã áp dụng
// cho display/motion/AI channel từ BUG-004 C2) — event NewFrame theo luôn, không còn convert SKBitmap.
using Avalonia.Media.Imaging;

namespace Kztek.Cameras
{
    public delegate void NewFrameHandler(object sender, ref WriteableBitmap? image);
}
