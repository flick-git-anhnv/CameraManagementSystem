using CMS.Core.Entities;

namespace CMS.Core.Interfaces;

public interface IVideoSegmentRepository
{
    Task<bool> AddSegmentAsync(VideoSegment segment);
    Task<List<VideoSegment>> GetSegmentsAsync(string cameraId, DateTime startTime, DateTime endTime);
    Task<List<VideoSegment>> GetOldestUnlockedSegmentsAsync(int count);
    Task<bool> DeleteSegmentAsync(string id);
    Task<long> GetTotalStorageUsedBytesAsync();
}
