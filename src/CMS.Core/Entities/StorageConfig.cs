namespace CMS.Core.Entities;

/// <summary>
/// Cấu hình dung lượng và vị trí lưu trữ video cho hệ thống VMS
/// </summary>
public class StorageConfig
{
    public string RootStoragePath { get; set; } = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Recordings");

    /// <summary>
    /// Thời lượng cho mỗi file phân đoạn (phút): 15, 30, hoặc 60 phút
    /// </summary>
    public int SegmentDurationMinutes { get; set; } = 15;

    /// <summary>
    /// Dung lượng trống tối thiểu của ổ đĩa (GB) trước khi kích hoạt ghi đè tự động
    /// </summary>
    public int MinFreeSpaceGigabytes { get; set; } = 10;

    /// <summary>
    /// Số ngày lưu trữ tối đa (sau số ngày này file sẽ tự động xóa)
    /// </summary>
    public int RetentionDays { get; set; } = 30;

    /// <summary>
    /// Tự động xóa file cũ nhất khi ổ đĩa đầy (FIFO)
    /// </summary>
    public bool AutoOverwriteOldest { get; set; } = true;
}
