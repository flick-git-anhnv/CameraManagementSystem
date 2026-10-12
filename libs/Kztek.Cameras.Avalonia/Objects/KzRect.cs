// G4.1: KzRect, KzPoint, KzSize — struct trung lập thay System.Drawing.Rectangle/Point/Size
// Không phụ thuộc Avalonia hoặc System.Drawing — service layer có thể test không cần UI.
// Dùng nhất quán trong toàn bộ business-logic / handler layer của Kztek.Cameras.Avalonia.
// Xem ADR-001-migration-strategy.md §6.3 cho quyết định thiết kế.

namespace Kztek.Cameras
{
    /// <summary>
    /// Struct trung lập thay <c>System.Drawing.Rectangle</c> trong business logic layer.
    /// Không phụ thuộc Avalonia — có thể dùng trong service / handler không cần UI.
    /// </summary>
    public struct KzRect
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        public KzRect(int x, int y, int width, int height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public int Right => X + Width;
        public int Bottom => Y + Height;
        public bool IsEmpty => Width == 0 && Height == 0;

        public static KzRect Empty => new KzRect(0, 0, 0, 0);

        public bool Contains(int px, int py) =>
            px >= X && px < Right && py >= Y && py < Bottom;

        public bool Contains(KzPoint p) => Contains(p.X, p.Y);

        public bool IntersectsWith(KzRect other) =>
            other.X < Right && X < other.Right && other.Y < Bottom && Y < other.Bottom;

        public override string ToString() =>
            $"{{X={X}, Y={Y}, Width={Width}, Height={Height}}}";

        public override bool Equals(object? obj) =>
            obj is KzRect r && r.X == X && r.Y == Y && r.Width == Width && r.Height == Height;

        public override int GetHashCode() => HashCode.Combine(X, Y, Width, Height);

        public static bool operator ==(KzRect a, KzRect b) =>
            a.X == b.X && a.Y == b.Y && a.Width == b.Width && a.Height == b.Height;

        public static bool operator !=(KzRect a, KzRect b) => !(a == b);
    }

    /// <summary>
    /// Struct trung lập thay <c>System.Drawing.Point</c> trong business logic layer.
    /// </summary>
    public struct KzPoint
    {
        public int X { get; set; }
        public int Y { get; set; }

        public KzPoint(int x, int y) { X = x; Y = y; }

        public static KzPoint Empty => new KzPoint(0, 0);

        public override string ToString() => $"{{X={X}, Y={Y}}}";

        public override bool Equals(object? obj) =>
            obj is KzPoint p && p.X == X && p.Y == Y;

        public override int GetHashCode() => HashCode.Combine(X, Y);

        public static bool operator ==(KzPoint a, KzPoint b) => a.X == b.X && a.Y == b.Y;
        public static bool operator !=(KzPoint a, KzPoint b) => !(a == b);
    }

    /// <summary>
    /// Struct trung lập thay <c>System.Drawing.Size</c> trong business logic layer.
    /// </summary>
    public struct KzSize
    {
        public int Width { get; set; }
        public int Height { get; set; }

        public KzSize(int width, int height) { Width = width; Height = height; }

        public static KzSize Empty => new KzSize(0, 0);
        public bool IsEmpty => Width == 0 && Height == 0;

        public override string ToString() => $"{{Width={Width}, Height={Height}}}";

        public override bool Equals(object? obj) =>
            obj is KzSize s && s.Width == Width && s.Height == Height;

        public override int GetHashCode() => HashCode.Combine(Width, Height);

        public static bool operator ==(KzSize a, KzSize b) => a.Width == b.Width && a.Height == b.Height;
        public static bool operator !=(KzSize a, KzSize b) => !(a == b);
    }
}
