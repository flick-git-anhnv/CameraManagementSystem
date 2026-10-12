using CMS.Core.Entities;

namespace CMS.Core.Interfaces;

public interface IStorageEngine
{
    StorageConfig Configuration { get; }

    /// <summary>
    /// Bắt đầu ghi hình cho một camera
    /// </summary>
    Task<bool> StartRecordingAsync(CameraDevice camera, CancellationToken cancellationToken = default);

    /// <summary>
    /// Dừng ghi hình cho một camera
    /// </summary>
    Task StopRecordingAsync(string cameraId);

    /// <summary>
    /// Kiểm tra camera có đang được ghi hình hay không
    /// </summary>
    bool IsRecording(string cameraId);

    /// <summary>
    /// Dừng toàn bộ các tiến trình ghi hình
    /// </summary>
    Task StopAllRecordingAsync();

    /// <summary>
    /// Cập nhật cấu hình lưu trữ
    /// </summary>
    void UpdateConfiguration(StorageConfig config);
}
