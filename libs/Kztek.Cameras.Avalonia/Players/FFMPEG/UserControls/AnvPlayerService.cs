// G4.3: Port từ Players/FFMPEG/UserControls/AnvPlayerService.cs (WinForms, 1262 dòng).
// Thay đổi chính:
//   - [SupportedOSPlatform("windows")] — dùng NhanDangBienSo (ReadPL, Windows-only LPR SDK).
//   - Bitmap → SKBitmap xuyên suốt (frame.Bitmap, GetCurrentImage, CheckAiDetection...).
//   - BitmapPool.Return(bmp) → SKBitmapPool.Return(bmp).
//   - ImageHelper.CropBitmap(bmp, box.Rect) → CropSKBitmap(bmp, box.Rect) (private, KzRect).
//   - AnvPlayer.DrawRect(clone, x, y, w, h, Color.Red) → DrawRect(clone, x, y, w, h) (private, SKCanvas).
//   - (Bitmap)bmp.Clone() → bmp.Copy() (SKBitmap).
//   - plateReader.Recognize(bmp): ReadPL 1.0.8 (Multi-Platform) nhận thẳng SKBitmap — không cần convert.
//   - plate.Crop: ReadPL 1.0.8 trả thẳng SKBitmap? — không cần convert.
//   - new Rectangle(...) cho ObjectBox.BBox → new KzRect(...).
//   - MotionAnalyzingEventArgs(conf, bmp.Clone(), plate.Crop, text) → MotionAnalyzingEventArgs(conf, bmp.Copy(), plate.Crop, text).
//   - Bitmap.Save(ms, ImageFormat.Jpeg) → SKImage.Encode(SKEncodedImageFormat.Jpeg).
//   - MessageBox.Show → Debug.WriteLine.
//   - Bỏ: using System.Drawing; System.Drawing.Imaging; System.Windows.Forms; Kztek.Tool.
//   - Thêm: using SkiaSharp; System.Runtime.Versioning.
//   - FrameEventArgs, NewFrameEventforSaveHandler, AIBoxDetect định nghĩa ở file này (nguồn gốc từ AnvPlayer.cs WinForms chưa port).
//   - GetCurrentVideoFrame/GetCurrentImage trả về SKBitmap? (thay Bitmap).
//   - CheckAiDetection nhận SKBitmap (thay Bitmap); boxAiDetector.Predict(SKBitmap) đã sẵn có ở G4.4.
using ANV.Cameras.PINVOKE;
using Avalonia.Media.Imaging;
using Kztek.Cameras.AiDetection;
using Kztek.Cameras.Handlers;
using Kztek.Cameras.Players.FFMPEG;
using Kztek.Cameras.TrackingObjects;
using ReadPL;
using System.Diagnostics;
using static Kztek.Object.MultyPlatform.ConfigObjects.VirtualLoopType;

namespace Kztek.Cameras
{
    // ============================================================================================
    // Types định nghĩa tại đây (nguồn gốc từ AnvPlayer.cs WinForms — chưa port UserControl)
    // ============================================================================================

    /// <summary>
    /// Gói frame JPEG cho pipeline ghi video.
    /// Thay System.Drawing.Image → byte[] (raw JPEG bytes từ SKImage.Encode).
    /// </summary>
    public sealed class FrameEventArgs : EventArgs
    {
        public byte[] FrameStream { get; }
        public FrameEventArgs(byte[] frameStream) { FrameStream = frameStream; }
    }

    /// <summary>Delegate sự kiện ghi video — không đổi signature so với WinForms.</summary>
    public delegate void NewFrameEventforSaveHandler(object sender, FrameEventArgs e);

    /// <summary>
    /// Cấu hình 1 vùng AI detection (tên box, vùng crop, ngưỡng confidence, camera ID).
    /// Thay AIBoxDetect.Rect: Rectangle → KzRect (ADR §6.3 — cross-platform).
    /// </summary>
    public sealed class AIBoxDetect
    {
        /// <summary>KzRect thay Rectangle cho vùng crop AI detection.</summary>
        public KzRect Rect { get; set; }

        public string BoxName { get; set; } = "";
        public float Confidence { get; set; }
        public string CameraId { get; set; } = "";

        /// <summary>Confidence của lần predict gần nhất — ghi từ background, đọc từ UI (trade-off đã accept).</summary>
        public float ScoreConfidence { get; set; }

        /// <summary>FrameId của frame gốc để DrawAIList kiểm tra stale (bước 2.3).</summary>
        public long FrameId { get; set; }

        /// <summary>CapturedAtTicks của frame gốc — đồng bộ box với display.</summary>
        public long CapturedAtTicks { get; set; }

        /// <summary>
        /// Trạng thái phân loại hiện tại — public readable, dùng bởi Camera.isAllClose()
        /// để kiểm tra tất cả box có classification == Open hay không.
        /// </summary>
        public EmClassification classification { get; private set; } = EmClassification.Unknown;

        private EmClassification _lastStatus = EmClassification.Unknown;

        /// <summary>
        /// Cập nhật trạng thái phân loại, trả về true nếu status thay đổi (trigger event AIDetectAnalyzing).
        /// Đồng thời cập nhật <see cref="classification"/> để Camera.isAllClose() đọc được.
        /// </summary>
        public bool UpdateStatus(EmClassification newStatus)
        {
            if (_lastStatus == newStatus) return false;
            _lastStatus = newStatus;
            classification = newStatus;
            return true;
        }
    }

    // ============================================================================================
    // AnvPlayerService
    // ============================================================================================

    /// <summary>
    /// Chứa toàn bộ business logic tách ra từ AnvPlayer (UserControl — Avalonia phiên bản):
    /// decode FFmpeg, nhận dạng biển số (LPR) + khử trùng lặp, ghi hình (recording),
    /// phát hiện chuyển động.
    ///
    /// [SupportedOSPlatform("windows")] đã gỡ (2026-07-14) theo yêu cầu, NHƯNG lưu ý:
    /// <c>plateReader</c> (<see cref="ReadPL.NhanDangBienSo"/>) là SDK nhận diện biển số
    /// CHỈ CHẠY THẬT trên Windows — việc gỡ attribute KHÔNG làm SDK này chạy được trên Linux,
    /// chỉ tắt cảnh báo CA1416 của analyzer. Class này trên thực tế vẫn Windows-only cho đến
    /// khi ReadPL có bản Linux hoặc được thay thế.
    /// </summary>
    public sealed class AnvPlayerService
    {
        private const int decodeTimeoutInSecond = 5;

        // ---- Callbacks từ AnvPlayer (cần biết kích thước control để decode đúng kích thước) ----
        private readonly Func<int> _getControlWidth;
        private readonly Func<int> _getControlHeight;

        // ---- Decode state ----
        private VideoStreamDecoderIntptr _decoder;
        /// <summary>Trả về decoder hiện tại để ConsumerDisplayFrameAsync trong AnvPlayer đọc FrameChannel.</summary>
        internal VideoStreamDecoderIntptr Decoder => _decoder;

        private CancellationTokenSource _cts;

        /// <summary>
        /// Task nền chạy PollingDecodeFrameAsync — lưu lại để Stop() có thể chờ
        /// task thoát hẳn trước khi dispose decoder (tránh race condition use-after-free native).
        /// </summary>
        private Task _pollingTask;

        private bool _isConnect;
        internal bool IsConnect => _isConnect;

        private volatile bool _isPaused;
        internal bool IsPaused
        {
            get => _isPaused;
            set
            {
                _isPaused = value;
                _decoder?.ResetPlaybackClock();
            }
        }

        private double _playbackSpeed = 1.0;
        internal double PlaybackSpeed
        {
            get => _playbackSpeed;
            set
            {
                _playbackSpeed = Math.Clamp(value, 0.25, 8.0);
                if (_decoder != null)
                {
                    _decoder.PlaybackSpeed = _playbackSpeed;
                }
            }
        }

        private double _pendingSeekSeconds = double.NaN;
        internal void RequestSeekSeconds(double seconds)
        {
            var dec = _decoder;
            if (dec != null)
            {
                dec.RequestSeekSeconds(seconds);
            }
            else
            {
                _pendingSeekSeconds = seconds;
            }
        }

        internal string RtspURL { get; set; }
        internal CameraStreamInfo VideoSource { get; set; }

        // ---- Static plateReader: dùng CHUNG cho mọi camera để đỡ tốn bộ nhớ load model OCR nhiều lần.
        // Phải khóa khi gọi Recognize() vì model không thread-safe khi ≥2 camera cùng đọc đồng thời.
        public static readonly NhanDangBienSo? plateReader = new NhanDangBienSo("CPU", "NORMAL", Camera.AI_ACTIVE_CODE);
        public static string GetCurrentStatus => plateReader?.Status() ?? "COMMUNITY";

        // ---- LPR dedup ----
        internal List<ObjectBox> lprDetectRegions { get; set; } = [];

        private readonly PlateDuplicateFilter _plateDuplicateFilter = new();

        internal int DuplicatePlateSuppressSeconds
        {
            get => _plateDuplicateFilter.SuppressSeconds;
            set => _plateDuplicateFilter.SuppressSeconds = value;
        }

        // ---- Loop ảo / Motion detection state ----
        internal EmVirtualLoopType VirtualLoopType { get; set; } = EmVirtualLoopType.None;

        internal List<ObjectBox> motionRegions { get; set; } = [];

        private double _fromMotionAlarmLevel = 15;
        internal double FromMotionAlarmLevel
        {
            get => _fromMotionAlarmLevel;
            set => _fromMotionAlarmLevel = value;
        }

        private double _toMotionAlarmLevel = 15;
        internal double ToMotionAlarmLevel
        {
            get => _toMotionAlarmLevel;
            set => _toMotionAlarmLevel = value;
        }

        internal bool CollectCalibrationSamples { get; set; } = false;

        private readonly List<(float MotionLevel, float Confidence)> _calibrationSamples = new();
        private readonly object _calibrationLock = new();

        internal IReadOnlyList<(float MotionLevel, float Confidence)> CalibrationSamples
        {
            get { lock (_calibrationLock) { return _calibrationSamples.ToList(); } }
        }

        internal void ClearCalibrationSamples()
        {
            lock (_calibrationLock) { _calibrationSamples.Clear(); }
        }

        internal bool CollectMotionLevelSamples { get; set; } = false;

        private readonly List<float> _motionLevelSamples = new();
        private readonly object _motionLevelSamplesLock = new();

        internal IReadOnlyList<float> MotionLevelSamples
        {
            get { lock (_motionLevelSamplesLock) { return _motionLevelSamples.ToList(); } }
        }

        internal bool EnableMotionEventGate { get; set; } = false;

        // ---- Motion Trigger nội bộ ----
        private readonly MotionTriggerDetector _motionTriggerDetector = new();

        internal int MotionTriggerMinStillSamples { get; set; } = 3;

        internal MotionTriggerPhase MotionPhase => _motionTriggerDetector.Phase;

        internal bool IsVehicleWaiting => VirtualLoopType == EmVirtualLoopType.MotionDetection && MotionPhase == MotionTriggerPhase.Still;

        internal event EventHandler MotionTriggerDetected;

        internal void ClearMotionLevelSamples()
        {
            lock (_motionLevelSamplesLock) { _motionLevelSamples.Clear(); }
        }

        private int _motionDetectionInterval = 40;
        internal int MotionDetectionInterval
        {
            get => _motionDetectionInterval;
            set => _motionDetectionInterval = value;
        }

        internal bool EnableDisplayMotionRegion { get; set; }

        internal List<ObjectBox> faceDetectRegions { get; set; } = [];

        internal List<ObjectBox> ObjectBoxes { get; set; } = [];

        internal List<AIBoxDetect> AiDetectBoxes { get; set; } = [];

        internal readonly object _regionsLock = new();

        private readonly FrameMotionDetector motionDetector;

        private float _currentMotionLevel = 0;
        internal float CurrentMotionLevel => _currentMotionLevel;

        private long _aiDetectionDuration = 0;
        internal long AiDetectionDuration => _aiDetectionDuration;

        // ---- AI Detection (MolexDetection) — KHÔNG THAY ĐỔI LOGIC ----
        private readonly bool enableBoxAIDetection = false;
        private readonly MolexDetection boxAiDetector;
        internal bool EnableBoxAIDetection => enableBoxAIDetection;

        // ---- Recording state ----
        private bool _enableRecording;
        internal bool EnableRecording
        {
            get => _enableRecording;
            set => _enableRecording = value;
        }

        private int _recordingRate = 5;
        internal int RecordingRate
        {
            get => _recordingRate;
            set => _recordingRate = value;
        }

        private string _recordingResolution = "";
        internal string RecordingResolution
        {
            get => _recordingResolution;
            set => _recordingResolution = value;
        }

        private string _recordingFolder = "./";
        internal string RecordingFolder
        {
            get => _recordingFolder;
            set => _recordingFolder = value;
        }

        private int _recordingVideoLength = 60;
        internal int RecordingVideoLength
        {
            get => _recordingVideoLength;
            set => _recordingVideoLength = value;
        }

        private Process _recordProcess = null;
        private Stream _recordStream = null;
        private readonly object _recordLock = new();

        private DateTime _currentSegmentStartTime;

        private readonly BoundedConcurrentQueue<MemoryStream> pre_imgs = new(330);
        private event NewFrameEventforSaveHandler OnNewFrame;
        private float averageFps = 0;
        private DateTime lastFrameTime = DateTime.MinValue;
        private readonly Queue<float> last5Deltas = new();
        private const int maxDeltas = 25;

        private bool isSavingVideo = false;
        private DateTime? lastSaveTime = null;
        private int frame_length = 330;

        // ---- Events phát ra để AnvPlayer forward ra ngoài ----
        internal event MotionAnalyzingEventHandler MotionAnalyzing;
        internal event AIDetectAnalyzingEventHandler AIDetectAnalyzing;

        // ---- Constructor ----
        internal AnvPlayerService(
            EmVirtualLoopType virtualLoopType,
            double fromMotionAlarmLevel,
            double toMotionAlarmLevel,
            bool enableBoxAIDetection,
            bool enableRecording,
            Func<int> getControlWidth,
            Func<int> getControlHeight)
        {
            this.VirtualLoopType = virtualLoopType;
            this._fromMotionAlarmLevel = fromMotionAlarmLevel;
            this._toMotionAlarmLevel = toMotionAlarmLevel;
            this.enableBoxAIDetection = enableBoxAIDetection;
            this._enableRecording = enableRecording;
            this._getControlWidth = getControlWidth;
            this._getControlHeight = getControlHeight;

            motionDetector = new FrameMotionDetector();

            if (enableBoxAIDetection)
            {
                try
                {
                    boxAiDetector = new MolexDetection();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[MolexDetection] Không thể nạp model AI detection: {ex.Message}. " +
                                    "Tính năng AI detection sẽ bị tắt cho camera này.");
                    boxAiDetector = null;
                }
            }
        }

        // ---- Lifecycle ----

        internal void Stop()
        {
            _cts?.Cancel();
            _cts = null;

            bool taskExited = false;
            try
            {
                taskExited = _pollingTask?.Wait(TimeSpan.FromSeconds(10)) ?? true;
            }
            catch (AggregateException)
            {
                taskExited = true;
            }

            if (!taskExited)
            {
                Debug.WriteLine("[AnvPlayerService.Stop] WARNING: PollingDecodeFrameAsync chưa thoát sau " +
                                "10s timeout. Decoder KHÔNG được dispose để tránh AccessViolationException. " +
                                "Có thể leak tài nguyên native FFmpeg — kiểm tra luồng decode bị kẹt.");
            }
            else
            {
                _decoder?.Dispose();
            }

            _decoder = null;
            _pollingTask = null;
            _pendingSeekSeconds = double.NaN;

            _plateDuplicateFilter.Clear();

            _isConnect = false;
        }

        internal void SignalToStop()
        {
            _cts?.Cancel();
        }

        internal void StartTasks(Action<CancellationToken> startDisplayConsumer)
        {
            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            _pollingTask = Task.Factory.StartNew(
                () => PollingDecodeFrameAsync(token),
                token,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default
            ).Unwrap();

            startDisplayConsumer(token);
            Task.Run(() => ConsumerMotionDetectionAsync(token), token);
            Task.Run(() => ConsumerAiDetectionAsync(token), token);
            Task.Run(() => RunSaveImageforVideo(token), token);
        }

        // ---- Polling decode loop ----
        private async Task PollingDecodeFrameAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    _decoder?.Dispose();
                    _decoder = new VideoStreamDecoderIntptr(RtspURL, VirtualLoopType, enableBoxAIDetection);
                    _decoder.PlaybackSpeed = _playbackSpeed;
                    if (!_decoder.Connect())
                    {
                        await Task.Delay(1000, token);
                        continue;
                    }

                    if (!double.IsNaN(_pendingSeekSeconds))
                    {
                        _decoder.RequestSeekSeconds(_pendingSeekSeconds);
                        _pendingSeekSeconds = double.NaN;
                    }

                    DateTime lastFrameTime = DateTime.Now;

                    while (!token.IsCancellationRequested)
                    {
                        if (_isPaused)
                        {
                            lastFrameTime = DateTime.Now;
                            _decoder.ResetPlaybackClock();
                            await Task.Delay(30, token);
                            continue;
                        }

                        var motionBox = this.motionRegions.Count > 0 ? this.motionRegions[0] : null;

                        // Tính kích thước decode giữ đúng aspect ratio của stream.
                        // _decoder.SourceWidth/SourceHeight là kích thước frame gốc từ codec context.
                        int ctrlW = _getControlWidth();
                        int ctrlH = _getControlHeight();
                        int srcW  = _decoder.SourceWidth;
                        int srcH  = _decoder.SourceHeight;
                        int decodeW, decodeH;

                        // GUARD: Nếu control chưa layout xong (Bounds = 0 → ctrlW/H < 32),
                        // đừng decode ở 1×1 (gây lỗi lần đầu). Decode native hoặc 1280×720.
                        // DrawImage trong AnvPlayer.Render() sẽ auto-scale xuống tile khi render.
                        bool controlReady = ctrlW >= 32 && ctrlH >= 32;

                        if (!controlReady)
                        {
                            // FIX (RAM leak vòng 4, 2026-07-20): TRƯỚC ĐÂY decode ở NGUYÊN độ phân giải gốc
                            // (VD 1280×720) khi chưa layout xong — với mục đích "có frame hợp lệ ngay" lúc
                            // khởi động. Nhưng thực nghiệm với camera thật xác nhận: control nằm trong
                            // tab/lane KHÔNG được chọn hiển thị KHÔNG BAO GIỜ có Bounds hợp lệ (Avalonia
                            // TabControl lazy layout — xem lesson avalonia-tabcontrol-lazy-visual-tree-columns.md)
                            // → camera bị kẹt decode ở ĐỘ PHÂN GIẢI GỐC VĨNH VIỄN, tốn CPU/native memory liên tục
                            // dù không ai xem tile đó. Cap về tối đa 640px (giữ aspect ratio) — vẫn đủ để có
                            // frame hợp lệ cho first-frame detection, giảm ~4x khối lượng xử lý/frame so với
                            // 1280×720 native (và nhiều hơn nữa với camera độ phân giải cao hơn).
                            if (srcW > 640 || srcH > 360)
                            {
                                double capScale = Math.Min(640.0 / Math.Max(1, srcW), 360.0 / Math.Max(1, srcH));
                                decodeW = Math.Max(1, (int)(srcW * capScale));
                                decodeH = Math.Max(1, (int)(srcH * capScale));
                            }
                            else
                            {
                                decodeW = srcW > 0 ? srcW : 640;
                                decodeH = srcH > 0 ? srcH : 360;
                            }
                        }
                        else if (srcW > 0 && srcH > 0)
                        {
                            // Render() draws as Fill, so decode to the viewport size to avoid an extra UI upscale.
                            decodeW = Math.Max(1, ctrlW);
                            decodeH = Math.Max(1, ctrlH);
                        }
                        else
                        {
                            // Chưa biết kích thước gốc — dùng control size hoặc fallback 1280×720
                            decodeW = Math.Max(64, ctrlW);
                            decodeH = Math.Max(36, ctrlH);
                        }

                        // C2 fix (RAM leak BUG-004): TryDecodeFrame trả bool thay SKBitmap? — FrameChannel
                        // giờ đã tự chứa WriteableBitmap, không cần trả bitmap ra ngoài chỉ để null-check.
                        bool producedFrame = _decoder.TryDecodeFrame(token, decodeW, decodeH, motionBox, this.VirtualLoopType, 150, this._motionDetectionInterval);
                        if (producedFrame)
                        {
                            lastFrameTime = DateTime.Now;
                            this._isConnect = true;
                        }

                        if ((DateTime.Now - lastFrameTime).TotalSeconds > decodeTimeoutInSecond)
                        {
                            this._isConnect = false;
                            throw new Exception("Mất kết nối - không nhận được frame");
                        }

                        await Task.Delay(5, token);
                    }
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested)
                {
                    // BUG-004 Fix 4a: Token bị cancel (StopCamera gọi) → thoát vòng lặp sạch.
                    // Không retry khi đã được yêu cầu dừng — tránh double-throw TaskCanceledException storm.
                    this._isConnect = false;
                    return;
                }
                catch
                {
                    // Lỗi thực (network drop, RTSP error, timeout) → reconnect sau 1s.
                    this._isConnect = false;
                    try
                    {
                        // BUG-004 Fix 4b: Guard Task.Delay bên trong catch — nếu token bị cancel
                        // trong khi đang chờ reconnect, thoát sạch thay vì throw thêm lần nữa.
                        await Task.Delay(1000, token);
                    }
                    catch (OperationCanceledException)
                    {
                        return;
                    }
                }
            }

            _isConnect = false;
        }

        // ---- Motion detection consumer ----
        private async Task ConsumerMotionDetectionAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    if (_decoder == null)
                    {
                        await Task.Delay(100, token);
                        continue;
                    }

                    await foreach (var frame in _decoder.MdChannel.Reader.ReadAllAsync(token))
                    {
                        var bmp = frame?.Bitmap;
                        if (bmp == null) continue;

                        if (this.VirtualLoopType == EmVirtualLoopType.None)
                        {
                            AvaloniaFramePool.Return(bmp);
                            continue;
                        }

                        try
                        {
                            if (this.VirtualLoopType == EmVirtualLoopType.AI)
                            {
                                _currentMotionLevel = motionDetector.ProcessFrame(bmp) * 1000;

                                if (!CollectCalibrationSamples && !ShouldReadPlate(_currentMotionLevel))
                                    continue;

                                // Fix (2026-07-20, loại bỏ SkiaSharp): ReadPL 1.1.2 chuyển sang tensor-based
                                // API — Recognize(float[] data, width, height), không còn nhận SKBitmap/Bitmap.
                                // Extract() decode trực tiếp từ WriteableBitmap đã lock sẵn — chỉ tốn CPU khi
                                // thực sự cần Recognize() (đã qua gate ShouldReadPlate ở trên).
                                var tensor = WriteableBitmapConverter.Extract(bmp);

                                var sw = Stopwatch.StartNew();
                                // Trả về ReadPL.Result (KHÔNG phải Kztek.Cameras.AiDetection.PredictionResult —
                                // PredictionResult là của MolexDetection/OpenVINO, dùng ở ConsumerAiDetectionAsync).
                                ReadPL.Result results = plateReader.Recognize(tensor.Data, tensor.Width, tensor.Height);
                                sw.Stop();
                                _aiDetectionDuration = sw.ElapsedMilliseconds;

                                List<ObjectBox> boxes = new List<ObjectBox>();

                                var cropOffsetX = frame.CropRect.X;
                                var cropOffsetY = frame.CropRect.Y;
                                var fullWidth = frame.FullWidth > 0 ? frame.FullWidth : bmp.PixelSize.Width;
                                var fullHeight = frame.FullHeight > 0 ? frame.FullHeight : bmp.PixelSize.Height;

                                foreach (var plate in results.Plates)
                                {
                                    if (CollectCalibrationSamples)
                                    {
                                        lock (_calibrationLock)
                                        {
                                            _calibrationSamples.Add((_currentMotionLevel, (float)plate.Conf));
                                        }
                                    }

                                    // KzRect thay Rectangle (ADR §6.3)
                                    boxes.Add(new()
                                    {
                                        BBox = new KzRect(cropOffsetX + plate.Box.Item1, cropOffsetY + plate.Box.Item2, plate.Box.Item3, plate.Box.Item4),
                                        Width = fullWidth,
                                        Height = fullHeight,
                                        Id = "",
                                        Name = plate.Type + "-" + plate.Text + "-" + Math.Round(plate.Conf, 2),
                                        FrameId = frame.Meta.FrameId,
                                        CapturedAtTicks = frame.Meta.CapturedAtTicks,
                                    });

                                    if (!IsRecentDuplicatePlate(plate.Text))
                                    {
                                        // Fix (2026-07-20, loại bỏ SkiaSharp): plate.Crop giờ là ReadPL.ImageTensorResult
                                        // (managed float[] wrapper, KHÔNG IDisposable, KHÔNG native memory — GC tự dọn,
                                        // không còn nguồn leak SKBitmap như trước). vehicleImageForEvent vẫn cần clone
                                        // riêng (AvaloniaFramePool.Clone, KHÔNG qua pool) vì bmp sẽ bị Return() ở
                                        // finally bên ngoài ngay sau vòng foreach này, còn event có thể xử lý bất
                                        // đồng bộ (Task.Run) sau đó.
                                        var vehicleImageForEvent = AvaloniaFramePool.Clone(bmp);
                                        var data = new MotionAnalyzingEventArgs(
                                            (float)plate.Conf,
                                            vehicleImageForEvent,
                                            plate.Crop,
                                            plate.Text);
                                        _ = Task.Run(() =>
                                        {
                                            try
                                            {
                                                MotionAnalyzing?.Invoke(this, data);
                                            }
                                            finally
                                            {
                                                vehicleImageForEvent.Dispose();
                                            }
                                        }, token);
                                    }
                                }

                                foreach (var vehicle in results.Vehicles.Where(e => e.Conf > 0.85).ToList())
                                {
                                    boxes.Add(new()
                                    {
                                        BBox = new KzRect(cropOffsetX + vehicle.Box.Item1, cropOffsetY + vehicle.Box.Item2, vehicle.Box.Item3, vehicle.Box.Item4),
                                        Width = fullWidth,
                                        Height = fullHeight,
                                        Id = "",
                                        Name = vehicle.Type + "-" + Math.Round(vehicle.Conf, 2),
                                        FrameId = frame.Meta.FrameId,
                                        CapturedAtTicks = frame.Meta.CapturedAtTicks,
                                    });
                                }

                                lock (_regionsLock) { this.ObjectBoxes = boxes; }
                            }
                            else
                            {
                                if (motionDetector != null)
                                {
                                    _currentMotionLevel = motionDetector.ProcessFrame(bmp) * 1000;

                                    if (CollectMotionLevelSamples)
                                    {
                                        lock (_motionLevelSamplesLock)
                                        {
                                            _motionLevelSamples.Add(_currentMotionLevel);
                                        }
                                    }

                                    _motionTriggerDetector.HighThreshold = _toMotionAlarmLevel;
                                    _motionTriggerDetector.LowThreshold = _fromMotionAlarmLevel;
                                    _motionTriggerDetector.MinStillSamples = MotionTriggerMinStillSamples;
                                    if (_motionTriggerDetector.Update(_currentMotionLevel))
                                    {
                                        _ = Task.Run(() => MotionTriggerDetected?.Invoke(this, EventArgs.Empty), token);
                                    }

                                    if (!EnableMotionEventGate || CollectMotionLevelSamples || ShouldRaiseMotionEvent(_currentMotionLevel))
                                    {
                                        var data = new MotionAnalyzingEventArgs((float)_currentMotionLevel, null, null, "");
                                        _ = Task.Run(() => MotionAnalyzing?.Invoke(this, data), token);
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            /* Ignore */
                        }
                        finally
                        {
                            // C2 fix (RAM leak BUG-004): bmp là WriteableBitmap → AvaloniaFramePool.
                            AvaloniaFramePool.Return(bmp);
                        }
                    }
                }
                catch
                {
                    /* Ignore */
                }
                await Task.Delay(10, token);
            }
        }

        // ---- AI Detection consumer (MolexDetection) ----
        private async Task ConsumerAiDetectionAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    if (_decoder == null)
                    {
                        await Task.Delay(100, token);
                        continue;
                    }

                    await foreach (var packet in _decoder.AiChannel.Reader.ReadAllAsync(token))
                    {
                        var bmp = packet.Bitmap;
                        var meta = packet.Meta;

                        if (!enableBoxAIDetection)
                        {
                            AvaloniaFramePool.Return(bmp);
                            continue;
                        }

                        try
                        {
                            if (enableBoxAIDetection && AiDetectBoxes is not null)
                            {
                                foreach (var box in AiDetectBoxes)
                                {
                                    // Fix (2026-07-20, loại bỏ SkiaSharp): CropWriteableBitmap (internal,
                                    // VideoStreamDecoderIntptr.cs) thay CropSKBitmap — trả bitmap RENT từ
                                    // AvaloniaFramePool, PHẢI Return() sau khi dùng (không Dispose trực tiếp).
                                    // box.Rect là KzRect (ADR §6.3)
                                    WriteableBitmap? cropped = VideoStreamDecoderIntptr.CropWriteableBitmap(bmp, box.Rect);
                                    if (cropped == null) continue;

                                    try
                                    {
                                        var result = CheckAiDetection(box.BoxName, box.Confidence, cropped, box.CameraId);
                                        if (result != null)
                                        {
                                            bool isStatusChanged = box.UpdateStatus(result?.ClassName ?? EmClassification.Unknown);
                                            box.ScoreConfidence = result?.Confidence ?? 0;
                                            box.FrameId = meta.FrameId;
                                            box.CapturedAtTicks = meta.CapturedAtTicks;
                                            if (isStatusChanged)
                                            {
                                                // AvaloniaFramePool.Clone thay bmp.Copy() (SKBitmap) — bản sao riêng
                                                // (KHÔNG qua pool) vì cần giữ lâu cho event xử lý bất đồng bộ.
                                                var fullFrameClone = AvaloniaFramePool.Clone(bmp);
                                                var boxedFrame = DrawRectWriteable(fullFrameClone, box.Rect.X, box.Rect.Y, box.Rect.Width, box.Rect.Height);
                                                try
                                                {
                                                    AIDetectAnalyzing?.Invoke(this,
                                                        new AIDetectAnalyzingEventArgs(result, box.BoxName, box.CameraId, meta.FrameId, meta.CapturedAtTicks),
                                                        boxedFrame);
                                                }
                                                finally
                                                {
                                                    // MEMORY LEAK FIX: fullFrameClone (= boxedFrame, DrawRectWriteable vẽ in-place
                                                    // và trả về CÙNG bitmap) là bản sao sở hữu riêng, KHÔNG qua pool. Không có
                                                    // subscriber nào cho AIDetectAnalyzing trong toàn bộ app (đã kiểm tra) — nếu
                                                    // không dispose ở đây, native memory rò rỉ mỗi lần AI box đổi trạng thái.
                                                    boxedFrame.Dispose();
                                                }
                                            }
                                        }
                                    }
                                    finally
                                    {
                                        AvaloniaFramePool.Return(cropped);
                                    }
                                }
                            }
                        }
                        finally
                        {
                            AvaloniaFramePool.Return(bmp);
                        }
                    }
                }
                catch { /* Ignore */ }
                await Task.Delay(10, token);
            }
        }

        // ---- LPR dedup ----
        #region Nhận dạng biển số (dedup)
        internal bool IsRecentDuplicatePlate(string plateText) => _plateDuplicateFilter.IsRecentDuplicate(plateText);

        internal bool ShouldReadPlate(float currentMotionLevel)
            => ShouldReadPlate(currentMotionLevel, _fromMotionAlarmLevel, _toMotionAlarmLevel);

        internal bool ShouldRaiseMotionEvent(float currentMotionLevel)
            => ShouldReadPlate(currentMotionLevel, _fromMotionAlarmLevel, _toMotionAlarmLevel);

        internal static bool ShouldReadPlate(float currentMotionLevel, double from, double to)
        {
            if (from == 0 && to == 0) return true;
            return currentMotionLevel <= to && currentMotionLevel >= from;
        }

        internal (double From, double To) SuggestMotionAlarmBand(double minConfidence = 95.0, double binSize = 5.0, int minSamplesPerBin = 3)
            => SuggestMotionAlarmBand(CalibrationSamples, minConfidence, binSize, minSamplesPerBin);

        internal static (double From, double To) SuggestMotionAlarmBand(
            IReadOnlyList<(float MotionLevel, float Confidence)> samples,
            double minConfidence = 95.0,
            double binSize = 5.0,
            int minSamplesPerBin = 3)
        {
            if (samples == null || samples.Count == 0 || binSize <= 0)
                return (0, 0);

            var binMap = samples
                .GroupBy(s => (long)Math.Floor(s.MotionLevel / binSize))
                .ToDictionary(g => g.Key, g => (Avg: g.Average(x => x.Confidence), Count: g.Count()));

            long minKey = binMap.Keys.Min();
            long maxKey = binMap.Keys.Max();

            double bestFrom = 0, bestTo = 0;
            long bestLen = 0;
            long? runStartKey = null;

            for (long key = minKey; key <= maxKey + 1; key++)
            {
                bool qualifies = binMap.TryGetValue(key, out var bin)
                    && bin.Count >= minSamplesPerBin
                    && bin.Avg >= minConfidence;

                if (qualifies)
                {
                    runStartKey ??= key;
                }
                else if (runStartKey != null)
                {
                    long len = key - runStartKey.Value;
                    if (len > bestLen)
                    {
                        bestLen = len;
                        bestFrom = runStartKey.Value * binSize;
                        bestTo = key * binSize;
                    }
                    runStartKey = null;
                }
            }

            return bestLen == 0 ? (0, 0) : (bestFrom, bestTo);
        }
        #endregion

        // ---- Motion Trigger (chế độ VirtualLoopType.MotionDetection) ----
        #region Motion Trigger — pattern Mạnh→Nhẹ→Mạnh (xe qua barrie)
        internal (double HighThreshold, double LowThreshold) SuggestMotionTriggerThresholds(
            double lowPercentile = 25.0, double highPercentile = 75.0, int numBins = 64)
            => SuggestMotionTriggerThresholds(MotionLevelSamples, lowPercentile, highPercentile, numBins);

        internal static (double HighThreshold, double LowThreshold) SuggestMotionTriggerThresholds(
            IReadOnlyList<float> samples,
            double lowPercentile = 25.0,
            double highPercentile = 75.0,
            int numBins = 64)
        {
            if (samples == null || samples.Count < 4)
                return (0, 0);

            double otsuThreshold = ComputeOtsuThreshold(samples, numBins);

            var lowGroup = samples.Where(s => s <= otsuThreshold).OrderBy(s => s).ToList();
            var highGroup = samples.Where(s => s > otsuThreshold).OrderBy(s => s).ToList();

            if (lowGroup.Count == 0 || highGroup.Count == 0)
                return (0, 0);

            double low = Percentile(lowGroup, lowPercentile);
            double high = Percentile(highGroup, highPercentile);

            return (high, low);
        }

        private static double ComputeOtsuThreshold(IReadOnlyList<float> samples, int numBins)
        {
            float min = samples[0], max = samples[0];
            foreach (var s in samples)
            {
                if (s < min) min = s;
                if (s > max) max = s;
            }
            if (max <= min || numBins <= 0)
                return min;

            double binWidth = (max - min) / numBins;
            var hist = new int[numBins];
            foreach (var s in samples)
            {
                int idx = (int)((s - min) / binWidth);
                if (idx >= numBins) idx = numBins - 1;
                if (idx < 0) idx = 0;
                hist[idx]++;
            }

            int total = samples.Count;
            double sumAll = 0;
            for (int i = 0; i < numBins; i++) sumAll += i * hist[i];

            long weightBelow = 0;
            double sumBelow = 0;
            double bestVariance = -1;
            int bestBin = 0;

            for (int t = 0; t < numBins; t++)
            {
                weightBelow += hist[t];
                sumBelow += t * hist[t];
                if (weightBelow == 0) continue;

                long weightAbove = total - weightBelow;
                if (weightAbove == 0) break;

                double meanBelow = sumBelow / weightBelow;
                double meanAbove = (sumAll - sumBelow) / weightAbove;
                double diff = meanBelow - meanAbove;
                double variance = weightBelow * (double)weightAbove * diff * diff;

                if (variance > bestVariance)
                {
                    bestVariance = variance;
                    bestBin = t;
                }
            }

            return min + (bestBin + 1) * binWidth;
        }

        private static double Percentile(List<float> sortedAscending, double percentile)
        {
            if (sortedAscending.Count == 1) return sortedAscending[0];

            double rank = percentile / 100.0 * (sortedAscending.Count - 1);
            int lo = (int)Math.Floor(rank);
            int hi = (int)Math.Ceiling(rank);
            if (lo == hi) return sortedAscending[lo];

            double frac = rank - lo;
            return sortedAscending[lo] + (sortedAscending[hi] - sortedAscending[lo]) * frac;
        }
        #endregion

        // ---- Recording ----
        #region Ghi video
        internal void StartManualRecord()
        {
            lock (_recordLock)
            {
                if (!EnableRecording) return;
                EnableRecording = true;
            }

            StartFFmpegProcess();

            OnNewFrame -= RecordingHandler;
            OnNewFrame += RecordingHandler;
        }

        internal void StopManualRecord()
        {
            lock (_recordLock)
            {
                if (!EnableRecording) return;
                EnableRecording = false;
            }

            OnNewFrame -= RecordingHandler;
            Task.Run(StopFFmpegProcess);
        }

        private void StartFFmpegProcess()
        {
            try
            {
                if (!Directory.Exists(_recordingFolder))
                    Directory.CreateDirectory(_recordingFolder);

                string dateFolder = DateTime.Now.ToString("yyyyMMdd");
                string fullFolderPath = Path.Combine(_recordingFolder, dateFolder);

                if (!Directory.Exists(fullFolderPath))
                    Directory.CreateDirectory(fullFolderPath);

                string fileName = $"{DateTime.Now:HHmmss}.mp4";
                string pathSave = Path.Combine(fullFolderPath, fileName);

                float recordFps = averageFps > 5 ? averageFps : 25.0f;

                string args = $"-y -f image2pipe -c:v mjpeg -r {recordFps} -i - -c:v libx264 -pix_fmt yuv420p -preset ultrafast -loglevel error \"{pathSave}\"";

                var processInfo = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = args,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardInput = true,
                    RedirectStandardError = true,
                    RedirectStandardOutput = false
                };

                _recordProcess = new Process { StartInfo = processInfo };
                _recordProcess.ErrorDataReceived += (s, e) => { /* ignored */ };
                _recordProcess.Start();
                _recordProcess.BeginErrorReadLine();

                _recordStream = _recordProcess.StandardInput.BaseStream;
                _currentSegmentStartTime = DateTime.Now;

                Debug.WriteLine($"[REC] Started segment: {pathSave}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error starting FFmpeg: {ex.Message}");
                EnableRecording = false;
            }
        }

        private void StopFFmpegProcess()
        {
            try
            {
                if (_recordStream != null)
                {
                    _recordStream.Flush();
                    _recordStream.Close();
                    _recordStream.Dispose();
                    _recordStream = null;
                }

                if (_recordProcess != null)
                {
                    if (!_recordProcess.WaitForExit(15000))
                        _recordProcess.Kill();
                    _recordProcess.Dispose();
                    _recordProcess = null;
                }
                Debug.WriteLine("[REC] Segment saved.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error stopping FFmpeg: {ex.Message}");
            }
        }

        private void RecordingHandler(object sender, FrameEventArgs e)
        {
            if (!EnableRecording) return;

            lock (_recordLock)
            {
                if (_recordStream != null && (DateTime.Now - _currentSegmentStartTime).TotalMinutes >= _recordingVideoLength)
                {
                    Debug.WriteLine("[REC] Time limit reached. Rotating file...");
                    StopFFmpegProcess();
                    StartFFmpegProcess();
                }

                if (_recordStream != null && _recordStream.CanWrite)
                {
                    try
                    {
                        if (e.FrameStream is byte[] bytes)
                        {
                            _recordStream.Write(bytes, 0, bytes.Length);
                        }
                    }
                    catch
                    {
                        /* Bỏ qua lỗi ghi lẻ tẻ */
                    }
                }
            }
        }

        internal async Task RunSaveImageforVideo(CancellationToken token)
        {
            if (!this._enableRecording)
                return;

            while (!token.IsCancellationRequested)
            {
                try
                {
                    // Fix (2026-07-20, loại bỏ SkiaSharp): GetCurrentFrameFullResWriteable() (thay
                    // GetCurrentSKBitmap) — WriteableBitmap.Save(stream, quality) encode JPEG trực tiếp,
                    // không cần SKImage trung gian.
                    WriteableBitmap? currentBmp = GetCurrentFrameFullResWriteable();
                    if (currentBmp != null)
                    {
                        MemoryStream ms = new();
                        currentBmp.Save(ms, 90);
                        pre_imgs.Enqueue(ms);
                        OnNewFrame?.Invoke(this, new FrameEventArgs(ms.ToArray()));

                        currentBmp.Dispose();
                    }

                    DateTime now = DateTime.Now;
                    if (lastFrameTime != DateTime.MinValue)
                    {
                        float deltaSeconds = (float)(now - lastFrameTime).TotalSeconds;
                        last5Deltas.Enqueue(deltaSeconds);
                        if (last5Deltas.Count > maxDeltas) last5Deltas.Dequeue();

                        float avgDelta = last5Deltas.Average();
                        if (avgDelta > 0) averageFps = (float)(1.0 / avgDelta);
                    }
                    lastFrameTime = now;
                }
                catch { }
                await Task.Delay(40, token);
            }
        }

        internal async void SaveVideo(string directory)
        {
            if (string.IsNullOrEmpty(directory)) return;

            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            var pathSave = Path.Combine(directory, DateTime.Now.ToString("yyyyMMdd_HHmmss_ffff") + ".mp4");
            if (pre_imgs.Count == 0) return;

            float currentFps = averageFps > 0 ? averageFps : 15;
            if (lastSaveTime is null)
            {
                isSavingVideo = true;
                lastSaveTime = DateTime.Now;
                frame_length = 330;
            }
            else
            {
                var seconds = (int)((DateTime.Now - lastSaveTime).Value.TotalSeconds);
                frame_length += (int)(seconds * currentFps);
                return;
            }

            var pre_snapshot = pre_imgs.ToArray(frame_length);
            var post_snapshot = new List<MemoryStream>();

            var tcs = new TaskCompletionSource<bool>();

            NewFrameEventforSaveHandler handler = null;
            handler = (sender, e) =>
            {
                if (post_snapshot.Count < frame_length)
                    post_snapshot.Add(new MemoryStream(e.FrameStream));

                if (post_snapshot.Count >= frame_length)
                    tcs.TrySetResult(true);
            };

            OnNewFrame += handler;
            await Task.WhenAny(tcs.Task, Task.Delay(60000));

            isSavingVideo = false;
            lastSaveTime = null;
            OnNewFrame -= handler;

            _ = Task.Run(() =>
            {
                FFMPEG.SaveVideoWithFFmpeg(pathSave, pre_snapshot, [.. post_snapshot], currentFps);
            });
        }
        #endregion

        // ---- Frame snapshot ----
        #region Lấy hình ảnh
        /// <summary>Fix (2026-07-20, loại bỏ SkiaSharp): trả WriteableBitmap tại kích thước tùy chỉnh.</summary>
        internal WriteableBitmap? GetCurrentVideoFrame(int destWidth, int destHeight)
        {
            return _decoder?.GetCurrentFrameResizedWriteable(destWidth, destHeight);
        }

        /// <summary>Snapshot full-res (WriteableBitmap, không SKBitmap) — dùng cho GetCurrentVideoFrame()/2() ở AnvPlayer.</summary>
        internal WriteableBitmap? GetCurrentFrameFullResWriteable()
        {
            return _decoder?.GetCurrentFrameFullResWriteable();
        }

        /// <summary>Kích thước native của frame hiện tại, không decode pixel — dùng cho label thông tin.</summary>
        internal (int Width, int Height)? GetCurrentFrameSize()
        {
            return _decoder?.GetCurrentFrameSize();
        }

        internal void SaveCurrentVideoFrame(string path)
        {
            var currentFrame = GetCurrentFrameFullResWriteable();
            if (currentFrame is null) return;
            try
            {
                using var stream = File.OpenWrite(path);
                currentFrame.Save(stream);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SaveCurrentVideoFrame] {ex.Message}");
            }
            finally
            {
                currentFrame.Dispose();
            }
        }
        #endregion

        // ---- Thread-safe snapshot của regions/boxes cho OnPaint ----
        internal (List<ObjectBox> lpr, List<ObjectBox> motion, List<ObjectBox> face, List<ObjectBox> objects)
            GetRegionsSnapshot()
        {
            lock (_regionsLock)
            {
                return ([.. lprDetectRegions], [.. motionRegions], [.. faceDetectRegions], [.. ObjectBoxes]);
            }
        }

        // ---- AI Detection helper (MolexDetection) ----
        #region AI Detection
        /// <summary>
        /// Fix (2026-07-20, loại bỏ SkiaSharp): nhận WriteableBitmap (thay SKBitmap).
        /// boxAiDetector.Predict(WriteableBitmap, float) — MolexDetection đã port sang WriteableBitmap.
        /// Thay MessageBox.Show → Debug.WriteLine (cross-platform).
        /// </summary>
        internal PredictionResult? CheckAiDetection(string boxName, float conf, WriteableBitmap bitmap, string cameraId)
        {
            try
            {
                if (bitmap == null) return null;
                using var bmp = AvaloniaFramePool.Clone(bitmap);
                if (boxAiDetector != null && bmp != null)
                {
                    return boxAiDetector.Predict(bmp, conf);
                }
            }
            catch (Exception ex)
            {
                // MessageBox.Show → Debug.WriteLine (cross-platform)
                Debug.WriteLine($"[CheckAiDetection] {ex.Message}");
            }
            return null;
        }
        #endregion

        // ---- Private helpers ----
        #region Private helpers

        /// <summary>
        /// Vẽ viền hình chữ nhật màu đỏ lên WriteableBitmap (thay SKCanvas.DrawRect) — modify in-place
        /// bằng ghi trực tiếp pixel BGRA vào 4 cạnh, rồi trả về cùng bitmap để tiện dùng như expression.
        /// Caller truyền vào fullFrameClone đã là copy riêng — an toàn khi sửa trực tiếp.
        /// </summary>
        private static unsafe WriteableBitmap DrawRectWriteable(WriteableBitmap bmp, int x, int y, int w, int h)
        {
            if (bmp == null || w <= 0 || h <= 0) return bmp;

            const int strokeWidth = 2;
            using var fb = bmp.Lock();
            byte* basePtr = (byte*)fb.Address.ToPointer();
            int stride = fb.RowBytes;
            int imgW = bmp.PixelSize.Width;
            int imgH = bmp.PixelSize.Height;

            void SetPixel(int px, int py)
            {
                if (px < 0 || py < 0 || px >= imgW || py >= imgH) return;
                byte* p = basePtr + py * stride + px * 4; // Bgra8888
                p[0] = 0; p[1] = 0; p[2] = 255; p[3] = 255; // Red
            }

            for (int t = 0; t < strokeWidth; t++)
            {
                for (int px = x; px < x + w; px++)
                {
                    SetPixel(px, y + t);
                    SetPixel(px, y + h - 1 - t);
                }
                for (int py = y; py < y + h; py++)
                {
                    SetPixel(x + t, py);
                    SetPixel(x + w - 1 - t, py);
                }
            }

            return bmp;
        }

        #endregion
    }
}
