// Avalonia port: AnvPlayer (WinForms UserControl + PictureBox-based) → Custom Control (Avalonia render pipeline).
//
// Sub-task 5.2a — Structure (task 5.2, session này):
//   Khai báo class, constructor, field _currentSKFrame + _avaloniaFrame,
//   cache Pen/Brush, override Render(DrawingContext).
// Sub-task 5.2b — ConsumerDisplayFrame (task 5.3, session sau):
//   Channel consumer loop + Dispatcher.UIThread.Post + InvalidateVisual().
// Sub-task 5.2c — Overlay drawing (task 5.4, session sau):
//   DrawRegionList / DrawObjectBoxs / DrawTrackingLines chuyển sang DrawingContext.
// Sub-task 5.2d — Public API + events (task 5.5, session sau):
//   Forward properties/events, wire Start/Stop/SignalToStop → _service.
//
// Mapping ADR §6.2 (WinForms GDI+ → Avalonia):
//   UserControl (OnPaint + PaintEventArgs.Graphics) → Control (Render + DrawingContext)
//   Invalidate() / Update()                         → InvalidateVisual()
//   this.ClientRectangle                             → this.Bounds (Rect)
//   this.Width / this.Height                         → (int)Bounds.Width / (int)Bounds.Height
//   Volatile.Read(ref _currentImage) [Bitmap GDI+]  → Volatile.Read(ref _currentSKFrame) [SKBitmap]
//   e.Graphics.DrawImage(bmp, ClientRectangle)       → ctx.DrawImage(WriteableBitmap, Bounds)
//   BeginInvoke(Action) [WinForms UI marshal]        → Dispatcher.UIThread.Post(Action) [task 5.3]
//   new SolidBrush(Color.White)                      → Brushes.White (static IBrush)
//   new SolidBrush(Color.FromArgb(180,0,0,0))        → new SolidColorBrush(Color.FromArgb(180,0,0,0))
//   new Pen(color, width) + DashStyle/LineCap         → new Pen(IBrush, thickness, DashStyle?, PenLineCap)

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using Kztek.Cameras.AiDetection;
using Kztek.Cameras.Handlers;
using Kztek.Cameras.TrackingObjects;
using SkiaSharp;
using System.Globalization;
using static Kztek.Object.MultyPlatform.ConfigObjects.VirtualLoopType;

namespace Kztek.Cameras
{
    /// <summary>
    /// Hiển thị luồng video camera qua FFmpeg — Avalonia phiên bản.<br/>
    /// Kế thừa <see cref="Avalonia.Controls.Control"/> để tham gia Avalonia render pipeline
    /// (thay WinForms <c>UserControl</c> với <c>OnPaint</c> + GDI+).<br/>
    /// Chỉ chứa logic UI: render frame, vẽ overlay, xử lý mouse event.<br/>
    /// Business logic (decode, LPR, recording, motion detection) được ủy thác
    /// <see cref="AnvPlayerService"/> nội bộ qua composition.<br/>
    /// <para>
    /// <b>[SupportedOSPlatform("windows")] đã gỡ (2026-07-14)</b>: FFmpeg PInvoke giờ resolve qua
    /// FFmpegLoader (Windows .dll / Linux .so, cả 2 bộ đã đóng gói). LƯU Ý: <see cref="_service"/>
    /// (<see cref="AnvPlayerService"/>) vẫn dùng ReadPL SDK (nhận diện biển số) — SDK này CHỈ chạy
    /// Windows, không liên quan gì đến FFmpeg — nên trên thực tế class này (qua composition)
    /// VẪN CHỈ CHẠY ĐÚNG trên Windows cho đến khi ReadPL có bản thay thế cross-platform.
    /// Ngoài ra ABI FFmpeg Linux (major 62) chưa verify khớp struct P/Invoke viết theo version 61
    /// (Windows) — xem ADR §7 R9.
    /// </para>
    /// </summary>
    public class AnvPlayer : Control, iCameraSourcePlayer
    {
        // ---- Business logic service (composition — giữ nguyên pattern WinForms) ----
        private readonly AnvPlayerService _service;

        // ---- Frame state ----
        /// <summary>
        // ---- Thread-safe bounds cache (cập nhật từ UI thread, đọc từ bg decode thread) ----
        // Bounds trong Avalonia chỉ hợp lệ trên UI thread. Nếu bg thread đọc Bounds.Width trực tiếp
        // → luôn trả 0 (thread-unsafe) → decode ở 1×1 → ảnh zoom. Dùng volatile field thay thế.
        private volatile int _cachedWidth;
        private volatile int _cachedHeight;

        /// <summary>
        /// C2 fix (RAM leak BUG-004, 2026-07-20): Frame hiện tại từ decoder pipeline — ĐÃ ĐỔI từ SKBitmap
        /// sang <see cref="WriteableBitmap"/> Avalonia (format BGRA8888) vì đã chứng minh bằng thực
        /// nghiệm rằng SKBitmap (SkiaSharp 4.150.1) rò rỉ native memory liên tục khi ghi pixel qua
        /// GetPixels()+NotifyPixelsChanged() ở tần suất decode cao (~30fps) — xem BUG-004 vòng 11-12.
        /// WriteableBitmap ghi thẳng qua Lock()/ILockedFramebuffer (cùng cơ chế con trỏ thô như GDI+
        /// LockBits, đã test KHÔNG leak) NÊN dùng trực tiếp làm frame hiển thị, KHÔNG cần copy sang
        /// buffer trung gian nữa (loại bỏ luôn bước UpdateAvaloniaFrame/Buffer.MemoryCopy cũ mỗi Render).<br/>
        /// Được cập nhật bởi ConsumerDisplayFrame qua <c>Dispatcher.UIThread.Post</c> — luôn trên UI thread.<br/>
        /// <c>Render()</c> cũng chạy trên UI thread → không cần lock khi render.<br/>
        /// Lifecycle: ConsumerDisplayFrame swap cũ → mới bằng <c>Interlocked.Exchange</c>,
        /// frame cũ trả về pool (<c>AvaloniaFramePool.Return</c>).
        /// </summary>
        internal WriteableBitmap? _currentAvaloniaFrame; // internal để ConsumerDisplayFrame swap trực tiếp

        // ---- Cached drawing resources — tạo 1 lần, tái dùng mỗi Render để tránh GC allocation ----
        // Fixed-color brushes dùng chung (ADR §6.2: SolidBrush → IBrush)
        private readonly IBrush _brushTextWhite;
        private readonly IBrush _brushBgOverlay;

        // Color-keyed caches cho box draw functions (ADR §6.2: Dictionary<GDI.Color, GDI.Pen/SolidBrush>
        //   → Dictionary<Avalonia.Media.Color, Pen/IBrush>)
        // Dùng bởi DrawObjectBoxs / DrawAIList / DrawRegionList ở task 5.4.
        private readonly Dictionary<Avalonia.Media.Color, Pen> _boxPenCache = new();
        private readonly Dictionary<Avalonia.Media.Color, IBrush> _boxFillCache = new();

        // Tracking line pens (ADR §6.2 — GDI+ Pen + DashStyle/LineCap → Avalonia Pen immutable):
        //   GDI+ LineCap.SquareAnchor → Avalonia PenLineCap.Square (closest; anchor không có trong Avalonia)
        //   GDI+ LineCap.ArrowAnchor  → Avalonia PenLineCap.Flat  (mũi tên phải tự vẽ polygon ở 5.4)
        //   GDI+ DashStyle.Dash       → Avalonia DashStyle.Dash (tương đương)
        private readonly Pen _penTrackLineIn;       // Green, Square↔Square, Dash (in boundary)
        private readonly Pen _penTrackLineInDir;    // Green, Round→Flat (direction arrow)
        private readonly Pen _penTrackLineOut;      // Red, Square↔Square, Dash (out boundary)
        private readonly Pen _penTrackLineOutDir;   // Red, Round→Flat (direction arrow)

        // ---- Consumer loop backing state ----
        private int _pendingUIFrames = 0;
        
        /// <summary>Backing field cho event NewFrame — wired trong ConsumerDisplayFrameAsync (task 5.3).</summary>
        private NewFrameHandler? _newFrameHandler;

        /// <summary>
        /// FrameId của frame đang hiển thị — cập nhật trong Dispatcher.UIThread.Post cùng với _currentSKFrame.
        /// Dùng bởi DrawAIList (task 5.4) để kiểm tra stale box.
        /// </summary>
        private long _currentFrameId;

        /// <summary>
        /// Environment.TickCount64 khi frame đang hiển thị được decode gốc.
        /// Dùng để tính độ trễ (ms) giữa AI detection và display — task 5.4.
        /// </summary>
        private long _currentCapturedAtTicks;

        // ---- Tracking/counting display state — cập nhật bởi DisplayItems (task 5.5) ----
        private Dictionary<string, int> _countInItems  = [];
        private Dictionary<string, int> _countOutItems = [];
        private decimal _coutingTime    = 0;
        private decimal _minCoutingTime = 0;
        private decimal _maxCoutingTime = 0;
        private readonly List<decimal> _100TimesDetect           = [];
        private readonly Dictionary<string, decimal> _minConf    = [];
        /// <summary>Backing field cho TrackingConfig — cần bởi DrawTrackingLines (task 5.4).</summary>
        private TrackingConfig? _trackingConfig;

        // ---- Event backing fields (task 5.5 — Public API + events) ----
        private MotionAnalyzingEventHandler?   _motionAnalyzingHandler;
        private AIDetectAnalyzingEventHandler? _aiDetectAnalyzingHandler;
        private EventHandler?                  _motionTriggerDetectedHandler;
        private OnMouseDownHandler?            _onMouseDownHandler;
        private OnMouseUpHandler?              _onMouseUpHandler;
        private OnMouseWheelHandler?           _onMouseWheelHandler;
        private OnMouseMoveHandler?            _onMouseMoveHandler;
        private OnDoubleClickHandler?          _onDoubleClickHandler;
        private EventHandler?                  _onCameraMouseDownHandler;
        private EventHandler?                  _onCameraDoubleClickHandler;

        // ---- Phase 2.3 config — giữ nguyên behavior từ WinForms ----
        /// <summary>
        /// Ngưỡng độ trễ tối đa (ms) cho phép hiển thị box AI/LPR.<br/>
        /// Nếu <c>(Environment.TickCount64 − box.CapturedAtTicks) &gt; MaxBoxStaleMs</c> thì box bị ẩn.<br/>
        /// &lt;= 0 = tắt (luôn hiện box — hành vi y hệt trước Phase 2).
        /// </summary>
        public int MaxBoxStaleMs = 500;

        /// <summary>
        /// <c>true</c>: hiện "lag: Xms" cạnh mỗi box để đo độ trễ AI thực tế.
        /// Chỉ bật khi debug/QA. Mặc định <c>false</c>.
        /// </summary>
        public bool ShowFrameLagOverlay = false;

        // ---- Constructors ----

        /// <summary>Default constructor — Camera.cs dùng <c>videoSourcePlayer ??= new AnvPlayer()</c>.</summary>
        public AnvPlayer() : this(EmVirtualLoopType.None, 0, 0) { }

        /// <summary>
        /// Constructor đầy đủ — Camera.cs dùng
        /// <c>new AnvPlayer(virtualLoopType, from, to, enableAI, enableRecording)</c>.
        /// </summary>
        public AnvPlayer(
            EmVirtualLoopType virtualLoopType,
            double fromMotionAlarmLevel,
            double toMotionAlarmLevel,
            bool enableAIDetection = false,
            bool enableRecording = false)
        {
            // Fixme: ĐÃ BỎ MinWidth=640/MinHeight=480 — đây là lỗi port từ WinForms.
            // Ở WinForms, Width/Height=640x480 chỉ là kích thước KHỞI TẠO, Dock/Anchor của container
            // vẫn co giãn nó nhỏ lại bình thường. MinWidth/MinHeight trong Avalonia là RÀNG BUỘC CỨNG
            // tối thiểu: dù panel cha nhỏ hơn (VD tile preview ~350x120), control vẫn bị ép tối thiểu
            // 640x480 → tràn ra ngoài border (nếu cha không ClipToBounds) hoặc bị crop chỉ còn 1 góc nhỏ
            // (nếu cha có ClipToBounds) thay vì co giãn vừa khung như mong đợi.
            // Layout system quyết định kích thước thực qua Stretch alignment (mặc định); Bounds.Width/Height
            // phản ánh giá trị thực tế khớp panel cha, không cần ép kích thước tối thiểu nào.

            // Tạo service với callbacks lấy kích thước control để decode đúng resolution hiển thị.
            // ADR §6.2: UserControl.Width/Height → (int)Bounds.Width/(int)Bounds.Height
            _service = new AnvPlayerService(
                virtualLoopType,
                fromMotionAlarmLevel,
                toMotionAlarmLevel,
                enableAIDetection,
                enableRecording,
                () => _cachedWidth,   // volatile: safe từ background decode thread
                () => _cachedHeight);  // volatile: safe từ background decode thread

            // ---- Khởi tạo cached drawing resources một lần ----
            // ADR §6.2: SolidBrush(Color.White) → Brushes.White (static immutable IBrush)
            _brushTextWhite = Brushes.White;
            // ADR §6.2: SolidBrush(Color.FromArgb(180,0,0,0)) → SolidColorBrush(Avalonia Color)
            _brushBgOverlay = new SolidColorBrush(Avalonia.Media.Color.FromArgb(180, 0, 0, 0));

            // ADR §6.2: GDI+ Pen → Avalonia Pen(IBrush, thickness, DashStyle?, PenLineCap)
            _penTrackLineIn     = new Pen(Brushes.LimeGreen, 1.5, DashStyle.Dash, PenLineCap.Square);
            _penTrackLineInDir  = new Pen(Brushes.LimeGreen, 1.5);
            _penTrackLineOut    = new Pen(Brushes.Red, 1.5, DashStyle.Dash, PenLineCap.Square);
            _penTrackLineOutDir = new Pen(Brushes.Red, 1.5);

            // Wire service events → forward to public event backing fields (marshal to UI thread)
            // ADR §6.2: WinForms constructor inline += → Dispatcher.UIThread.Post (threading safety)
            _service.MotionAnalyzing       += (_, args)      => Dispatcher.UIThread.Post(() => _motionAnalyzingHandler?.Invoke(this, args));
            _service.AIDetectAnalyzing     += (_, args, img) => Dispatcher.UIThread.Post(() => _aiDetectAnalyzingHandler?.Invoke(this, args, img));
            _service.MotionTriggerDetected += (_, args)      => Dispatcher.UIThread.Post(() => _motionTriggerDetectedHandler?.Invoke(this, args));

            // Wire Avalonia pointer events → KZTEK mouse event handlers
            // ADR §6.2: WinForms OnMouseDown/Up/Move/Wheel/DoubleClick → Avalonia PointerPressed/Released/Moved/WheelChanged/DoubleTapped
            PointerPressed      += OnAvaloniaPointerPressed;
            PointerReleased     += OnAvaloniaPointerReleased;
            PointerMoved        += OnAvaloniaPointerMoved;
            PointerWheelChanged += OnAvaloniaPointerWheelChanged;
            DoubleTapped        += OnAvaloniaDoubleTapped;
        }

        /// <summary>
        /// Cache Bounds để background decode thread đọc an toàn qua volatile fields.
        /// Bounds trong Avalonia chỉ hợp lệ trên UI thread — nếu bg thread đọc trực tiếp → luon trả 0.
        /// </summary>
        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);
            if (change.Property == BoundsProperty)
            {
                var r = (Rect)change.NewValue!;
                int newWidth = Math.Max(1, (int)Math.Round(r.Width));
                int newHeight = Math.Max(1, (int)Math.Round(r.Height));

                if (newWidth > 0 && newHeight > 0 && (newWidth != _cachedWidth || newHeight != _cachedHeight))
                {
                    _cachedWidth = newWidth;
                    _cachedHeight = newHeight;
                }
            }
        }

        // ============================================================
        // RENDER — ADR §6.2: override void OnPaint(PaintEventArgs) → override void Render(DrawingContext)
        // ============================================================

        /// <summary>
        /// Vẽ frame hiện tại lên control — Avalonia render pipeline gọi method này.
        /// Mapping ADR §6.2:<br/>
        ///   <c>OnPaint(PaintEventArgs e)</c>         → <c>Render(DrawingContext context)</c><br/>
        ///   <c>e.Graphics.DrawImage(img, ClientRect)</c> → <c>context.DrawImage(_avaloniaFrame, Bounds)</c><br/>
        ///   Overlay drawing (DrawRegionList/DrawObjectBoxs/DrawTrackingLines) → task 5.4.
        /// </summary>
        private Dictionary<string, FormattedText> _ftCache = new Dictionary<string, FormattedText>();
        private FormattedText GetCachedFormattedText(string text, double fontSize, IBrush brush)
        {
            var key = $"{text}_{fontSize}_{brush}";
            if (_ftCache.TryGetValue(key, out var ft)) return ft;

            ft = new FormattedText(
                text,
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                Typeface.Default,
                fontSize,
                brush);
            
            if (_ftCache.Count > 50) _ftCache.Clear(); // prevent memory leak of the cache itself
            _ftCache[key] = ft;
            return ft;
        }

        public override void Render(DrawingContext context)
        {
            base.Render(context);

            var bounds = Bounds;
            if (bounds.Width <= 0 || bounds.Height <= 0) return;

            // QUAN TRỌNG: Bounds là tọa độ tương đối với parent (X,Y khác 0).
            // DrawingContext.DrawImage/FillRectangle dùng tọa độ LOCAL của chính control (luôn bắt đầu từ 0,0).
            // Nếu dùng Bounds trực tiếp → ảnh bị offset ra ngoài → bị clip → chỉ thấy 1 góc nhỏ (zoom).
            var localBounds = new Rect(bounds.Size); // {0, 0, Width, Height}

            // C2 fix (RAM leak BUG-004): _currentAvaloniaFrame đã là WriteableBitmap sẵn sàng để vẽ —
            // không còn bước convert từ SKBitmap (UpdateAvaloniaFrame) mỗi Render nữa.
            var avaloniaBmp = Volatile.Read(ref _currentAvaloniaFrame);

            // Nếu chưa có frame hoặc service chưa kết nối → hiện "Connecting..." placeholder
            // ADR §6.2: Graphics.FillRectangle + DrawString → DrawingContext.FillRectangle + DrawText
            if (avaloniaBmp == null || avaloniaBmp.PixelSize.Width <= 0 || avaloniaBmp.PixelSize.Height <= 0 || !_service.IsConnect)
            {
                context.FillRectangle(_brushBgOverlay, localBounds);

                var text = GetCachedFormattedText("Connecting...", 14, _brushTextWhite);

                var textOrigin = new Point(
                    (localBounds.Width  - text.Width)  / 2,
                    (localBounds.Height - text.Height) / 2);

                context.DrawText(text, textOrigin);
                return;
            }

            {
                // Dùng explicit source+dest rect (2-rect overload) để FORCE scale (không clip).
                // Stretch = Fill: ảnh vừa đầy tile, không có black bar.
                // WinForms gốc: e.Graphics.DrawImage(bmp, ClientRectangle) — cùng hành vi.
                var bmpW    = (double)avaloniaBmp.PixelSize.Width;
                var bmpH    = (double)avaloniaBmp.PixelSize.Height;
                var srcRect = new Rect(0, 0, bmpW, bmpH);
                context.DrawImage(avaloniaBmp, srcRect, localBounds);
            }

            // ---- Overlay drawing (task 5.4: GDI+ → DrawingContext) ----
            // ADR §6.2: OnPaint → Render; e.Graphics.DrawRectangle/DrawString/DrawLine → ctx.*
            int wImg = avaloniaBmp.PixelSize.Width;
            int hImg = avaloniaBmp.PixelSize.Height;
            try
            {
                var (lprSnapshot, motionSnapshot, faceSnapshot, objectBoxes) = _service.GetRegionsSnapshot();
                DrawRegionList(context, lprSnapshot,   Colors.LimeGreen,  "LPR",  wImg, hImg);
                if (_service.VirtualLoopType != EmVirtualLoopType.None)
                {
                    DrawRegionList(context, motionSnapshot, Colors.OrangeRed,  "LOOP", wImg, hImg);
                    DrawMotionLevelOverlay(context, localBounds);
                }
                DrawRegionList(context, faceSnapshot,  Colors.DeepSkyBlue, "FACE", wImg, hImg);
                if (_service.EnableBoxAIDetection)
                    DrawAIList(context, _service.AiDetectBoxes, wImg, hImg);
                DrawObjectBoxs(context, objectBoxes, wImg, hImg);
                DrawTrackingLines(context, wImg, hImg);
            }
            catch { }

        }

        // ============================================================
        // FRAME CONVERSION — WriteableBitmap → SKBitmap (on-demand, cho API SKBitmap? cũ)
        // ============================================================

        // ============================================================
        // INTERFACE MEMBERS — iCameraSourcePlayer (task 5.5 Public API + events)
        // Properties/methods/events đều forward sang _service (composition pattern).
        // ============================================================

        // ---- Properties ----
        // AiDetectBoxes: forward to service — service owns the list, AI consumer updates it.
        public List<AIBoxDetect> AiDetectBoxes
        {
            get => _service.AiDetectBoxes;
            set => _service.AiDetectBoxes = value;
        }
        public CameraStreamInfo VideoSource
        {
            get => _service.VideoSource;
            set => _service.VideoSource = value;
        }
        public bool IsRunning => _service.IsConnect;
        public bool IsPaused
        {
            get => _service.IsPaused;
            set => _service.IsPaused = value;
        }
        public double PlaybackSpeed
        {
            get => _service.PlaybackSpeed;
            set => _service.PlaybackSpeed = value;
        }
        public double CurrentPositionSeconds => _service.Decoder?.CurrentPositionSeconds ?? 0;
        public double DurationSeconds => _service.Decoder?.DurationSeconds ?? 0;
        public void SeekSeconds(double seconds) => _service.Decoder?.RequestSeekSeconds(seconds);
        public EmVirtualLoopType VirtualLoopType
        {
            get => _service.VirtualLoopType;
            set => _service.VirtualLoopType = value;
        }
        public List<ObjectBox> MotionRegions
        {
            set => _service.motionRegions = value;
        }
        public bool EnableDisplayMotionRegion
        {
            set => _service.EnableDisplayMotionRegion = value;
        }
        public double FromMotionAlarmLevel
        {
            set => _service.FromMotionAlarmLevel = value;
        }
        public double ToMotionAlarmLevel
        {
            set => _service.ToMotionAlarmLevel = value;
        }
        public int MotionDetectionInterval
        {
            get => _service.MotionDetectionInterval;
            set => _service.MotionDetectionInterval = value;
        }
        public bool CollectCalibrationSamples
        {
            get => _service.CollectCalibrationSamples;
            set => _service.CollectCalibrationSamples = value;
        }
        public IReadOnlyList<(float MotionLevel, float Confidence)> CalibrationSamples
            => _service.CalibrationSamples;
        public bool CollectMotionLevelSamples
        {
            get => _service.CollectMotionLevelSamples;
            set => _service.CollectMotionLevelSamples = value;
        }
        public IReadOnlyList<float> MotionLevelSamples => _service.MotionLevelSamples;
        public bool EnableMotionEventGate
        {
            get => _service.EnableMotionEventGate;
            set => _service.EnableMotionEventGate = value;
        }
        public int MotionTriggerMinStillSamples
        {
            get => _service.MotionTriggerMinStillSamples;
            set => _service.MotionTriggerMinStillSamples = value;
        }
        public MotionTriggerPhase MotionPhase => _service.MotionPhase;
        public bool IsVehicleWaiting => _service.IsVehicleWaiting;
        public List<ObjectBox> lprDetectRegions
        {
            get => _service.lprDetectRegions;
            set => _service.lprDetectRegions = value;
        }
        public int DuplicatePlateSuppressSeconds
        {
            get => _service.DuplicatePlateSuppressSeconds;
            set => _service.DuplicatePlateSuppressSeconds = value;
        }
        public List<ObjectBox> faceDetectRegions
        {
            get => _service.faceDetectRegions;
            set => _service.faceDetectRegions = value;
        }
        public int RecordingRate  { set => _service.RecordingRate       = value; }
        public string RecordingResolution { set => _service.RecordingResolution = value; }
        public string RecordingFolder     { set => _service.RecordingFolder     = value; }
        public int RecordingVideoLength   { set => _service.RecordingVideoLength = value; }
        public bool EnableRecording
        {
            get => _service.EnableRecording;
            set => _service.EnableRecording = value;
        }
        public TrackingConfig? TrackingConfig
        {
            get => _trackingConfig;
            set => _trackingConfig = value;
        }
        public List<ObjectBox> ObjectBoxes
        {
            get => _service.ObjectBoxes;
            set => _service.ObjectBoxes = value;
        }

        // ---- Events ----
        // NewFrame: wired in ConsumerDisplayFrameAsync (task 5.3) — backing field already exists.
        public event NewFrameHandler NewFrame
        {
            add    => _newFrameHandler += value;
            remove => _newFrameHandler -= value;
        }
        // MotionAnalyzing / AIDetectAnalyzing / MotionTriggerDetected: forwarded from _service (wired in constructor).
        // ADR §6.2: fire via Dispatcher.UIThread.Post — subscribers on UI thread are safe.
        public event MotionAnalyzingEventHandler MotionAnalyzing
        {
            add    => _motionAnalyzingHandler    += value;
            remove => _motionAnalyzingHandler    -= value;
        }
        public event AIDetectAnalyzingEventHandler AIDetectAnalyzing
        {
            add    => _aiDetectAnalyzingHandler  += value;
            remove => _aiDetectAnalyzingHandler  -= value;
        }
        public event EventHandler MotionTriggerDetected
        {
            add    => _motionTriggerDetectedHandler += value;
            remove => _motionTriggerDetectedHandler -= value;
        }
        // Mouse events — forwarded from Avalonia pointer events (wired in constructor).
        // ADR §6.2: WinForms mouse events → Avalonia PointerPressed/Released/Moved/WheelChanged/DoubleTapped
        public event OnMouseDownHandler OnMouseDownEvent
        {
            add    => _onMouseDownHandler    += value;
            remove => _onMouseDownHandler    -= value;
        }
        public event OnMouseUpHandler OnMouseUpEvent
        {
            add    => _onMouseUpHandler      += value;
            remove => _onMouseUpHandler      -= value;
        }
        public event OnMouseWheelHandler OnMouseWheelEvent
        {
            add    => _onMouseWheelHandler   += value;
            remove => _onMouseWheelHandler   -= value;
        }
        public event OnMouseMoveHandler OnMouseMoveEvent
        {
            add    => _onMouseMoveHandler    += value;
            remove => _onMouseMoveHandler    -= value;
        }
        public event OnDoubleClickHandler OnDoubleClickEvent
        {
            add    => _onDoubleClickHandler  += value;
            remove => _onDoubleClickHandler  -= value;
        }
        public event EventHandler OnCameraMouseDown
        {
            add    => _onCameraMouseDownHandler   += value;
            remove => _onCameraMouseDownHandler   -= value;
        }
        public event EventHandler OnCameraDoubleClick
        {
            add    => _onCameraDoubleClickHandler += value;
            remove => _onCameraDoubleClickHandler -= value;
        }

        // ---- Methods ----
        // Fix (2026-07-20, "Start bi goi nhieu lan/RAM tang" investigation): GetCurrentVideoFrame() KHÔNG
        // còn snapshot từ _currentAvaloniaFrame (WriteableBitmap kích thước HIỂN THỊ theo control) — caller
        // (double-click focus, nút Snapshot) cần ảnh ĐÚNG ĐỘ PHÂN GIẢI CAMERA GỐC. Decode on-demand trực
        // tiếp từ decoder (_lastFrame, native res) qua GetCurrentFrameFullResWriteable() — không cần marshal
        // qua UI thread nữa (decoder tự lock nội bộ, không phụ thuộc _currentAvaloniaFrame). Trả về
        // WriteableBitmap (không còn SKBitmap) — caller Dispose() bình thường khi dùng xong.
        public WriteableBitmap? GetCurrentVideoFrame()
            => _service.GetCurrentFrameFullResWriteable();
        /// <summary>Scale frame hiện tại về (destWidth × destHeight) — forward to service decoder.</summary>
        public WriteableBitmap? GetCurrentVideoFrame(int destWidth, int destHeight)
            => _service.GetCurrentVideoFrame(destWidth, destHeight);
        public WriteableBitmap? GetCurrentVideoFrame2()
            => GetCurrentVideoFrame();
        /// <summary>Kích thước native (width/height) của frame hiện tại — không decode pixel, rẻ cho label polling.</summary>
        public (int Width, int Height)? GetCurrentFrameSize()
            => _service.GetCurrentFrameSize();
        public void SaveCurrentVideoFrame(string path)
            => _service.SaveCurrentVideoFrame(path);
        public void ClearCalibrationSamples()
            => _service.ClearCalibrationSamples();
        public (double From, double To) SuggestMotionAlarmBand(
            double minConfidence = 95.0, double binSize = 5.0, int minSamplesPerBin = 3)
            => _service.SuggestMotionAlarmBand(minConfidence, binSize, minSamplesPerBin);
        public void ClearMotionLevelSamples()
            => _service.ClearMotionLevelSamples();
        public (double HighThreshold, double LowThreshold) SuggestMotionTriggerThresholds(
            double lowPercentile = 25.0, double highPercentile = 75.0, int numBins = 64)
            => _service.SuggestMotionTriggerThresholds(lowPercentile, highPercentile, numBins);
        public void StartManualRecord()  => _service.StartManualRecord();
        public void StopManualRecord()   => _service.StopManualRecord();
        public void SaveVideo(string pathSave) => _service.SaveVideo(pathSave);
        /// <summary>
        /// Bắt đầu decode + hiển thị luồng video.<br/>
        /// ADR §6.2: <c>videoSourcePlayer.Start()</c> giữ nguyên contract — set RtspURL từ VideoSource rồi Connect.
        /// </summary>
        public void Start()
        {
            _service.RtspURL = _service.VideoSource?.Source;
            Connect();
        }

        /// <summary>
        /// Huỷ CancellationToken ngay lập tức (không đợi task hoàn thành).<br/>
        /// ADR §6.2: <c>SignalToStop()</c> → <c>_service.SignalToStop()</c> giữ nguyên.
        /// </summary>
        public void SignalToStop()
        {
            _service.SignalToStop();
        }

        /// <summary>
        /// Dừng hoàn toàn: huỷ token, đợi polling task thoát, dispose decoder (tránh use-after-free native).<br/>
        /// Sau đó giải phóng frame đang hiển thị (UI-only state, trả về AvaloniaFramePool).<br/>
        /// C2 fix (RAM leak BUG-004): _currentAvaloniaFrame (WriteableBitmap) thay _currentSKFrame.
        /// </summary>
        public void Stop()
        {
            _service.Stop();
            // Giải phóng frame đang hiển thị — UI-only state, không thuộc service.
            // WriteableBitmap (khác SKBitmap) có UI-thread affinity khi Dispose() — marshal qua Dispatcher
            // giống hành vi cũ của _avaloniaFrame, tránh gọi Dispose() native resource từ thread khác.
            var old = Interlocked.Exchange(ref _currentAvaloniaFrame, null);
            if (old is not null)
            {
                Dispatcher.UIThread.Post(() => AvaloniaFramePool.Return(old));
            }
        }

        // ============================================================
        // LIFECYCLE INTERNAL — Connect + ConsumerDisplayFrameAsync
        // ============================================================

        /// <summary>
        /// Khởi động pipeline: Stop cũ (nếu đang chạy) → StartTasks (decode + display + motion + AI + record).<br/>
        /// ADR §6.2: <c>_service.StartTasks(token => Task.Run(() => ConsumerDisplayFrameAsync(token), token))</c>
        /// giữ nguyên pattern Task.Run + Channel consumer từ WinForms.
        /// </summary>
        private void Connect()
        {
            Stop();
            _service.StartTasks(token => Task.Run(() => ConsumerDisplayFrameAsync(token), token));
        }

        /// <summary>
        /// Consumer loop hiển thị frame — chạy trên background thread (Task.Run từ Connect).<br/>
        /// Đọc frame từ <c>decoder.FrameChannel</c> → marshal UI update qua <c>Dispatcher.UIThread.Post</c>.<br/>
        /// ADR §6.2: <c>BeginInvoke(Action)</c> → <c>Dispatcher.UIThread.Post(Action)</c>.<br/>
        /// C2 fix (RAM leak BUG-004): FrameChannel giờ chứa AvaloniaFramePacket (WriteableBitmap) sẵn sàng
        /// vẽ ngay — Render() dùng thẳng, không còn bước UpdateAvaloniaFrame/copy nữa.
        /// </summary>
        private async Task ConsumerDisplayFrameAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    // Snapshot decoder reference — nếu PollingDecodeFrameAsync tạo decoder mới,
                    // ReadAllAsync sẽ hoàn thành khi channel cũ bị complete, vòng ngoài retry.
                    var decoder = _service.Decoder;
                    if (decoder == null)
                    {
                        await Task.Delay(100, token);
                        continue;
                    }
                    // Chờ frame từ FrameChannel — giải phóng CPU khi không có dữ liệu
                    await foreach (var packet in decoder.FrameChannel.Reader.ReadAllAsync(token))
                    {
                        var bmp   = packet.Bitmap;
                        var meta  = packet.Meta;

                        // Frame dropping: nếu UI thread quá bận (>= 2 frames đang chờ xử lý), bỏ qua frame này để tránh tràn bộ nhớ
                        if (Interlocked.Increment(ref _pendingUIFrames) > 2)
                        {
                            Interlocked.Decrement(ref _pendingUIFrames);
                            AvaloniaFramePool.Return(bmp);
                            continue;
                        }

                        // Marshal toàn bộ UI work lên UI thread (ADR §6.2: BeginInvoke → Dispatcher.UIThread.Post)
                        Dispatcher.UIThread.Post(() =>
                        {
                            try
                            {
                                // Fix (2026-07-20, loại bỏ SkiaSharp): Fire NewFrame (ref WriteableBitmap) —
                                // handler có thể thay bmp bằng version đã xử lý. Bản sao riêng (KHÔNG qua
                                // pool), event handler tự Dispose sau khi dùng xong.
                                var handler = _newFrameHandler;
                                if (handler != null)
                                {
                                    var cloneForEvent = AvaloniaFramePool.Clone(bmp);
                                    handler.Invoke(this, ref cloneForEvent);
                                    cloneForEvent?.Dispose();
                                }

                                // Track FrameId/CapturedAtTicks cho overlay stale-check (task 5.4).
                                Interlocked.Exchange(ref _currentFrameId,       meta.FrameId);
                                Interlocked.Exchange(ref _currentCapturedAtTicks, meta.CapturedAtTicks);

                                // Swap frame: trả frame cũ về pool, ghi frame mới vào _currentAvaloniaFrame.
                                var old = Interlocked.Exchange(ref _currentAvaloniaFrame, bmp);
                                AvaloniaFramePool.Return(old);

                                // Kích hoạt Avalonia render pipeline → Render() sẽ vẽ _currentAvaloniaFrame trực tiếp.
                                // ADR §6.2: Invalidate() → InvalidateVisual() (phải qua Dispatcher vì đến từ bg thread).
                                InvalidateVisual();
                            }
                            catch
                            {
                                // Bất kỳ exception nào trong UI callback → trả frame về pool, tránh leak
                                AvaloniaFramePool.Return(bmp);
                            }
                            finally
                            {
                                Interlocked.Decrement(ref _pendingUIFrames);
                            }
                        });
                    }
                }
                catch (OperationCanceledException)
                {
                    break; // CancellationToken triggered — thoát sạch
                }
                catch
                {
                    // Bỏ qua lỗi decode / channel error, retry sau delay ngắn
                }

                if (!token.IsCancellationRequested)
                    await Task.Delay(10, token).ConfigureAwait(false);
            }
        }
        // ============================================================
        // OVERLAY DRAWING HELPERS — task 5.4
        // ADR §6.2: GDI+ Graphics.* → Avalonia DrawingContext.*
        // ============================================================

        /// <summary>
        /// Quyết định có nên ẩn box AI/LPR không dựa theo độ trễ TickCount64 so với MaxBoxStaleMs.
        /// Ported từ WinForms gốc — static method thuần túy, không phụ thuộc GDI+/UI/state.
        /// </summary>
        internal static bool IsBoxStale(long boxFrameId, long boxCapturedAtTicks, long currentFrameId, int maxBoxStaleMs, long nowTicks)
        {
            // CapturedAtTicks == 0 → chưa gán meta (luồng legacy) → luôn hiện (backward-compat)
            if (boxCapturedAtTicks == 0) return false;
            // maxBoxStaleMs < 50 → tắt tính năng, luôn hiện box
            if (maxBoxStaleMs < 50) return false;
            // FrameId khớp chính xác → đây đúng là frame live hiện tại → không stale
            if (boxFrameId != 0 && boxFrameId == currentFrameId) return false;
            long lagMs = nowTicks - boxCapturedAtTicks;
            return lagMs > maxBoxStaleMs;
        }

        /// <summary>Lấy (hoặc tạo và cache) Pen width=2 theo màu cho vẽ viền box.</summary>
        private Pen GetBoxPen(Avalonia.Media.Color color)
        {
            if (!_boxPenCache.TryGetValue(color, out var pen))
            {
                pen = new Pen(new SolidColorBrush(color), 2.0);
                _boxPenCache[color] = pen;
            }
            return pen;
        }

        /// <summary>Lấy (hoặc tạo và cache) IBrush alpha=40 theo màu cho fill box.</summary>
        private IBrush GetBoxFill(Avalonia.Media.Color color)
        {
            if (!_boxFillCache.TryGetValue(color, out var brush))
            {
                brush = new SolidColorBrush(Avalonia.Media.Color.FromArgb(40, color.R, color.G, color.B));
                _boxFillCache[color] = brush;
            }
            return brush;
        }

        /// <summary>
        /// Scale KzRect từ ảnh gốc (imgWidth × imgHeight) sang tọa độ control (viewRect — Avalonia.Rect).
        /// ADR §6.2: StandardlizeRectangle(System.Drawing.Rectangle, System.Drawing.Rectangle, ...)
        ///           → StandardlizeRect(KzRect, Avalonia.Rect, ...) → Avalonia.Rect
        /// </summary>
        private static Rect StandardlizeRect(KzRect src, Rect viewRect, int imgWidth, int imgHeight)
        {
            if (imgWidth <= 0 || imgHeight <= 0 || viewRect.Width <= 0 || viewRect.Height <= 0)
                return default; // Avalonia.Rect không có Rect.Empty — dùng default (Width=Height=0)
            double scaleX = viewRect.Width  / imgWidth;
            double scaleY = viewRect.Height / imgHeight;
            double x = viewRect.X + src.X * scaleX;
            double y = viewRect.Y + src.Y * scaleY;
            double w = src.Width  * scaleX;
            double h = src.Height * scaleY;
            return new Rect(Math.Round(x), Math.Round(y),
                            Math.Max(0, Math.Round(w)), Math.Max(0, Math.Round(h)));
        }

        /// <summary>
        /// Vẽ label (text trên nền tối) phía trên (hoặc bên trong nếu ra ngoài bounds) viewRect.
        /// ADR §6.2: g.MeasureString + g.FillRectangle + g.DrawString
        ///           → FormattedText + ctx.FillRectangle + ctx.DrawText
        /// </summary>
        private void DrawLabel(DrawingContext ctx, string text, Rect viewRect, double boundsHeight)
        {
            var ft = GetCachedFormattedText(text, 12, _brushTextWhite);
            double labelY = viewRect.Y - ft.Height - 2;
            if (labelY < 0) labelY = viewRect.Y + 2;
            var labelRect = new Rect(viewRect.X, labelY, ft.Width + 6, ft.Height + 2);
            ctx.FillRectangle(_brushBgOverlay, labelRect);
            ctx.DrawText(ft, new Point(labelRect.X + 3, labelRect.Y + 1));
        }

        /// <summary>Vẽ lag debug overlay dưới box khi ShowFrameLagOverlay=true.</summary>
        private void DrawLagOverlay(DrawingContext ctx, Rect viewRect, long nowTicks, long capturedAtTicks, double boundsHeight)
        {
            long lagMs = nowTicks - capturedAtTicks;
            var ft = GetCachedFormattedText($"lag: {lagMs}ms", 12, _brushTextWhite);
            double lagX = viewRect.X + 2;
            double lagY = viewRect.Bottom + 1;
            if (lagY + ft.Height > boundsHeight)
                lagY = viewRect.Bottom - ft.Height - 1;
            ctx.FillRectangle(_brushBgOverlay, new Rect(lagX - 1, lagY, ft.Width + 2, ft.Height));
            ctx.DrawText(ft, new Point(lagX, lagY));
        }

        /// <summary>
        /// Vẽ danh sách region box (LPR / LOOP / FACE) với tag label.
        /// ADR §6.2: DrawRegionList(Graphics, List&lt;ObjectBox&gt;, Color, ...) → DrawRegionList(DrawingContext, ..., Avalonia.Media.Color, ...)
        /// </summary>
        private void DrawRegionList(DrawingContext ctx, List<ObjectBox> boxes,
                                    Avalonia.Media.Color color, string tag, int imgWidth, int imgHeight)
        {
            if (boxes == null || boxes.Count == 0) return;
            if (imgWidth <= 0 || imgHeight <= 0) return;
            var pen  = GetBoxPen(color);
            var fill = GetBoxFill(color);
            var bounds = new Rect(Bounds.Size); // local coords (0,0,W,H)
            foreach (var r in boxes)
            {
                if (r is null || r.BBox.Width <= 0 || r.BBox.Height <= 0) continue;
                var viewRect = StandardlizeRect(r.BBox, bounds, r.Width, r.Height);
                if (viewRect.Width <= 0 || viewRect.Height <= 0) continue;
                ctx.FillRectangle(fill, viewRect);
                ctx.DrawRectangle(null, pen, viewRect);
                DrawLabel(ctx, tag, viewRect, bounds.Height);
            }
        }

        /// <summary>
        /// Vẽ motion level text ở góc dưới trái khi VirtualLoopType != None.
        /// ADR §6.2: g.MeasureString + g.FillRectangle + g.DrawString (bottom-left) → FormattedText + ctx.*
        /// </summary>
        private void DrawMotionLevelOverlay(DrawingContext ctx, Rect bounds)
        {
            string text = $"{Math.Round(_service.CurrentMotionLevel, 1)} | " +
                          $"{_service.FromMotionAlarmLevel}->{_service.ToMotionAlarmLevel} - " +
                          $"{_service.AiDetectionDuration} ms";
            var ft = GetCachedFormattedText(text, 12, _brushTextWhite);
            double labelY = bounds.Height - ft.Height - 5;
            var labelRect = new Rect(3, labelY, ft.Width + 6, ft.Height + 2);
            ctx.FillRectangle(_brushBgOverlay, labelRect);
            ctx.DrawText(ft, new Point(labelRect.X + 3, labelRect.Y + 1));
        }

        /// <summary>
        /// Vẽ AI detect box list với stale-check và lag debug overlay tùy chọn.
        /// ADR §6.2: DrawAIList(Graphics, List&lt;AIBoxDetect&gt;, ...) → DrawAIList(DrawingContext, ...)
        /// Avalonia AIBoxDetect: không có lastUpdateStatus/count (simplified model) → dùng BoxName + classification.
        /// </summary>
        private void DrawAIList(DrawingContext ctx, List<AIBoxDetect> aiDetectBoxes, int imgWidth, int imgHeight)
        {
            if (aiDetectBoxes == null || aiDetectBoxes.Count == 0) return;
            if (imgWidth <= 0 || imgHeight <= 0) return;

            long currentFrameId = _currentFrameId; // Render() chạy trên UI thread — không cần Volatile.Read
            long nowTicks = Environment.TickCount64;
            var bounds = new Rect(Bounds.Size); // local coords (0,0,W,H)

            foreach (var r in aiDetectBoxes)
            {
                if (r is null) continue;
                if (IsBoxStale(r.FrameId, r.CapturedAtTicks, currentFrameId, MaxBoxStaleMs, nowTicks))
                    continue;

                // ADR §6.2: System.Drawing.Color.Orange/Green/Red → Avalonia.Media.Colors.*
                Avalonia.Media.Color color = Colors.Orange;
                switch (r.classification)
                {
                    case EmClassification.Close:   color = Colors.Green;  break;
                    case EmClassification.Open:    color = Colors.Red;    break;
                    case EmClassification.Unknown: color = Colors.Orange; break;
                }
                var pen  = GetBoxPen(color);
                var fill = GetBoxFill(color);

                if (r.Rect.Width <= 0 || r.Rect.Height <= 0) continue;
                // Avalonia AIBoxDetect.Rect là KzRect; AI decode ở 1920×1080 (giống WinForms gốc)
                var viewRect = StandardlizeRect(r.Rect, bounds, 1920, 1080);
                if (viewRect.Width <= 0 || viewRect.Height <= 0) continue;

                ctx.FillRectangle(fill, viewRect);
                ctx.DrawRectangle(null, pen, viewRect);

                // WinForms: "lastUpdateStatus - count"; Avalonia: "BoxName: classification" (phần simplified model)
                DrawLabel(ctx, $"{r.BoxName}: {r.classification}", viewRect, bounds.Height);

                if (ShowFrameLagOverlay && r.CapturedAtTicks > 0)
                    DrawLagOverlay(ctx, viewRect, nowTicks, r.CapturedAtTicks, bounds.Height);
            }
        }

        /// <summary>
        /// Vẽ tracking object box list (ENTRY/EXIT...) với stale-check và lag debug overlay tùy chọn.
        /// ADR §6.2: DrawObjectBoxs(Graphics, List&lt;ObjectBox&gt;, ...) → DrawObjectBoxs(DrawingContext, ...)
        /// </summary>
        private void DrawObjectBoxs(DrawingContext ctx, List<ObjectBox> boxes, int imgWidth, int imgHeight)
        {
            if (boxes == null || boxes.Count == 0) return;
            if (imgWidth <= 0 || imgHeight <= 0) return;

            long currentFrameId = _currentFrameId; // UI thread
            long nowTicks = Environment.TickCount64;
            var bounds = new Rect(Bounds.Size); // local coords (0,0,W,H)

            foreach (var r in boxes)
            {
                if (r is null) continue;
                if (IsBoxStale(r.FrameId, r.CapturedAtTicks, currentFrameId, MaxBoxStaleMs, nowTicks))
                    continue;

                Avalonia.Media.Color color = Colors.Blue;
                switch (r.Status)
                {
                    case EmObjectBoxStatus.UNKNOWN:  color = Colors.Blue;        break;
                    case EmObjectBoxStatus.ENTRY:    color = Colors.Green;       break;
                    case EmObjectBoxStatus.ENTRIED:  color = Colors.GreenYellow; break;
                    case EmObjectBoxStatus.EXIT:     color = Colors.Red;         break;
                    case EmObjectBoxStatus.EXITED:   color = Colors.IndianRed;   break;
                }
                var pen  = GetBoxPen(color);
                var fill = GetBoxFill(color);

                if (r.BBox.Width <= 0 || r.BBox.Height <= 0) continue;
                var viewRect = StandardlizeRect(r.BBox, bounds, r.Width, r.Height);
                if (viewRect.Width <= 0 || viewRect.Height <= 0) continue;

                ctx.FillRectangle(fill, viewRect);
                ctx.DrawRectangle(null, pen, viewRect);
                DrawLabel(ctx, $"{r.Id} - {r.Name}", viewRect, bounds.Height);

                if (ShowFrameLagOverlay && r.CapturedAtTicks > 0)
                    DrawLagOverlay(ctx, viewRect, nowTicks, r.CapturedAtTicks, bounds.Height);
            }
        }

        /// <summary>
        /// Vẽ tracking lines (IN/OUT boundary + direction) và counting display overlay.
        /// ADR §6.2:
        ///   GDI+ DrawLine(Pen, Point, Point)         → ctx.DrawLine(Avalonia.Pen, Avalonia.Point, Avalonia.Point)
        ///   System.Drawing.Point (int)                → Avalonia.Point (double) với integer coords từ KzPoint
        ///   _service fields (minConf, countInItems...) → backing fields (populated by DisplayItems — task 5.5)
        /// </summary>
        private void DrawTrackingLines(DrawingContext ctx, int wImg, int hImg)
        {
            // Tracking line segments — scale từ config image → current display size
            try
            {
                var cfg = _trackingConfig;
                if (cfg?.LineIN != null)
                {
                    var l = cfg.LineIN;
                    ctx.DrawLine(_penTrackLineIn,
                        new Point(l.Start.X * wImg / l.ConfigImageWidth,  l.Start.Y * hImg / l.ConfigImageHeight),
                        new Point(l.End.X   * wImg / l.ConfigImageWidth,  l.End.Y   * hImg / l.ConfigImageHeight));
                }
                if (cfg?.LineINDirection != null)
                {
                    var l = cfg.LineINDirection;
                    ctx.DrawLine(_penTrackLineInDir,
                        new Point(l.Start.X * wImg / l.ConfigImageWidth,  l.Start.Y * hImg / l.ConfigImageHeight),
                        new Point(l.End.X   * wImg / l.ConfigImageWidth,  l.End.Y   * hImg / l.ConfigImageHeight));
                }
                if (cfg?.LineOUT != null)
                {
                    var l = cfg.LineOUT;
                    ctx.DrawLine(_penTrackLineOut,
                        new Point(l.Start.X * wImg / l.ConfigImageWidth,  l.Start.Y * hImg / l.ConfigImageHeight),
                        new Point(l.End.X   * wImg / l.ConfigImageWidth,  l.End.Y   * hImg / l.ConfigImageHeight));
                }
                if (cfg?.LineOUTDirection != null)
                {
                    var l = cfg.LineOUTDirection;
                    ctx.DrawLine(_penTrackLineOutDir,
                        new Point(l.Start.X * wImg / l.ConfigImageWidth,  l.Start.Y * hImg / l.ConfigImageHeight),
                        new Point(l.End.X   * wImg / l.ConfigImageWidth,  l.End.Y   * hImg / l.ConfigImageHeight));
                }
            }
            catch { }

            // Counting stats overlay ở góc trên trái (populated by DisplayItems — task 5.5)
            try
            {
                if (_100TimesDetect.Count == 0) return;
                string text;
                try
                {
                    text = "IN: "  + System.Text.Json.JsonSerializer.Serialize(_countInItems)  +
                           "\r\nOUT: " + System.Text.Json.JsonSerializer.Serialize(_countOutItems) +
                           "\r\n"  + _coutingTime + "ms" +
                           $"\r\nAVG ({_100TimesDetect.Count}):- " + Math.Round(_100TimesDetect.Average()) + "ms" +
                           "\r\nMIN:- " + _minCoutingTime + "ms" +
                           "\r\nMAX:- " + _maxCoutingTime + "ms" +
                           "\r\nMIN Conf:- " + System.Text.Json.JsonSerializer.Serialize(_minConf);
                }
                catch (Exception ex) { text = ex.Message; }

                var ft = GetCachedFormattedText(text, 12, _brushTextWhite);
                var labelRect = new Rect(3, 3, ft.Width + 6, ft.Height + 2);
                ctx.FillRectangle(_brushBgOverlay, labelRect);
                ctx.DrawText(ft, new Point(labelRect.X + 3, labelRect.Y + 1));
            }
            catch { }
        }

        // ============================================================
        // END OVERLAY DRAWING HELPERS
        // ============================================================

        /// <summary>
        /// Cập nhật dữ liệu counting/timing cho overlay — backing fields dùng bởi DrawTrackingLines.
        /// ADR §6.2: behavior parity với WinForms AnvPlayer.DisplayItems() — cập nhật đủ 6 trường thống kê.
        /// </summary>
        public void DisplayItems(
            Dictionary<string, int> countInItems,
            Dictionary<string, int> countOutItems,
            decimal detectTime,
            Dictionary<string, decimal> confidences)
        {
            // Track min/max detectTime (parity WinForms DrawTrackingLines inline min/max logic)
            if (_minCoutingTime == 0 || _minCoutingTime > detectTime) _minCoutingTime = detectTime;
            if (_maxCoutingTime < detectTime) _maxCoutingTime = detectTime;

            _countInItems  = countInItems;
            _countOutItems = countOutItems;
            _coutingTime   = detectTime;

            _100TimesDetect.Add(detectTime);
            if (_100TimesDetect.Count > 100)
                _100TimesDetect.RemoveAt(0);

            foreach (var item in confidences)
            {
                if (_minConf.TryGetValue(item.Key, out var existing))
                    _minConf[item.Key] = Math.Min(existing, item.Value);
                else
                    _minConf[item.Key] = item.Value;
            }
        }

        /// <summary>Hiển thị mô tả camera — empty body (parity WinForms gốc: phương thức rỗng).</summary>
        public void DisplayDescription(string description) { }

        // ============================================================
        // POINTER EVENT HANDLERS — ADR §6.2
        // WinForms OnMouseDown/Up/Move/Wheel/DoubleClick → Avalonia pointer events
        // ============================================================

        private void OnAvaloniaPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            var pt    = e.GetPosition(this);
            var props = e.GetCurrentPoint(this).Properties;
            // Map PointerUpdateKind → WinForms button constant (1=Left, 2=Right, 4=Middle)
            int button = props.PointerUpdateKind switch
            {
                PointerUpdateKind.RightButtonPressed  => 2,
                PointerUpdateKind.MiddleButtonPressed => 4,
                _                                     => 1
            };
            _onMouseDownHandler?.Invoke(button, 0, (int)pt.X, (int)pt.Y);
            _onCameraMouseDownHandler?.Invoke(this, e);
        }

        private void OnAvaloniaPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            var pt = e.GetPosition(this);
            int button = e.InitialPressMouseButton switch
            {
                MouseButton.Right  => 2,
                MouseButton.Middle => 4,
                _                  => 1
            };
            _onMouseUpHandler?.Invoke(button, 0, (int)pt.X, (int)pt.Y);
        }

        private void OnAvaloniaPointerMoved(object? sender, PointerEventArgs e)
        {
            var pt = e.GetPosition(this);
            _onMouseMoveHandler?.Invoke(0, 0, (int)pt.X, (int)pt.Y);
        }

        private void OnAvaloniaPointerWheelChanged(object? sender, PointerWheelEventArgs e)
        {
            var pt = e.GetPosition(this);
            // WinForms: delta = 120 per notch; Avalonia Delta.Y = +1.0 per notch (up) / -1.0 (down)
            int delta = (int)(e.Delta.Y * 120);
            _onMouseWheelHandler?.Invoke(0, delta, (int)pt.X, (int)pt.Y);
        }

        private void OnAvaloniaDoubleTapped(object? sender, TappedEventArgs e)
        {
            var pt = e.GetPosition(this);
            _onDoubleClickHandler?.Invoke((int)pt.X, (int)pt.Y);
            _onCameraDoubleClickHandler?.Invoke(this, e);
        }
    }
}
