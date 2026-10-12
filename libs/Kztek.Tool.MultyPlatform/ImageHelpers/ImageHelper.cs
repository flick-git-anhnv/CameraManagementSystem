using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace Kztek.Tool.ImageHelpers
{
    /// <summary>
    /// Image processing helper using Avalonia natively (no SkiaSharp dependency).
    /// </summary>
    public static class ImageHelper
    {
        // ─────────────────────────────────────────────────────────────
        // Load / Read
        // ─────────────────────────────────────────────────────────────
        public static Bitmap? GetCloneImageFromPath(string filePath)
        {
            if (!File.Exists(filePath)) return null;
            try
            {
                using var fs = File.OpenRead(filePath);
                return new Bitmap(fs);
            }
            catch { return null; }
        }

        public static async Task<Bitmap?> LoadBitmapFromUrl(string url)
        {
            try
            {
                using var http = new HttpClient();
                http.Timeout = TimeSpan.FromSeconds(10);
                var data = await http.GetByteArrayAsync(url);
                using var ms = new MemoryStream(data);
                return new Bitmap(ms);
            }
            catch { return null; }
        }

        // ─────────────────────────────────────────────────────────────
        // Base64 & Bytes
        // ─────────────────────────────────────────────────────────────
        public static string ImageToBase64(string imagePath)
        {
            if (!File.Exists(imagePath)) return string.Empty;
            try
            {
                return Convert.ToBase64String(File.ReadAllBytes(imagePath));
            }
            catch { return string.Empty; }
        }

        public static void ImageToBase64(this Bitmap? wb, out string base64, out int size, int quality = 90)
        {
            base64 = "";
            size = 0;
            if (wb == null) return;
            try
            {
                using var ms = new MemoryStream();
                wb.Save(ms, quality);
                var bytes = ms.ToArray();
                base64 = Convert.ToBase64String(bytes);
                size = bytes.Length;
            }
            catch { }
        }

        public static Bitmap? Base64ToImage(string base64Str)
        {
            if (string.IsNullOrEmpty(base64Str)) return null;
            try
            {
                var bytes = Convert.FromBase64String(base64Str);
                using var ms = new MemoryStream(bytes);
                return new Bitmap(ms);
            }
            catch { return null; }
        }

        public static async Task<List<byte>> ImageToByteArrayAsync(this Bitmap? imageIn, int quality = 90)
        {
            if (imageIn == null) return new List<byte>();
            return await Task.Run(() =>
            {
                using var ms = new MemoryStream();
                imageIn.Save(ms, quality);
                return ms.ToArray().ToList();
            });
        }

        public static byte[] BitmapToBytes(this Bitmap wb, int quality = 90)
        {
            using var ms = new MemoryStream();
            wb.Save(ms, quality);
            return ms.ToArray();
        }

        // ─────────────────────────────────────────────────────────────
        // Resize, Crop, Transform
        // ─────────────────────────────────────────────────────────────

        public static Bitmap? ResizeImage(this Bitmap? source, int targetWidth, int targetHeight)
        {
            if (source == null || targetWidth <= 0 || targetHeight <= 0) return null;
            var rtb = new RenderTargetBitmap(new PixelSize(targetWidth, targetHeight));
            using (var ctx = rtb.CreateDrawingContext())
            {
                ctx.DrawImage(source, new Rect(0, 0, source.Size.Width, source.Size.Height), new Rect(0, 0, targetWidth, targetHeight));
            }
            return rtb;
        }

        public static Bitmap? CropBitmap(this Bitmap? source, PixelRect cropRect)
        {
            if (source == null) return null;
            var bounded = cropRect.Intersect(new PixelRect(0, 0, source.PixelSize.Width, source.PixelSize.Height));
            if (bounded.Width <= 0 || bounded.Height <= 0) return null;
            
            var rtb = new RenderTargetBitmap(new PixelSize(bounded.Width, bounded.Height));
            using (var ctx = rtb.CreateDrawingContext())
            {
                ctx.DrawImage(source, new Rect(bounded.X, bounded.Y, bounded.Width, bounded.Height), new Rect(0, 0, bounded.Width, bounded.Height));
            }
            return rtb;
        }

        public static Bitmap? CropBitmap(this Bitmap? source, int x, int y, int width, int height)
        {
            return source?.CropBitmap(new PixelRect(x, y, width, height));
        }
        
        public static Bitmap? CropBitmap(this Bitmap? source, Rect cutRect)
        {
            return source?.CropBitmap(new PixelRect((int)cutRect.X, (int)cutRect.Y, (int)cutRect.Width, (int)cutRect.Height));
        }

        public static Bitmap? RotateImage(this Bitmap? source, float angleDegrees)
        {
            if (source == null) return null;
            double rad = angleDegrees * Math.PI / 180.0;
            double cos = Math.Abs(Math.Cos(rad));
            double sin = Math.Abs(Math.Sin(rad));
            int newW = (int)(source.Size.Width * cos + source.Size.Height * sin);
            int newH = (int)(source.Size.Width * sin + source.Size.Height * cos);

            var rtb = new RenderTargetBitmap(new PixelSize(newW, newH));
            using (var ctx = rtb.CreateDrawingContext())
            {
                var transform = Matrix.CreateTranslation(-source.Size.Width / 2, -source.Size.Height / 2) *
                                Matrix.CreateRotation(rad) *
                                Matrix.CreateTranslation(newW / 2.0, newH / 2.0);
                
                using (ctx.PushTransform(transform))
                {
                    ctx.DrawImage(source, new Rect(0, 0, source.Size.Width, source.Size.Height));
                }
            }
            return rtb;
        }

        public static Bitmap? Zoom(this Bitmap? source, float zoomFactor)
        {
            if (source == null || zoomFactor <= 0) return null;
            int newWidth = (int)(source.Size.Width * zoomFactor);
            int newHeight = (int)(source.Size.Height * zoomFactor);
            return source.ResizeImage(newWidth, newHeight);
        }

        // ─────────────────────────────────────────────────────────────
        // Drawing
        // ─────────────────────────────────────────────────────────────
        
        public static Bitmap? DrawRect(this Bitmap? bitmap, int x, int y, int width, int height, Color color, int strokeWidth = 3)
        {
            if (bitmap == null) return null;
            var rtb = new RenderTargetBitmap(new PixelSize(bitmap.PixelSize.Width, bitmap.PixelSize.Height));
            using (var ctx = rtb.CreateDrawingContext())
            {
                ctx.DrawImage(bitmap, new Rect(0, 0, bitmap.Size.Width, bitmap.Size.Height));
                var pen = new Pen(new SolidColorBrush(color), strokeWidth);
                ctx.DrawRectangle(pen, new Rect(x, y, width, height));
            }
            return rtb;
        }

        public static Bitmap? FillRect(this Bitmap? bitmap, int x, int y, int width, int height, Color color)
        {
            if (bitmap == null) return null;
            var rtb = new RenderTargetBitmap(new PixelSize(bitmap.PixelSize.Width, bitmap.PixelSize.Height));
            using (var ctx = rtb.CreateDrawingContext())
            {
                ctx.DrawImage(bitmap, new Rect(0, 0, bitmap.Size.Width, bitmap.Size.Height));
                ctx.DrawRectangle(new SolidColorBrush(color), null, new Rect(x, y, width, height));
            }
            return rtb;
        }

        public static Bitmap? DrawText(this Bitmap? bitmap, string text, float x, float y, float fontSize = 24f, Color? color = null)
        {
            if (bitmap == null || string.IsNullOrEmpty(text)) return null;
            var rtb = new RenderTargetBitmap(new PixelSize(bitmap.PixelSize.Width, bitmap.PixelSize.Height));
            using (var ctx = rtb.CreateDrawingContext())
            {
                ctx.DrawImage(bitmap, new Rect(0, 0, bitmap.Size.Width, bitmap.Size.Height));
                var formattedText = new FormattedText(
                    text,
                    System.Globalization.CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    Typeface.Default,
                    fontSize,
                    new SolidColorBrush(color ?? Colors.Red));
                ctx.DrawText(formattedText, new Point(x, y));
            }
            return rtb;
        }

        // ─────────────────────────────────────────────────────────────
        // Grayscale
        // ─────────────────────────────────────────────────────────────
        
        //public static unsafe Bitmap? ToGrayscale(this Bitmap? source)
        //{
        //    if (source == null) return null;
        //    var rtb = new RenderTargetBitmap(source.PixelSize, source.Dpi);
        //    using (var ctx = rtb.CreateDrawingContext())
        //    {
        //        ctx.DrawImage(source, new Rect(source.Size));
        //    }
            
        //    // To manipulate pixels, we need WriteableBitmap or we can just return rtb
        //    // Wait, RenderTargetBitmap doesn't have Lock().
        //    // We need to use WriteableBitmap.
        //    var wb = new WriteableBitmap(source.PixelSize, source.Dpi, PixelFormat.Bgra8888, AlphaFormat.Premul);
        //    using (var ctx = wb.CreateDrawingContext())
        //    {
        //        ctx.DrawImage(source, new Rect(source.Size));
        //    }

        //    using var fb = wb.Lock();
        //    var ptr = (byte*)fb.Address;
        //    int totalBytes = fb.RowBytes * wb.PixelSize.Height;
            
        //    for (int i = 0; i < totalBytes; i += 4)
        //    {
        //        byte b = ptr[i];
        //        byte g = ptr[i + 1];
        //        byte r = ptr[i + 2];
        //        byte gray = (byte)((0.299 * r) + (0.587 * g) + (0.114 * b));
        //        ptr[i] = gray;
        //        ptr[i + 1] = gray;
        //        ptr[i + 2] = gray;
        //    }
        //    return wb;
        //}

        // ─────────────────────────────────────────────────────────────
        // Save to file
        // ─────────────────────────────────────────────────────────────

        public static bool SaveToFile(this Bitmap? wb, string filePath, int quality = 90)
        {
            if (wb == null || string.IsNullOrEmpty(filePath)) return false;
            try
            {
                wb.Save(filePath, quality);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}