using SkiaSharp;
using System.Collections.Concurrent;

namespace Kztek.Cameras
{
    /// <summary>
    /// Pool for reusing SKBitmap objects to reduce GC pressure.
    /// </summary>
    public static class SKBitmapPool
    {
        private static readonly ConcurrentBag<SKBitmap> _pool = new();

        public static SKBitmap Rent(int width, int height, SKColorType colorType = SKColorType.Bgra8888)
        {
            if (_pool.TryTake(out var bmp))
            {
                if (bmp.Width == width && bmp.Height == height && bmp.ColorType == colorType)
                {
                    return bmp;
                }
                bmp.Dispose();
            }

            return new SKBitmap(new SKImageInfo(width, height, colorType));
        }

        public static void Return(SKBitmap? bitmap)
        {
            if (bitmap == null) return;

            if (_pool.Count < 32)
            {
                _pool.Add(bitmap);
            }
            else
            {
                bitmap.Dispose();
            }
        }
    }
}
