// G6.6 — Smoke test project Kztek.Cameras.Avalonia
// Phạm vi: khởi tạo object business layer + gọi API tĩnh KHÔNG cần network/hardware/UI runtime.
// Các test cần Avalonia.Headless (AnvPlayer, Controls) được đánh dấu Skip với lý do rõ.
//
// TC-AV-001: Build gate — documented riêng (dotnet build Release win-x64: 0 Error 0 Warning)
// TC-AV-002..010: xunit tests bên dưới
using Kztek.Cameras;
using SkiaSharp;
using System.Collections.Generic;
using Xunit;

namespace Kztek.Cameras.Avalonia.Tests
{
    // ─────────────────────────────────────────────────────────────────────────
    // TC-AV-002 / TC-AV-003  — Camera POCO
    // ─────────────────────────────────────────────────────────────────────────
    public class CameraConstructorTests
    {
        [Fact]
        public void Camera_DefaultConstructor_DoesNotThrow()
        {
            // Verify: Camera() không throw — business object thuần, không có Avalonia dep.
            var camera = new Camera();
            Assert.NotNull(camera);
        }

        [Fact]
        public void Camera_DefaultProperties_AreCorrect()
        {
            // Verify: default field values khớp WinForms gốc (parity TC-AV-003).
            var camera = new Camera();
            Assert.Equal("Camera", camera.Name);   // private name = "Camera"
            Assert.Equal("C",      camera.Code);   // private code = "C"
            Assert.Equal(554,      camera.RtspPort); // private rtspPort = 554
            Assert.Equal(1,        camera.StreamIndex); // private streamIndex = 1
            Assert.False(camera.IsRunning); // videoSourcePlayer == null → false
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // TC-AV-004 / TC-AV-005 / TC-AV-006  — CameraRtspUrlBuilderFactory (internal)
    // Accessible nhờ InternalsVisibleTo("Kztek.Cameras.Avalonia.Tests") trong csproj.
    // ─────────────────────────────────────────────────────────────────────────
    public class CameraRtspUrlBuilderFactoryTests
    {
        private static Camera MakeCamera(string ip = "192.168.1.10",
                                         string login = "admin",
                                         string password = "12345",
                                         int rtspPort = 554,
                                         int chanel = 1,
                                         int streamIndex = 1)
        {
            return new Camera
            {
                VideoSource = ip,
                Login       = login,
                Password    = password,
                RtspPort    = rtspPort,
                Chanel      = chanel,
                StreamIndex = streamIndex
            };
        }

        [Fact]
        public void TryBuild_Avigilon_ReturnsTrueAndValidUrl()
        {
            // TC-AV-004: Avigilon có builder đăng ký — phải trả true + URL hợp lệ.
            var cam = MakeCamera();
            var ok  = CameraRtspUrlBuilderFactory.TryBuild(CameraType.Avigilon, cam, out var result);

            Assert.True(ok);
            Assert.NotNull(result);
            Assert.Contains("rtsp://", result.Url);
            Assert.Contains("192.168.1.10", result.Url);
            Assert.True(result.SetAiDetectBoxes, "Avigilon phải set AiDetectBoxes=true theo ADR");
        }

        [Fact]
        public void TryBuild_VideoFile_ReturnsFalse()
        {
            // TC-AV-005: VideoFile KHÔNG có builder (comment rõ trong source) → phải false.
            var cam = MakeCamera();
            var ok  = CameraRtspUrlBuilderFactory.TryBuild(CameraType.VideoFile, cam, out var result);

            Assert.False(ok);
            Assert.Null(result);
        }

        [Fact]
        public void TryBuild_Dahua_Channel1_UrlContainsExpectedParams()
        {
            // TC-AV-006: Dahua với channel=1 → URL có "realmonitor?channel=1&subtype=0".
            var cam = MakeCamera(chanel: 1);
            var ok  = CameraRtspUrlBuilderFactory.TryBuild(CameraType.Dahua, cam, out var result);

            Assert.True(ok);
            Assert.NotNull(result);
            Assert.Contains("realmonitor?channel=1", result.Url);
            Assert.Contains("subtype=0", result.Url);
        }

        [Fact]
        public void TryBuild_Dahua_Channel0_FallsBackTo1()
        {
            // Parity: WinForms gốc — khi chanel <= 0 dùng "1" (xem comment trong source).
            var cam = MakeCamera(chanel: 0);
            var ok  = CameraRtspUrlBuilderFactory.TryBuild(CameraType.Dahua, cam, out var result);

            Assert.True(ok);
            Assert.Contains("channel=1", result!.Url); // chanel=0 → "1"
        }

        [Fact]
        public void TryBuild_Custom_UsesVideoSourceDirectlyAndSetsAiDetect()
        {
            // TC: CameraType.Custom → URL = VideoSource, SetAiDetectBoxes = true.
            var cam = MakeCamera(ip: "rtsp://192.168.1.99:8554/live");
            var ok  = CameraRtspUrlBuilderFactory.TryBuild(CameraType.Custom, cam, out var result);

            Assert.True(ok);
            Assert.Equal("rtsp://192.168.1.99:8554/live", result!.Url);
            Assert.True(result.SetAiDetectBoxes);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // TC-AV-007 / TC-AV-008  — CameraCollection
    // ─────────────────────────────────────────────────────────────────────────
    public class CameraCollectionTests
    {
        [Fact]
        public void CameraCollection_DefaultConstructor_IsEmpty()
        {
            // TC-AV-007: CameraCollection() tạo không throw + rỗng.
            var col = new CameraCollection();
            Assert.NotNull(col);
            Assert.Equal(0, col.Count);
        }

        [Fact]
        public void CameraCollection_FromList_HasCorrectCount()
        {
            // TC-AV-008: CameraCollection(List<Camera>) — count khớp.
            var cameras = new List<Camera> { new Camera(), new Camera(), new Camera() };
            var col = new CameraCollection(cameras);
            Assert.Equal(3, col.Count);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // TC-AV-009 / TC-AV-010  — SKBitmapPool (public static, pure SkiaSharp)
    // ─────────────────────────────────────────────────────────────────────────
    public class SKBitmapPoolTests
    {
        [Fact]
        public void Rent_ReturnsBitmapWithCorrectSize()
        {
            // TC-AV-009: SKBitmapPool.Rent trả về SKBitmap đúng kích thước.
            var bmp = SKBitmapPool.Rent(320, 240);
            try
            {
                Assert.NotNull(bmp);
                Assert.Equal(320, bmp.Width);
                Assert.Equal(240, bmp.Height);
                Assert.Equal(SKColorType.Bgra8888, bmp.ColorType); // default colorType
            }
            finally
            {
                SKBitmapPool.Return(bmp);
            }
        }

        [Fact]
        public void RentReturn_RoundTrip_NoException()
        {
            // TC-AV-010: Rent → Return → Rent lại — không throw, bitmap tái dùng được.
            var bmp1 = SKBitmapPool.Rent(640, 480);
            SKBitmapPool.Return(bmp1);
            var bmp2 = SKBitmapPool.Rent(640, 480); // có thể lấy lại từ pool
            Assert.NotNull(bmp2);
            Assert.Equal(640, bmp2.Width);
            Assert.Equal(480, bmp2.Height);
            SKBitmapPool.Return(bmp2);
        }

        [Fact]
        public void Return_NullBitmap_DoesNotThrow()
        {
            // Edge case: Return(null) — không throw (method có null guard).
            var ex = Record.Exception(() => SKBitmapPool.Return(null));
            Assert.Null(ex);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // TC-AV-011  — AnvPlayer() constructor (SKIP — cần Avalonia.Headless)
    // ─────────────────────────────────────────────────────────────────────────
    public class AnvPlayerSmokeTests
    {
        [Fact(Skip = "Avalonia.Headless chưa cấu hình trong test project này. " +
                     "AnvPlayer kế thừa Avalonia.Controls.Control — constructor dùng " +
                     "AvaloniaProperty setter (MinWidth/MinHeight) cần Avalonia platform thread. " +
                     "Future work: thêm Avalonia.Headless + [AvaloniaFact] để test Control lifecycle.")]
        public void AnvPlayer_DefaultConstructor_DoesNotThrow()
        {
            // Khi Avalonia.Headless khả dụng, test này verify:
            // - new AnvPlayer() không throw
            // - MinWidth = 640, MinHeight = 480
            // - _service được khởi tạo (via reflection hoặc InternalsVisibleTo)
            // - Event wiring không throw
        }
    }
}
