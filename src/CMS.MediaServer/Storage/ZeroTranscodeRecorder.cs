using CMS.Core.Entities;
using CMS.Core.Enums;
using CMS.Core.Interfaces;
using CMS.Drivers;
using System.Diagnostics;

namespace CMS.MediaServer.Storage;

/// <summary>
/// Ghi hình trực tiếp luồng stream RTSP (H.264/H.265/AAC) sang file MP4 phân đoạn
/// mà KHÔNG cần giải mã hình ảnh (Zero-Transcode / Direct Stream Copy) — tải CPU &lt; 0.5%.
/// </summary>
public class ZeroTranscodeRecorder : IDisposable
{
    private readonly CameraDevice _camera;
    private readonly StorageConfig _config;
    private readonly IVideoSegmentRepository _segmentRepo;
    private Process? _ffmpegProcess;
    private FileSystemWatcher? _fileWatcher;
    private string _cameraStorageDir = string.Empty;
    private bool _isRecording;
    private DateTime _currentSegmentStartTime;

    public string CameraId => _camera.Id;
    public bool IsRecording => _isRecording;

    public ZeroTranscodeRecorder(CameraDevice camera, StorageConfig config, IVideoSegmentRepository segmentRepo)
    {
        _camera = camera;
        _config = config;
        _segmentRepo = segmentRepo;
    }

    public Task<bool> StartAsync(CancellationToken cancellationToken = default)
    {
        if (_isRecording) return Task.FromResult(true);

        try
        {
            // 1. Chuẩn bị thư mục lưu trữ: {Root}/{CameraId}/{yyyy-MM-dd}/
            string todayStr = DateTime.Now.ToString("yyyy-MM-dd");
            _cameraStorageDir = Path.Combine(_config.RootStoragePath, _camera.Id, todayStr);
            Directory.CreateDirectory(_cameraStorageDir);

            // 2. Tìm binary ffmpeg
            string ffmpegPath = ResolveFfmpegPath();
            if (!File.Exists(ffmpegPath))
            {
                throw new FileNotFoundException($"Cannot locate ffmpeg binary at '{ffmpegPath}'");
            }

            // 3. Lấy URL stream (Ưu tiên Main-stream để có chất lượng cao nhất khi ghi hình)
            var driver = CameraDriverFactory.GetDriver(_camera.Brand);
            var endpoints = driver.BuildStreamUrls(_camera);
            string rtspUrl = _camera.RecordSubStream ? endpoints.SubStreamUrl : endpoints.MainStreamUrl;

            // 4. Thiết lập FileSystemWatcher để tự động bắt và lưu metadata file vào DB khi hoàn tất
            SetupWatcher(_cameraStorageDir);

            // 5. Cấu hình tham số Zero-transcode:
            // -rtsp_transport tcp : chống mất gói qua mạng
            // -c copy            : không decode/encode lại (0% CPU)
            // -f segment         : tự động ngắt file theo thời gian
            // -reset_timestamps 1: reset timestamp mỗi file để tương thích mọi media player
            int segmentSeconds = Math.Max(60, _config.SegmentDurationMinutes * 60);
            string outputPattern = Path.Combine(_cameraStorageDir, $"{_camera.Id}_%Y%m%d_%H%M%S.mp4");

            var psi = new ProcessStartInfo
            {
                FileName = ffmpegPath,
                Arguments = $"-nostdin -hide_banner -loglevel error -rtsp_transport tcp -i \"{rtspUrl}\" -c copy -f segment -segment_time {segmentSeconds} -segment_format mp4 -reset_timestamps 1 -strftime 1 \"{outputPattern}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true
            };

            _currentSegmentStartTime = DateTime.Now;
            _ffmpegProcess = Process.Start(psi);

            if (_ffmpegProcess != null && !_ffmpegProcess.HasExited)
            {
                _isRecording = true;
                return Task.FromResult(true);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ZeroTranscodeRecorder] Start failed for camera {_camera.Name}: {ex.Message}");
        }

        return Task.FromResult(false);
    }

    public async Task StopAsync()
    {
        if (!_isRecording) return;
        _isRecording = false;

        try
        {
            if (_fileWatcher != null)
            {
                _fileWatcher.EnableRaisingEvents = false;
                _fileWatcher.Dispose();
                _fileWatcher = null;
            }

            if (_ffmpegProcess != null && !_ffmpegProcess.HasExited)
            {
                _ffmpegProcess.Kill();
                await _ffmpegProcess.WaitForExitAsync();
                _ffmpegProcess.Dispose();
                _ffmpegProcess = null;
            }

            // Quét và cập nhật file cuối cùng vào DB
            ScanAndIndexRecentFiles();
        }
        catch
        {
        }
    }

    private void SetupWatcher(string directory)
    {
        _fileWatcher = new FileSystemWatcher(directory, "*.mp4")
        {
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size
        };

        _fileWatcher.Created += async (_, e) =>
        {
            // Khi file mới được tạo, file trước đó đã kết thúc ghi
            await Task.Delay(1000); // Chờ ffmpeg finalize file
            IndexVideoFile(e.FullPath);
        };

        _fileWatcher.EnableRaisingEvents = true;
    }

    private void IndexVideoFile(string filePath)
    {
        try
        {
            if (!File.Exists(filePath)) return;

            var fi = new FileInfo(filePath);
            if (fi.Length < 1024) return; // Bỏ qua file rác rỗng

            var seg = new VideoSegment
            {
                CameraId = _camera.Id,
                CameraName = _camera.Name,
                FilePath = filePath,
                FileSizeBytes = fi.Length,
                StartTime = fi.CreationTime,
                EndTime = fi.LastWriteTime > fi.CreationTime ? fi.LastWriteTime : fi.CreationTime.AddMinutes(_config.SegmentDurationMinutes),
                RecordingType = _camera.RecordMode,
                StreamType = _camera.RecordSubStream ? StreamType.SubStream : StreamType.MainStream
            };

            _ = _segmentRepo.AddSegmentAsync(seg);
        }
        catch
        {
        }
    }

    private void ScanAndIndexRecentFiles()
    {
        try
        {
            if (!Directory.Exists(_cameraStorageDir)) return;
            var files = Directory.GetFiles(_cameraStorageDir, "*.mp4");
            foreach (var f in files)
            {
                IndexVideoFile(f);
            }
        }
        catch
        {
        }
    }

    private static string ResolveFfmpegPath()
    {
        // 1. Kiểm tra cạnh output của app
        string localPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ffmpeg.exe");
        if (File.Exists(localPath)) return localPath;

        // 2. Kiểm tra trong Resource của Kztek.Cameras.Avalonia
        string kztekPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Players", "FFMPEG", "Resource", "ffmpeg.exe");
        if (File.Exists(kztekPath)) return kztekPath;

        // 3. Fallback sang system PATH
        return "ffmpeg";
    }

    public void Dispose()
    {
        _ = StopAsync();
    }
}
