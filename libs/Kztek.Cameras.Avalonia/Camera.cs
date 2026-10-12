// G4.4: Port từ Camera.cs (WinForms).
// Thay đổi System.Drawing:
//   - Bitmap GetCurrentVideoFrame() / Task<Bitmap> GetCurrentVideoFrameAsync() → SKBitmap?
//   - videoSourcePlayer_NewFrame delegate: ref Bitmap → ref SKBitmap (khớp NewFrameHandler G4.1)
//   - VideoSourcePlayer_AIDetectAnalyzing tham số Image eventImage → SKBitmap? eventImage (khớp AIDetectAnalyzingEventHandler G4.1)
//   - List<Rectangle> DetectVehicleTypeRegions → List<KzRect>
//   - List<Rectangle> faceDetectRegions → List<KzRect>
//
// EXPECTED COMPILE ERRORS (chờ G4.3 + G4.5):
//   CS0246 iCameraSourcePlayer   — sẽ có sau G4.5 (port Players/iCameraSourcePlayer.cs)
//   CS0246 AIBoxDetect           — định nghĩa trong AnvPlayer.cs (G4.3, chưa port)
//   CS0246 AnvPlayer             — Players/FFMPEG/UserControls/AnvPlayer.cs (G4.3, chưa port)
//   CS0246 MotionTriggerPhase    — định nghĩa trong MotionTriggerDetector.cs (G4.3, chưa port)
// Các lỗi này là expected và đã ghi vào Handoff Log G4.4.
using Avalonia.Media.Imaging;
using Kztek.Cameras.Handlers;
using Kztek.Cameras.TrackingObjects;
using static Kztek.Object.MultyPlatform.ConfigObjects.VirtualLoopType;

namespace Kztek.Cameras
{
    public class Camera
    {
        /// <summary>
        /// Đối tượng player thực tế hiển thị/stream video (KzPlayer, AnvPlayer, HikPlayer, VideoSourcePlayer...).
        /// </summary>
        public iCameraSourcePlayer videoSourcePlayer;

        #region ==== THÔNG TIN CAMERA CƠ BẢN ====
        public bool isAllClose() => !this.videoSourcePlayer?.AiDetectBoxes?.Where(e => e.classification == AiDetection.EmClassification.Open)?.Any() ?? true;
        private string id;
        private string name = "Camera";
        private string code = "C";

        private int groupID;
        private string videoSource = "video source";

        private int httpPort;
        private int rtspPort = 554;
        private int serverPort;

        /// <summary>
        /// Kênh logic của camera / đầu ghi.
        /// Ví dụ với NVR: Channel 1, 2, 3...
        /// </summary>
        private int chanel;

        /// <summary>
        /// Chỉ số luồng (stream) trên cùng một channel.
        /// Ví dụ:
        /// 0 = main stream (full HD),
        /// 1 = sub stream (preview nhẹ),
        /// 2 = stream khác...
        /// </summary>
        private int streamIndex = 1;

        private string login = "";
        private string password = "";

        private CameraType cameraType;
        private StreamType streamType = StreamType.H264;
        private ProtocolType protocolType;

        private string resolution = "640x480";
        private string description = "";

        /// <summary>ID định danh camera trong hệ thống (GUID, mã DB...).</summary>
        public string ID { get => id; set => id = value; }

        /// <summary>Tên hiển thị của camera.</summary>
        public string Name { get => name; set => name = value; }

        /// <summary>Mã rút gọn của camera (code nội bộ).</summary>
        public string Code { get => code; set => code = value; }

        /// <summary>Nhóm camera (nếu dùng phân nhóm).</summary>
        public int GroupID { get => groupID; set => groupID = value; }

        /// <summary>
        /// Địa chỉ nguồn video:
        /// - Với IP: IP hoặc hostname (ví dụ 192.168.1.10).
        /// - Với capture device: tên device.
        /// </summary>
        public string VideoSource { get => videoSource; set => videoSource = value; }

        /// <summary>Cổng HTTP dùng cho một số camera (Hikvision, v.v...).</summary>
        public int HttpPort { get => httpPort; set => httpPort = value; }

        /// <summary>Cổng RTSP (mặc định 554).</summary>
        public int RtspPort { get => rtspPort; set => rtspPort = value; }

        /// <summary>Cổng server (nếu dùng SDK riêng: Hikvision, v.v...).</summary>
        public int ServerPort { get => serverPort; set => serverPort = value; }

        /// <summary>Channel logic của camera (camera thứ mấy trên đầu ghi / kênh thứ mấy).</summary>
        public int Chanel { get => chanel; set => chanel = value; }

        /// <summary>Chỉ định luồng stream của channel hiện tại (0 = main, 1 = sub, ... tuỳ từng hãng).</summary>
        public int StreamIndex { get => streamIndex; set => streamIndex = value; }

        /// <summary>Tài khoản login cho camera (IP/RTSP/HTTP).</summary>
        public string Login { get => login; set => login = value; }

        /// <summary>Mật khẩu login cho camera.</summary>
        public string Password { get => password; set => password = value; }

        /// <summary>Loại camera (HIKVISION, Dahua, VideoCaptureDevice, Custom...).</summary>
        public CameraType CameraType { get => cameraType; set => cameraType = value; }

        /// <summary>Kiểu stream (H264, MJPEG, H265...).</summary>
        public StreamType StreamType { get => streamType; set => streamType = value; }

        /// <summary>Kiểu giao thức (RTSP, HTTP...).</summary>
        public ProtocolType ProtocolType { get => protocolType; set => protocolType = value; }

        /// <summary>Độ phân giải mong muốn: "WxH" (ví dụ "1280x720").</summary>
        public string Resolution { get => resolution; set => resolution = value; }

        /// <summary>Ghi chú mô tả camera.</summary>
        public string Description { get => description; set => description = value; }
        #endregion

        /// <summary>Camera có đang chạy stream không.</summary>
        public bool IsRunning { get => videoSourcePlayer?.IsRunning ?? false; }

        #region ==== SỰ KIỆN PUBLIC ====
        /// <summary>Raised trong quá trình phân tích motion, trả về mức độ motion.</summary>
        public event MotionAnalyzingEventHandler MotionAnalyzing;
        public event AIDetectAnalyzingEventHandler AIDetectAnalyzing;

        /// <summary>Raised khi vừa phát hiện xong pattern xe qua barrie (Mạnh→Nhẹ→Mạnh).</summary>
        public event EventHandler MotionTriggerDetected;

        /// <summary>Raised khi có lỗi trong quá trình xử lý camera.</summary>
        public event CameraErrorEventHandler CameraError;
        #endregion

        public static string AI_ACTIVE_CODE = "";

        #region ==== START / STOP / FRAME API ====
        private List<AIBoxDetect> aiDetectBoxs = [];

        /// <summary>
        /// Bắt đầu kết nối và phát stream camera.
        /// </summary>
        public void Start(double fromMotionAlarmLevel, double toMotionAlarmLevel, int motionDetectionInterval,
                          bool enableAIDetection = false, List<AIBoxDetect> aiDetectBoxs = null,
                          bool enableRecording = false, EmVirtualLoopType virtualLoopType = EmVirtualLoopType.None)
        {
            this.aiDetectBoxs = aiDetectBoxs;
            this.virtualLoopType = virtualLoopType;
            this.fromMotionAlarmLevel = fromMotionAlarmLevel;
            this.toMotionAlarmLevel = toMotionAlarmLevel;
            this.motionDetectionInterval = motionDetectionInterval;
            CloseVideoSource();

            // Tách URL building ra CameraRtspUrlBuilderFactory — mỗi hãng 1 method riêng.
            // Thêm hãng mới: chỉ cần thêm method + đăng ký trong factory, không sửa file này.
            if (CameraRtspUrlBuilderFactory.TryBuild(cameraType, this, out var buildResult))
            {
                var videoStream = new CameraStreamInfo { Source = buildResult.Url };
                videoSourcePlayer = new AnvPlayer(virtualLoopType, fromMotionAlarmLevel, toMotionAlarmLevel, enableAIDetection, enableRecording)
                {
                    MotionDetectionInterval = motionDetectionInterval,
                    AiDetectBoxes = buildResult.SetAiDetectBoxes ? aiDetectBoxs : null
                };
                OpenVideoSource(videoStream);
            }

            if (this.videoSourcePlayer != null)
            {
                this.videoSourcePlayer.AIDetectAnalyzing -= VideoSourcePlayer_AIDetectAnalyzing;
                this.videoSourcePlayer.AIDetectAnalyzing += VideoSourcePlayer_AIDetectAnalyzing;
                this.videoSourcePlayer.TrackingConfig = this.trackingConfig;
                this.videoSourcePlayer.VirtualLoopType = virtualLoopType;
            }
        }

        private void VideoSourcePlayer_AIDetectAnalyzing(object sender, AIDetectAnalyzingEventArgs e, WriteableBitmap? eventImage)
        {
            this.AIDetectAnalyzing?.Invoke(this, e, eventImage);
        }

        /// <summary>Gửi tín hiệu dừng (SignalToStop) cho VideoSourcePlayer nhưng không giải phóng resource.</summary>
        public void SignalToStop()
        {
            if (videoSourcePlayer != null)
            {
                videoSourcePlayer.SignalToStop();
            }
        }

        /// <summary>Dừng camera và giải phóng các resource liên quan (PTZ, snapshot, videoSourcePlayer).</summary>
        public void Stop()
        {
            CloseVideoSource();
        }
        #endregion

        /// <summary>Khởi tạo VideoSourcePlayer, bind event, cấu hình overlay và bắt đầu stream.</summary>
        private void OpenVideoSource(CameraStreamInfo source)
        {
            try
            {
                videoSourcePlayer ??= new AnvPlayer();

                source.Login = login;
                source.Password = password;
                videoSourcePlayer.RecordingFolder = recordingFolder;
                videoSourcePlayer.RecordingVideoLength = recordingVideoLength;
                videoSourcePlayer.VideoSource = source;

                videoSourcePlayer.NewFrame += videoSourcePlayer_NewFrame;

                videoSourcePlayer.Start();
                UpdateMotionDetection();
            }
            catch (Exception ex)
            {
                CameraError?.Invoke(this, ex.ToString());
            }
        }

        /// <summary>Dừng videoSourcePlayer, PTZ, snapshot, hủy event và giải phóng tham chiếu.</summary>
        private void CloseVideoSource()
        {
            try
            {
                if (videoSourcePlayer == null)
                {
                    return;
                }

                videoSourcePlayer.NewFrame -= videoSourcePlayer_NewFrame;
                videoSourcePlayer.SignalToStop();

                // Chờ tối đa 500ms (20 x 25ms) để player tự dừng sau SignalToStop.
                for (int i = 0; i < 20 && videoSourcePlayer != null && videoSourcePlayer.IsRunning; i++)
                {
                    Thread.Sleep(25);
                }

                // BUG-004 Fix 3: Luôn gọi Stop() — không phụ thuộc IsRunning.
                // Camera chưa bao giờ connect: IsRunning = false ngay từ đầu → Stop() bị bỏ qua
                // → _decoder không được dispose → native FFmpeg memory leak vĩnh viễn (no finalizer).
                // Stop() là idempotent: gọi khi đã stopped không gây lỗi.
                if (videoSourcePlayer != null)
                {
                    videoSourcePlayer.Stop();
                }

                videoSourcePlayer = null;
            }
            catch (Exception ex)
            {
                CameraError?.Invoke(this, ex.ToString());
            }
        }

        #region ==== EVENT HANDLERS NỘI BỘ ====
        /// <summary>
        /// Event nội bộ: nhận frame mới từ VideoSourcePlayer.
        /// Delegate thay đổi: ref Bitmap → ref SKBitmap (khớp NewFrameHandler G4.1).
        /// </summary>
        private void videoSourcePlayer_NewFrame(object sender, ref WriteableBitmap? image)
        {
            //NewFrame?.Invoke(this, new NewFrameEventArgs(new Bitmap(image)));
        }
        #endregion

        #region Nhận dạng biển số
        /// <summary>Danh sách vùng detect LPR đã parse sẵn.</summary>
        public List<ObjectBox> lprDetectRegions = [];

        private int duplicatePlateSuppressSeconds = 10;
        /// <summary>
        /// Thời gian (giây) lưu lại biển số đã nhận dạng để bỏ qua các lần nhận dạng trùng lặp gần đây.
        /// </summary>
        public int DuplicatePlateSuppressSeconds
        {
            get => duplicatePlateSuppressSeconds;
            set
            {
                duplicatePlateSuppressSeconds = value;
                if (videoSourcePlayer != null)
                {
                    videoSourcePlayer.DuplicatePlateSuppressSeconds = value;
                }
            }
        }
        #endregion

        #region Phát hiện phương tiện
        public bool EnableDetectVehicleType { get; set; } = false;
        public int VehicleTypeDetectionInterval { get; set; } = 100;
        /// <summary>
        /// Vùng detect vehicle type — thay List&lt;Rectangle&gt; bằng List&lt;KzRect&gt; (ADR §6.3).
        /// </summary>
        public List<KzRect> DetectVehicleTypeRegions { get; set; } = [];
        #endregion

        #region Nhận dạng mặt
        /// <summary>
        /// Vùng detect khuôn mặt — thay List&lt;Rectangle&gt; bằng List&lt;KzRect&gt; (ADR §6.3).
        /// </summary>
        public List<KzRect> faceDetectRegions = [];
        #endregion

        #region Phát hiện chuyển động
        private bool enableDisplayMotionRegion;
        /// <summary>Có hiển thị vùng motion trên video preview hay không.</summary>
        public bool EnableDisplayMotionRegion
        {
            get => enableDisplayMotionRegion;
            set
            {
                enableDisplayMotionRegion = value;
                UpdateMotionDetection();
            }
        }

        private List<ObjectBox> motionRegions = [];
        /// <summary>Các khung phát hiện chuyển động.</summary>
        public List<ObjectBox> MotionRegions
        {
            get => motionRegions;
            set => motionRegions = value;
        }

        private double fromMotionAlarmLevel = 0.15;
        /// <summary>Ngưỡng báo động chuyển động dưới (0–100).</summary>
        public double FromMotionAlarmLevel
        {
            get => fromMotionAlarmLevel;
            set { fromMotionAlarmLevel = value; UpdateMotionDetection(); }
        }

        private double toMotionAlarmLevel = 0.15;
        /// <summary>Ngưỡng báo động chuyển động trên (0–100).</summary>
        public double ToMotionAlarmLevel
        {
            get => toMotionAlarmLevel;
            set { toMotionAlarmLevel = value; UpdateMotionDetection(); }
        }

        private int motionDetectionInterval = 100;
        /// <summary>Khoảng thời gian giữa hai lần phân tích motion (ms).</summary>
        public int MotionDetectionInterval
        {
            get => motionDetectionInterval;
            set { motionDetectionInterval = value; UpdateMotionDetection(); }
        }

        private bool collectCalibrationSamples = false;
        /// <summary>
        /// Bật để thu thập mẫu (motion level, confidence) mỗi lần AI đọc được biển số —
        /// dùng cho <see cref="SuggestMotionAlarmBand"/> tự tính band [From,To] tối ưu.
        /// </summary>
        public bool CollectCalibrationSamples
        {
            get => collectCalibrationSamples;
            set
            {
                collectCalibrationSamples = value;
                if (videoSourcePlayer != null)
                    videoSourcePlayer.CollectCalibrationSamples = value;
            }
        }

        /// <summary>Mẫu (motion level, confidence) đã thu thập khi <see cref="CollectCalibrationSamples"/> đang/đã bật.</summary>
        public IReadOnlyList<(float MotionLevel, float Confidence)> CalibrationSamples
            => videoSourcePlayer?.CalibrationSamples ?? Array.Empty<(float, float)>();

        /// <summary>Xoá toàn bộ mẫu đã thu thập — gọi trước khi bắt đầu 1 lượt calibrate mới.</summary>
        public void ClearCalibrationSamples() => videoSourcePlayer?.ClearCalibrationSamples();

        /// <summary>
        /// Tự tính band [From, To] tối ưu cho <see cref="FromMotionAlarmLevel"/>/<see cref="ToMotionAlarmLevel"/>
        /// từ mẫu đã thu thập qua <see cref="CollectCalibrationSamples"/>.
        /// </summary>
        public (double From, double To) SuggestMotionAlarmBand(double minConfidence = 95.0, double binSize = 5.0, int minSamplesPerBin = 3)
            => videoSourcePlayer?.SuggestMotionAlarmBand(minConfidence, binSize, minSamplesPerBin) ?? (0, 0);

        private bool collectMotionLevelSamples = false;
        /// <summary>
        /// Bật để thu thập motion level thô mỗi frame ở chế độ MotionDetection —
        /// dùng cho <see cref="SuggestMotionTriggerThresholds"/> tự tính (HighThreshold, LowThreshold).
        /// </summary>
        public bool CollectMotionLevelSamples
        {
            get => collectMotionLevelSamples;
            set
            {
                collectMotionLevelSamples = value;
                if (videoSourcePlayer != null)
                    videoSourcePlayer.CollectMotionLevelSamples = value;
            }
        }

        /// <summary>Motion level thô đã thu thập khi <see cref="CollectMotionLevelSamples"/> đang/đã bật.</summary>
        public IReadOnlyList<float> MotionLevelSamples
            => videoSourcePlayer?.MotionLevelSamples ?? Array.Empty<float>();

        /// <summary>Xoá toàn bộ mẫu motion level thô đã thu thập.</summary>
        public void ClearMotionLevelSamples() => videoSourcePlayer?.ClearMotionLevelSamples();

        /// <summary>
        /// Tự tính (HighThreshold, LowThreshold) cho <see cref="MotionTriggerDetector"/> từ motion level thô.
        /// </summary>
        public (double HighThreshold, double LowThreshold) SuggestMotionTriggerThresholds(
            double lowPercentile = 25.0, double highPercentile = 75.0, int numBins = 64)
            => videoSourcePlayer?.SuggestMotionTriggerThresholds(lowPercentile, highPercentile, numBins) ?? (0, 0);

        private bool enableMotionEventGate = false;
        /// <summary>
        /// Bật để gate event <see cref="iCameraSourcePlayer.MotionAnalyzing"/> theo band
        /// [<see cref="FromMotionAlarmLevel"/>,<see cref="ToMotionAlarmLevel"/>].
        /// </summary>
        public bool EnableMotionEventGate
        {
            get => enableMotionEventGate;
            set
            {
                enableMotionEventGate = value;
                if (videoSourcePlayer != null)
                    videoSourcePlayer.EnableMotionEventGate = value;
            }
        }

        private int motionTriggerMinStillSamples = 3;
        /// <summary>Số mẫu liên tiếp tối thiểu ở trạng thái "đứng yên" để công nhận pha Still thật. Mặc định 3.</summary>
        public int MotionTriggerMinStillSamples
        {
            get => motionTriggerMinStillSamples;
            set
            {
                motionTriggerMinStillSamples = value;
                if (videoSourcePlayer != null)
                    videoSourcePlayer.MotionTriggerMinStillSamples = value;
            }
        }

        /// <summary>
        /// Trạng thái hiện tại của pattern xe qua barrie (Mạnh→Nhẹ→Mạnh) —
        /// <see cref="MotionTriggerPhase.Idle"/> nếu chưa Start() camera.
        /// </summary>
        public MotionTriggerPhase MotionPhase => videoSourcePlayer?.MotionPhase ?? MotionTriggerPhase.Idle;

        /// <summary>true khi xe đã tiến vào bằng chuyển động mạnh và giờ đang đứng yên/đứng chờ (pha "Nhẹ").</summary>
        public bool IsVehicleWaiting => videoSourcePlayer?.IsVehicleWaiting ?? false;

        private EmVirtualLoopType virtualLoopType;
        /// <summary>Bật/tắt giám sát chuyển động.</summary>
        public EmVirtualLoopType VirtualLoopType
        {
            get => virtualLoopType;
            set { virtualLoopType = value; UpdateMotionDetection(); }
        }

        /// <summary>Cập nhật cấu hình phát hiện chuyển động (motion detection) cho VideoSourcePlayer.</summary>
        public void UpdateMotionDetection()
        {
            if (videoSourcePlayer != null)
            {
                videoSourcePlayer.FromMotionAlarmLevel = fromMotionAlarmLevel;
                videoSourcePlayer.ToMotionAlarmLevel = toMotionAlarmLevel;
                videoSourcePlayer.MotionDetectionInterval = motionDetectionInterval;
                videoSourcePlayer.MotionRegions = new List<ObjectBox>();
                var temp = new List<ObjectBox>();
                foreach (var item in this.MotionRegions)
                {
                    if (item.BBox.Height > 0 && item.BBox.Width > 0)
                    {
                        temp.Add(item);
                    }
                }
                videoSourcePlayer.MotionRegions = temp;
                videoSourcePlayer.EnableDisplayMotionRegion = enableDisplayMotionRegion;
                videoSourcePlayer.VirtualLoopType = virtualLoopType;
                videoSourcePlayer.MotionAnalyzing -= VideoSourcePlayer_MotionAnalyzing;
                videoSourcePlayer.MotionAnalyzing += VideoSourcePlayer_MotionAnalyzing;
                videoSourcePlayer.EnableMotionEventGate = enableMotionEventGate;
                videoSourcePlayer.MotionTriggerMinStillSamples = motionTriggerMinStillSamples;
                videoSourcePlayer.MotionTriggerDetected -= VideoSourcePlayer_MotionTriggerDetected;
                videoSourcePlayer.MotionTriggerDetected += VideoSourcePlayer_MotionTriggerDetected;
            }
        }

        public void UpdateLprDetection()
        {
            if (videoSourcePlayer != null)
            {
                videoSourcePlayer.lprDetectRegions = this.lprDetectRegions;
                videoSourcePlayer.DuplicatePlateSuppressSeconds = this.duplicatePlateSuppressSeconds;
            }
        }

        private void VideoSourcePlayer_MotionAnalyzing(object sender, MotionAnalyzingEventArgs motionLevel)
        {
            MotionAnalyzing?.Invoke(this, motionLevel);
        }

        private void VideoSourcePlayer_MotionTriggerDetected(object sender, EventArgs e)
        {
            MotionTriggerDetected?.Invoke(this, e);
        }
        #endregion

        #region Lấy hình ảnh
        /// <summary>
        /// Lấy frame hiện tại từ videoSourcePlayer (nếu có).
        /// Kiểu trả về đổi: Bitmap → SKBitmap? (nhất quán ADR §6.3 + G4.1 SKBitmap precedent).
        /// Lưu ý G4.5: iCameraSourcePlayer.GetCurrentVideoFrame2() cũng phải trả SKBitmap? sau khi port.
        /// </summary>
        public WriteableBitmap? GetCurrentVideoFrame()
        {
            if (videoSourcePlayer != null)
            {
                return videoSourcePlayer.GetCurrentVideoFrame2();
            }
            return null;
        }

        public async Task<WriteableBitmap?> GetCurrentVideoFrameAsync()
        {
            return await Task.Run(() => GetCurrentVideoFrame());
        }

        /// <summary>Lưu frame hiện tại ra file với đường dẫn chỉ định.</summary>
        public void SaveCurrentVideoFrame(string path)
        {
            if (videoSourcePlayer != null)
            {
                videoSourcePlayer.SaveCurrentVideoFrame(path);
            }
        }
        #endregion

        #region Ghi video
        private bool enableRecording;
        public bool EnableRecording { get => enableRecording; set => enableRecording = value; }

        private int recordingRate = 5;
        public int RecordingRate { get => recordingRate; set => recordingRate = value; }

        private string recordingResolution = "";
        public string RecordingResolution { get => recordingResolution; set => recordingResolution = value; }

        private string recordingFolder = "";
        public string RecordingFolder { get => recordingFolder; set => recordingFolder = value; }

        private int recordingVideoLength = 30;
        public int RecordingVideoLength { get => recordingVideoLength; set => recordingVideoLength = value; }

        public void StartManualRecord()
        {
            if (this.videoSourcePlayer is null) return;
            this.videoSourcePlayer.EnableRecording = this.EnableRecording;
            this.videoSourcePlayer.RecordingRate = this.RecordingRate;
            this.videoSourcePlayer.RecordingResolution = this.RecordingResolution;
            this.videoSourcePlayer.RecordingFolder = this.RecordingFolder;
            this.videoSourcePlayer.RecordingVideoLength = this.RecordingVideoLength;
            this.videoSourcePlayer.StartManualRecord();
        }

        public void StopMaunalRecord()
        {
            this.videoSourcePlayer?.StopManualRecord();
        }

        public void SaveVideo(string pathSave)
        {
            this.videoSourcePlayer?.SaveVideo(pathSave);
        }
        #endregion

        #region Molex AI
        #endregion

        #region Tracking And Counting
        public Dictionary<string, int> countItems = new Dictionary<string, int>();
        private TrackingConfig trackingConfig;
        public TrackingConfig TrackingConfig
        {
            get => trackingConfig;
            set
            {
                trackingConfig = value;
                if (this.videoSourcePlayer != null)
                    this.videoSourcePlayer.TrackingConfig = value;
            }
        }

        public void DisplayItems(Dictionary<string, int> countItems, Dictionary<string, int> countOutItems, decimal detectTime, Dictionary<string, decimal> confidences)
        {
            this.videoSourcePlayer?.DisplayItems(countItems, countOutItems, detectTime, confidences);
        }
        #endregion

        public bool isClientLiview = false;
        public string ParentId { get; set; } = string.Empty;
        public double AlarmLevel { get; set; } = 0.85;

        public void DisplayDescription(string description)
        {
            this.videoSourcePlayer?.DisplayDescription(description);
        }
    }
}
