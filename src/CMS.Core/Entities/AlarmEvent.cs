using CMS.Core.Enums;

namespace CMS.Core.Entities;

public class AlarmEvent
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string CameraId { get; set; } = string.Empty;
    public string CameraName { get; set; } = string.Empty;
    public AlarmEventType EventType { get; set; } = AlarmEventType.MotionDetected;
    public AlarmLevel Level { get; set; } = AlarmLevel.Warning;
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public string Description { get; set; } = string.Empty;
    public string? SnapshotPath { get; set; }
    public bool IsHandled { get; set; } = false;
    public string? HandledBy { get; set; }
    public DateTime? HandledAt { get; set; }
}
