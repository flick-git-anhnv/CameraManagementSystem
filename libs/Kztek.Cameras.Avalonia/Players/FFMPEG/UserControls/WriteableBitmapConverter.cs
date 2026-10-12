// Fix (2026-07-20, loại bỏ SkiaSharp): ReadPL 1.1.2 đổi sang tensor-based API (Recognize(float[] data,
// width, height)) — không còn nhận SKBitmap/Bitmap. Class này convert WriteableBitmap (Bgra8888) sang
// ReadPL.ImageTensorResult (CHW float32, RGB order) để feed trực tiếp vào Recognize()/ReadPlate()/Detect().
using System.Runtime.InteropServices;
using Avalonia.Media.Imaging;
using ReadPL;

namespace Kztek.Cameras;

public static class WriteableBitmapConverter
{
    public static ImageTensorResult Extract(WriteableBitmap bmp)
    {
        int width = bmp.PixelSize.Width;
        int height = bmp.PixelSize.Height;
        const int channels = 3; // output RGB 3-channel (CHW)

        using var buf = bmp.Lock();
        int stride = buf.RowBytes;
        const int srcChannels = 4; // Avalonia WriteableBitmap mặc định Bgra8888

        byte[] pixels = new byte[height * stride];
        Marshal.Copy(buf.Address, pixels, 0, pixels.Length);

        int planeSize = width * height;
        float[] result = new float[channels * planeSize]; // CHW: [R...][G...][B...]

        for (int y = 0; y < height; y++)
        {
            int rowOffset = y * stride;
            for (int x = 0; x < width; x++)
            {
                int p = rowOffset + x * srcChannels;
                int idx = y * width + x;

                result[idx] = pixels[p + 2]; // R
                result[planeSize + idx] = pixels[p + 1]; // G
                result[2 * planeSize + idx] = pixels[p + 0]; // B
            }
        }

        return new ImageTensorResult(result, width, height, channels);
    }
}
