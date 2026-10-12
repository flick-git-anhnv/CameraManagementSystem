// G4.4: Port từ TrackingObjects/TrackingConfig.cs (WinForms).
// Thay đổi:
//   - LineConfig.Start/End: Point → KzPoint (ADR §6.3).
//   - IsRectIntersect(Rectangle, ...) → IsRectIntersect(KzRect, ...).
//   - detectRect.Left/Top dùng KzRect.X/Y; Right/Bottom đã là property trong KzRect.
//   - detectRect.Contains(Point.Round(p)) → Contains(new KzPoint(round)).
//   - PointF nội bộ tính toán: giữ nguyên (System.Drawing.PointF có trong System.Drawing.Primitives,
//     cross-platform, không phụ thuộc GDI+/System.Drawing.Common).
using System;
using System.Drawing; // PointF — System.Drawing.Primitives, cross-platform, không cần System.Drawing.Common

namespace Kztek.Cameras.TrackingObjects
{
    public class TrackingConfig
    {
        public int PreferTrackingImageSize { get; set; } = 320;
        public LineConfig LineIN;
        public LineConfig LineINDirection;
        public LineConfig LineOUT;
        public LineConfig LineOUTDirection;
    }

    public class LineConfig
    {
        public EmLinePurpose Purpose { get; set; } = EmLinePurpose.IN;
        /// <summary>Điểm bắt đầu đường line — thay System.Drawing.Point bằng KzPoint.</summary>
        public KzPoint Start { get; set; }
        /// <summary>Điểm kết thúc đường line — thay System.Drawing.Point bằng KzPoint.</summary>
        public KzPoint End { get; set; }
        public int ConfigImageWidth { get; set; }
        public int ConfigImageHeight { get; set; }

        /// <summary>
        /// Kiểm tra line này có giao với detectRect không (có scale về cùng hệ tọa độ).
        /// </summary>
        public bool IsRectIntersect(KzRect detectRect, int detectImageWidth, int detectImageHeight)
        {
            if (ConfigImageWidth == 0 || ConfigImageHeight == 0)
                return false;

            // 1. Scale line về cùng hệ tọa độ detect
            float scaleX = (float)detectImageWidth / ConfigImageWidth;
            float scaleY = (float)detectImageHeight / ConfigImageHeight;

            PointF p1 = new PointF(Start.X * scaleX, Start.Y * scaleY);
            PointF p2 = new PointF(End.X * scaleX, End.Y * scaleY);

            // 2. Nếu 1 trong 2 điểm nằm trong rect → intersect
            // Thay Point.Round(p) bằng KzPoint(Round) vì Point.Round là System.Drawing API
            if (detectRect.Contains(new KzPoint((int)Math.Round(p1.X), (int)Math.Round(p1.Y))) ||
                detectRect.Contains(new KzPoint((int)Math.Round(p2.X), (int)Math.Round(p2.Y))))
                return true;

            // 3. Check cắt với 4 cạnh rect
            // KzRect: X=Left, Y=Top, Right=X+Width, Bottom=Y+Height (đã có property)
            PointF r1 = new PointF(detectRect.X,     detectRect.Y);
            PointF r2 = new PointF(detectRect.Right,  detectRect.Y);
            PointF r3 = new PointF(detectRect.Right,  detectRect.Bottom);
            PointF r4 = new PointF(detectRect.X,     detectRect.Bottom);

            if (LineIntersectsLine(p1, p2, r1, r2)) return true; // top
            if (LineIntersectsLine(p1, p2, r2, r3)) return true; // right
            if (LineIntersectsLine(p1, p2, r3, r4)) return true; // bottom
            if (LineIntersectsLine(p1, p2, r4, r1)) return true; // left

            return false;
        }

        private static bool LineIntersectsLine(PointF a1, PointF a2, PointF b1, PointF b2)
        {
            float d = (a2.X - a1.X) * (b2.Y - b1.Y) - (a2.Y - a1.Y) * (b2.X - b1.X);

            if (d == 0)
                return false; // song song

            float u = ((b1.X - a1.X) * (b2.Y - b1.Y) - (b1.Y - a1.Y) * (b2.X - b1.X)) / d;
            float v = ((b1.X - a1.X) * (a2.Y - a1.Y) - (b1.Y - a1.Y) * (a2.X - a1.X)) / d;

            return (u >= 0 && u <= 1 && v >= 0 && v <= 1);
        }
    }

    public enum EmLinePurpose
    {
        IN,
        OUT,
        IN_DIRECTION,
        OUT_DIRECTION
    }
}
