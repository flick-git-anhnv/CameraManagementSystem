using CMS.Core.Enums;

namespace CMS.Core.Entities;

/// <summary>
/// Đại diện cho một đoạn file video đã được ghi hình và lưu trữ trên ổ đĩa
/// </summary>
public class VideoSegment
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string CameraId { get; set; } = string.Empty;
    public string CameraName { get; set; } = string.Empty;

    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public double DurationSeconds => (EndTime - StartTime).TotalSeconds;

    public string FilePath { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }

    public RecordingMode RecordingType { get; set; } = RecordingMode.Continuous247;
    public StreamType StreamType { get; set; } = StreamType.MainStream;

    public bool IsLocked { get; set; } = false; // Đánh dấu file quan trọng (không bị xóa FIFO)
}
