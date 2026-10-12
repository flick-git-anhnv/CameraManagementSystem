namespace Kztek.Cameras
{
    public enum CameraType
    {
        VideoFile,
        VideoCaptureDevice,
        HIKVISION,
        Avigilon,
        Enster,
        Enster1,
        Hanse,
        Tiandy,
        Dahua,
        HIKVISION2,
        DMAX,
        Vivantek,
        CNB,
        HANET,
        PELCO,
        Bosch,
        ZKteco,
        Hanwha,
        IPRO,
        Custom,
        // Thêm cuối enum (2026-07-20) — KHÔNG chèn giữa: nhiều nơi trong codebase decode
        // "loại camera" bằng int ordinal (xem lesson camera-type-int-decoded-with-wrong-enum.md),
        // chèn giữa sẽ dịch chuyển toàn bộ giá trị int phía sau và gây lỗi im lặng ở nơi khác.
        Milesight,
    }
}
