// G4.4: Port từ CameraRtspUrlBuilderFactory.cs (WinForms).
// KHÔNG thay đổi bất kỳ logic nào — copy nguyên vẹn.
// QUAN TRỌNG (Risk R6 ADR): TryBuild signature PHẢI giữ nguyên (internal static) vì
// MotionGateTest gọi qua InternalsVisibleTo. Dù MotionGateTest không migrate lần này,
// method signature phải khớp để không phá vỡ giả định tương lai.
using System;
using System.Collections.Generic;

namespace Kztek.Cameras
{
    /// <summary>
    /// Kết quả build URL RTSP cho một hãng camera.
    /// </summary>
    /// <param name="Url">Chuỗi URL RTSP đầy đủ.</param>
    /// <param name="SetAiDetectBoxes">
    /// true: AnvPlayer của camera này cần gán AiDetectBoxes (Avigilon, Tiandy, Custom, Hanwha).
    /// false: không gán AiDetectBoxes (mặc định — hầu hết các hãng còn lại).
    /// </param>
    internal record CameraRtspBuildResult(string Url, bool SetAiDetectBoxes = false);

    /// <summary>
    /// Factory build URL RTSP theo <see cref="CameraType"/>.
    /// Mỗi hãng được tách thành 1 private static method riêng —
    /// TUYỆT ĐỐI không sửa method cũ khi thêm hãng mới.
    ///
    /// Cách thêm hãng camera mới:
    ///   1. Thêm <c>private static CameraRtspBuildResult Build[Brand](Camera cam)</c>.
    ///   2. Đăng ký vào <c>_builders[CameraType.Brand] = Build[Brand]</c>.
    ///   3. Không cần sửa Camera.Start().
    /// </summary>
    internal static class CameraRtspUrlBuilderFactory
    {
        /// <summary>
        /// Mật khẩu mặc định từ nhà sản xuất cho camera Enster (factory default).
        /// Chỉ dùng khi caller không truyền password —
        /// KHÔNG phải thông tin bí mật riêng của hệ thống này.
        /// </summary>
        private const string EnsterFactoryDefaultPassword = "tlJwpbo6";

        private static readonly Dictionary<CameraType, Func<Camera, CameraRtspBuildResult>> _builders
            = new()
            {
                [CameraType.Avigilon]   = BuildAvigilon,
                [CameraType.Enster]     = BuildEnster,
                [CameraType.Vivantek]   = BuildVivantek,
                [CameraType.Enster1]    = BuildEnster1,
                [CameraType.Hanse]      = BuildHanse,
                [CameraType.Tiandy]     = BuildTiandy,
                [CameraType.DMAX]       = BuildDMAX,
                [CameraType.Dahua]      = BuildDahua,
                [CameraType.HIKVISION2] = BuildHIKVISION2,
                [CameraType.CNB]        = BuildCNB,
                [CameraType.HANET]      = BuildHANET,
                [CameraType.PELCO]      = BuildPELCO,
                [CameraType.Bosch]      = BuildBosch,
                [CameraType.Custom]     = BuildCustom,
                [CameraType.ZKteco]     = BuildZKteco,
                [CameraType.Hanwha]     = BuildHanwha,
                [CameraType.IPRO]       = BuildIPRO,
                [CameraType.Milesight]  = BuildMilesight,
                // CameraType.VideoFile        — no entry intentionally (no-op trong switch gốc)
                // CameraType.VideoCaptureDevice — no entry (không có case trong switch gốc)
                // CameraType.HIKVISION         — no entry (không có case trong switch gốc)
            };

        /// <summary>
        /// Xây dựng URL RTSP cho <paramref name="cameraType"/>.
        /// </summary>
        /// <param name="cameraType">Loại camera cần build URL.</param>
        /// <param name="camera">Đối tượng camera chứa IP, login, port, channel, v.v.</param>
        /// <param name="result">
        /// Kết quả gồm URL đầy đủ và cờ SetAiDetectBoxes.
        /// null khi trả về false.
        /// </param>
        /// <returns>
        /// true nếu có builder đăng ký cho <paramref name="cameraType"/>;
        /// false nếu không (VD: VideoFile, VideoCaptureDevice, HIKVISION).
        /// </returns>
        public static bool TryBuild(CameraType cameraType, Camera camera, out CameraRtspBuildResult result)
        {
            if (_builders.TryGetValue(cameraType, out var builder))
            {
                result = builder(camera);
                return true;
            }
            result = null;
            return false;
        }

        // ──────────────────────────────────────────────────────────────────────
        // Builder methods
        // Quy tắc: 1 method = 1 hãng. Format string được copy NGUYÊN VĂN từ
        // switch-case cũ — KHÔNG thay đổi bất kỳ ký tự nào trong URL template.
        // ──────────────────────────────────────────────────────────────────────

        private static CameraRtspBuildResult BuildAvigilon(Camera cam) =>
            new(
                $"rtsp://{cam.Login}:{cam.Password}@{cam.VideoSource}:{cam.RtspPort}/rtsp/defaultSecondary?streamType=m",
                SetAiDetectBoxes: true);

        private static CameraRtspBuildResult BuildEnster(Camera cam)
        {
            string url = string.IsNullOrEmpty(cam.Password)
                ? $"rtsp://{cam.VideoSource}:{cam.RtspPort}/user=admin_password={EnsterFactoryDefaultPassword}_channel=1_stream=1.sdp?real_stream"
                : $"rtsp://{cam.Login}:{cam.Password}@{cam.VideoSource}:{cam.RtspPort}/1{cam.Chanel}";
            return new(url);
        }

        private static CameraRtspBuildResult BuildVivantek(Camera cam) =>
            new($"rtsp://{cam.Login}:{cam.Password}@{cam.VideoSource}:{cam.RtspPort}/main");

        private static CameraRtspBuildResult BuildEnster1(Camera cam) =>
            new($"rtsp://{cam.Login}:{cam.Password}@{cam.VideoSource}:{cam.RtspPort}/channel=1_stream=0&protocol=unicast.sdp?real_stream");

        private static CameraRtspBuildResult BuildHanse(Camera cam) =>
            new($"rtsp://{cam.Login}:{cam.Password}@{cam.VideoSource}:{cam.RtspPort}/stream{cam.Chanel}");

        private static CameraRtspBuildResult BuildTiandy(Camera cam) =>
            // Lưu ý: có dòng test cũ bị comment: rtsp://192.168.21.202:8554/live
            new(
                $"rtsp://{cam.Login}:{cam.Password}@{cam.VideoSource}:{cam.RtspPort}/{cam.Chanel}/1",
                SetAiDetectBoxes: true);

        private static CameraRtspBuildResult BuildDMAX(Camera cam) =>
            new($"rtsp://{cam.Login}:{cam.Password}@{cam.VideoSource}:{cam.RtspPort}/1/stream{cam.Chanel}");

        private static CameraRtspBuildResult BuildDahua(Camera cam)
        {
            string chanel_str = (cam.Chanel > 0) ? cam.Chanel.ToString() : "1";
            // subtype=0 : main stream, subtype=1 : sub-stream
            return new($"rtsp://{cam.Login}:{cam.Password}@{cam.VideoSource}:{cam.RtspPort}/cam/realmonitor?channel={chanel_str}&subtype=0");
        }

        private static CameraRtspBuildResult BuildHIKVISION2(Camera cam) =>
            // /PSIA/streaming/channels/{channel}0{streamIndex}
            new($"rtsp://{cam.Login}:{cam.Password}@{cam.VideoSource}:{cam.RtspPort}/PSIA/streaming/channels/{((cam.Chanel <= 0) ? 1 : cam.Chanel)}0{cam.StreamIndex}");

        private static CameraRtspBuildResult BuildCNB(Camera cam) =>
            // /media/video{channel}0{streamIndex}
            new($"rtsp://{cam.Login}:{cam.Password}@{cam.VideoSource}:{cam.RtspPort}/media/video{((cam.Chanel <= 0) ? 1 : cam.Chanel)}0{cam.StreamIndex}");

        private static CameraRtspBuildResult BuildHANET(Camera cam) =>
            // Demo: fixed stream (hardcoded trong switch gốc)
            new("rtsp://192.168.20.50:554/user:hanet;pwd:hanet123-main264");

        private static CameraRtspBuildResult BuildPELCO(Camera cam) =>
            new($"rtsp://{cam.Login}:{cam.Password}@{cam.VideoSource}:{cam.RtspPort}/rtsp/defaultPrimary?streamType=u");

        private static CameraRtspBuildResult BuildBosch(Camera cam) =>
            new($"rtsp://{cam.Login}:{cam.Password}@{cam.VideoSource}:{cam.RtspPort}/rtsp_tunnel?p=0&line=1&inst=1&vcd=2");

        private static CameraRtspBuildResult BuildCustom(Camera cam) =>
            new(cam.VideoSource, SetAiDetectBoxes: true);

        private static CameraRtspBuildResult BuildZKteco(Camera cam) =>
            new($"rtsp://{cam.Login}:{cam.Password}@{cam.VideoSource}:{cam.RtspPort}/ch01.264");

        private static CameraRtspBuildResult BuildHanwha(Camera cam) =>
            // rtsp://user:password@ip:port/profile1/media.smp
            new(
                $"rtsp://{cam.Login}:{cam.Password}@{cam.VideoSource}:{cam.RtspPort}/profile1/media.smp",
                SetAiDetectBoxes: true);

        private static CameraRtspBuildResult BuildIPRO(Camera cam) =>
            // rtsp://user:password@ip:port/ONVIF/MediaInput?profile=def_profile{channel}
            new($"rtsp://{cam.Login}:{cam.Password}@{cam.VideoSource}:{cam.RtspPort}/ONVIF/MediaInput?profile=def_profile{cam.Chanel}");

        /// <summary>
        /// Milesight Network Camera — theo tài liệu RTSP chính hãng (Network &gt; Advanced &gt; RTSP):
        /// rtsp://ip:rtspPort/main (Primary), /sub (Secondary), /third (Tertiary). Port mặc định 554.
        /// Dùng <see cref="Camera.Chanel"/> (KHÔNG phải StreamIndex — caller không set field đó) để
        /// chọn stream, đánh số kênh bắt đầu từ 1 (parity với Dahua/HIKVISION2/CNB): 1 = main (mặc định
        /// khi Chanel &lt;= 0), 2 = sub, ≥3 = third.
        /// </summary>
        private static CameraRtspBuildResult BuildMilesight(Camera cam)
        {
            int channel = cam.Chanel <= 0 ? 1 : cam.Chanel;
            string streamPath = channel switch
            {
                1 => "main",
                2 => "sub",
                _ => "third",
            };
            return new($"rtsp://{cam.Login}:{cam.Password}@{cam.VideoSource}:{cam.RtspPort}/{streamPath}");
        }
    }
}
