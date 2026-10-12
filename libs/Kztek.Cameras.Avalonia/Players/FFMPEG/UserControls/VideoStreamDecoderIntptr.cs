// G4.3: Port từ Players/FFMPEG/UserControls/VideoStreamDecoderIntptr.cs (WinForms).
// Thay đổi chính:
//   - [SupportedOSPlatform("windows")] — gọi FFMPEG PInvoke trực tiếp.
//   - FramePacket.Bitmap: Bitmap → SKBitmap; MotionFrame.Bitmap: Bitmap → SKBitmap.
//   - MotionFrame.CropRect: Rectangle → KzRect.
//   - BoundedConcurrentQueue<Bitmap> → BoundedConcurrentQueue<SKBitmap>.
//   - ConvertFrameToBitmap: BitmapPool.Rent + LockBits/Scan0/Stride → SKBitmapPool.Rent + GetPixels()/RowBytes.
//   - DST_FMT: AV_PIX_FMT_BGR24 → AV_PIX_FMT_BGRA (4 bytes/pixel, khớp SKColorType.Bgra8888).
//   - VolatileBitmapWrapper.Bmp: Bitmap → SKBitmap.
//   - (Bitmap)wrapper.Bmp.Clone() → wrapper.Bmp.Copy().
//   - ImageHelper.CropBitmap(motionBmp, cropRect) → CropSKBitmap(motionBmp, cropRect) (private helper).
//   - new Rectangle(...) → new KzRect(...).
//   - MessageBox.Show → Debug.WriteLine.
//   - WriteToChannelSafely: BitmapPool.Return → SKBitmapPool.Return.
//   - GetCurrentFrame / GetFullCurrentFrame trả về SKBitmap?.
//   - Bỏ: using System.Drawing; System.Drawing.Imaging; System.Windows.Forms; Kztek.Tool.
// Namespace Kztek.Cameras (thay vì global như source WinForms) để nhất quán với library.
using ANV.Cameras.Enums;
using ANV.Cameras.Objects;
using ANV.Cameras.PINVOKE;
using Avalonia.Media.Imaging;
using Kztek.Cameras.Players.FFMPEG;
using Kztek.Cameras.Players.FFMPEG.Enums;
using Kztek.Cameras.TrackingObjects;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Channels;
using static Kztek.Object.MultyPlatform.ConfigObjects.VirtualLoopType;

namespace Kztek.Cameras
{
    /// <summary>
    /// Metadata định danh frame, được gắn tại thời điểm decode trong TryDecodeFrame.
    /// <see cref="FrameId"/> là bộ đếm đơn điệu tăng dần trong phiên kết nối — reset về 0 khi kết nối lại.
    /// <see cref="CapturedAtTicks"/> là <see cref="Environment.TickCount64"/> khi decode —
    /// đơn vị ms, đơn điệu, dùng để tính độ trễ AI vs display ở bước 2.3+.
    /// </summary>
    public readonly struct FrameMeta
    {
        public long FrameId { get; }
        public long CapturedAtTicks { get; }

        public FrameMeta(long frameId, long capturedAtTicks)
        {
            FrameId = frameId;
            CapturedAtTicks = capturedAtTicks;
        }

        public static readonly FrameMeta Empty = new FrameMeta(0, 0);
    }

    /// <summary>
    /// C2 fix (RAM leak BUG-004, 2026-07-20): gói WriteableBitmap (Avalonia) kèm FrameMeta — dùng cho
    /// FrameChannel (hiển thị) VÀ AiChannel (AI box detection). Thay cho FramePacket/SKBitmap vì đã
    /// chứng minh SKBitmap rò rỉ native memory ở tần suất decode cao; WriteableBitmap ghi thẳng qua
    /// Lock() không rò rỉ (đã test thực nghiệm bằng cách so sánh với System.Drawing.Bitmap chạy cùng
    /// pipeline). SKBitmap CHỈ còn được tạo (bản sao, on-demand, tần suất thấp) ngay trước khi gọi
    /// ReadPL.Recognize()/MolexDetection.Predict() — 2 API ngoài bắt buộc nhận SKBitmap thật.
    /// </summary>
    public sealed class AvaloniaFramePacket
    {
        public WriteableBitmap Bitmap { get; }
        public FrameMeta Meta { get; }
        public AvaloniaFramePacket(WriteableBitmap bitmap, FrameMeta meta) { Bitmap = bitmap; Meta = meta; }
    }

    /// <summary>
    /// Gói dữ liệu gửi qua MdChannel.
    /// C2 fix (RAM leak BUG-004): Bitmap đổi từ SKBitmap sang WriteableBitmap (xem AvaloniaFramePacket).
    /// </summary>
    public sealed class MotionFrame
    {
        public WriteableBitmap Bitmap { get; set; }
        /// <summary>KzRect thay Rectangle (ADR §6.3 — cross-platform).</summary>
        public KzRect CropRect { get; set; }
        public int FullWidth { get; set; }
        public int FullHeight { get; set; }
        public FrameMeta Meta { get; set; }
    }

    // [SupportedOSPlatform("windows")] đã gỡ (2026-07-14): FFMPEG PInvoke giờ resolve qua
    // FFmpegLoader (Windows .dll / Linux .so) — xem ADR §7 R9 về rủi ro ABI chưa verify.
    public class VideoStreamDecoderIntptr : IDisposable
    {
        private bool _isLocalFile = false;
        private long _playbackStartTick = 0;
        private long _streamStartPts = long.MinValue;
        private AVRational _timeBase;

        public event NewFrameHandler NewFrame;

        #region Properties
        private IntPtr fmtCtx;
        private IntPtr _pCodecContext;
        private IntPtr _pFrame;
        private IntPtr _lastFrame;
        private IntPtr _pPacket;
        private int _streamIndex;
        private IntPtr _pHwDeviceCtx;

        int width = 0;
        int height = 0;

        /// <summary>Kích thước frame gốc từ codec context (set sau Connect()). Dùng để tính aspect ratio.</summary>
        public int SourceWidth => width;
        public int SourceHeight => height;

        private string url;

        private CancellationTokenSource? _cts;

        private readonly BoundedChannelOptions _channelOptions = new(3)
        {
            FullMode = BoundedChannelFullMode.Wait
        };
        private readonly BoundedChannelOptions _aiChannelOptions = new(1) { FullMode = BoundedChannelFullMode.Wait };

        // C2 fix: FrameChannel đổi sang AvaloniaFramePacket (WriteableBitmap) — xem doc comment AvaloniaFramePacket.
        public Channel<AvaloniaFramePacket> FrameChannel { get; }
        public Channel<MotionFrame> MdChannel { get; }
        // C2 fix: AiChannel đổi sang AvaloniaFramePacket (WriteableBitmap) — xem doc comment AvaloniaFramePacket.
        public Channel<AvaloniaFramePacket> AiChannel { get; }

        private EmVirtualLoopType virtualLoopType = EmVirtualLoopType.None;
        private bool _isAiDetection = false;
        #endregion

        #region Constructor
        public VideoStreamDecoderIntptr(string url, EmVirtualLoopType virtualLoopType, bool isAiDetection)
        {
            this._isAiDetection = isAiDetection;
            this.virtualLoopType = virtualLoopType;
            this.url = url;

            FrameChannel = Channel.CreateBounded<AvaloniaFramePacket>(_channelOptions);
            MdChannel = Channel.CreateBounded<MotionFrame>(_aiChannelOptions);
            AiChannel = Channel.CreateBounded<AvaloniaFramePacket>(_aiChannelOptions);
        }
        #endregion

        #region Public Function
        public bool Connect(bool isCpu = true)
        {
            IntPtr options = IntPtr.Zero;
            try
            {
                if (this.url.Contains("rtsp"))
                {
                    _isLocalFile = false;
                    if (!FFMPEG.SetParam(5000000, "tcp", ref options))
                        return false;

                    if (!FFMPEG.OpenRTSP(url, out fmtCtx, ref options))
                        return false;
                }
                else
                {
                    _isLocalFile = true;
                    if (FFMPEG.avformat_open_input(ref fmtCtx, url, IntPtr.Zero, ref options) < 0)
                        return false;
                }

                if (FFMPEG.avformat_find_stream_info(fmtCtx, IntPtr.Zero) < 0)
                    return false;

                AVFormatContext formatContext = Marshal.PtrToStructure<AVFormatContext>(fmtCtx);
                IntPtr codecCtx = IntPtr.Zero;

                if (isCpu)
                    codecCtx = GetCodec(formatContext);
                else
                    codecCtx = GetHwAcceleratedCodec(formatContext);

                if (codecCtx == IntPtr.Zero)
                    return false;

                IntPtr streamPtr = Marshal.ReadIntPtr(formatContext.streams, _streamIndex * IntPtr.Size);
                AVStream videoStream = Marshal.PtrToStructure<AVStream>(streamPtr);
                _timeBase = videoStream.time_base;

                AVCodecContext codecContext = Marshal.PtrToStructure<AVCodecContext>(codecCtx);
                width = codecContext.width;
                height = codecContext.height;

                _pPacket = FFMPEG.av_packet_alloc();
                _pFrame = FFMPEG.av_frame_alloc();

                return true;
            }
            catch (Exception ex)
            {
                // Thay MessageBox.Show → Debug.WriteLine (cross-platform)
                Debug.WriteLine($"[VideoStreamDecoderIntptr.Connect] {ex.Message}");
                Dispose();
                return false;
            }
            finally
            {
                if (options != IntPtr.Zero)
                {
                    FFMPEG.av_dict_free(ref options);
                    options = IntPtr.Zero;
                }
            }
        }
        #endregion

        const int AVERROR_EAGAIN = -11;
        const int AVERROR_EOF = -541478725;

        private long lastGetMotionImage = 0;
        private long lastGetAiImage = 0;
        private long _frameIdCounter = 0;

        private void ThrottleLocalPlayback(long pts)
        {
            if (!_isLocalFile || pts == long.MinValue || pts < 0) return;
            if (_timeBase.den == 0) return;

            if (_streamStartPts == long.MinValue)
            {
                _streamStartPts = pts;
                _playbackStartTick = Environment.TickCount64;
                return;
            }

            double streamElapsedMs = (pts - _streamStartPts) * 1000.0 * _timeBase.num / _timeBase.den;
            double wallElapsedMs = Environment.TickCount64 - _playbackStartTick;

            int sleepMs = (int)(streamElapsedMs - wallElapsedMs);
            if (sleepMs > 1)
                Thread.Sleep(Math.Min(sleepMs, 500));
        }

        /// <summary>
        /// Đọc packet – decode – scale frame → gửi vào FrameChannel (WriteableBitmap)/MdChannel/AiChannel (SKBitmap).
        /// C2 fix (RAM leak BUG-004, 2026-07-20): FrameChannel (luồng hiển thị) đổi từ SKBitmap sang
        /// WriteableBitmap để tránh leak native của SKBitmap ở tần suất decode cao — return type đổi
        /// SKBitmap? → bool (caller PollingDecodeFrameAsync chỉ cần biết "có frame mới hay không").
        /// </summary>
        public bool TryDecodeFrame(
            CancellationToken token,
            int destWidth,
            int destHeight,
            ObjectBox? motionBox,
            EmVirtualLoopType virtualLoopType,
            int delayGetAI = 5000,
            int delayGetMotion = 1000,
            int timeoutMs = 10000)
        {
            destWidth = Math.Max(1, destWidth);
            destHeight = Math.Max(1, destHeight);

            long start = Environment.TickCount64;
            bool producedAny = false;

            while (!token.IsCancellationRequested)
            {
                if (Environment.TickCount64 - start > timeoutMs)
                    throw new TimeoutException("Mất kết nối stream quá thời gian cho phép.");

                int rd = FFMPEG.av_read_frame(fmtCtx, _pPacket);
                if (rd < 0)
                {
                    Thread.Sleep(5);
                    continue;
                }

                try
                {
                    AVPacket pkt = Marshal.PtrToStructure<AVPacket>(_pPacket);
                    if (pkt.stream_index != _streamIndex)
                        continue;

                    ThrottleLocalPlayback(pkt.pts);

                    int send = FFMPEG.avcodec_send_packet(_pCodecContext, _pPacket);
                    if (send < 0 && send != AVERROR_EAGAIN)
                        continue;

                    while (true)
                    {
                        int r = FFMPEG.avcodec_receive_frame(_pCodecContext, _pFrame);
                        if (r == 0)
                        {
                            var frameMeta = new FrameMeta(
                                Interlocked.Increment(ref _frameIdCounter),
                                Environment.TickCount64);

                            // UpdateLatestFullFrame() (SKBitmap độ phân giải gốc, chỉ phục vụ
                            // GetFullCurrentFrame/SaveCurrentVideoFrame — KHÔNG dùng ở app hiện tại) đã tắt
                            // hẳn kể từ điều tra RAM leak BUG-004 vòng 5 — không bật lại nếu chưa có nơi gọi.

                            // Fix (2026-07-20, "Start bị gọi nhiều lần/RAM tăng"): cập nhật _lastFrame —
                            // clone RẺ (av_frame_ref, KHÔNG copy pixel) mỗi frame để
                            // GetCurrentFrameFullResWriteable()/GetCurrentFrameSize() (double-click focus,
                            // nút Snapshot, label frmViewCamera) đọc được frame mới nhất an toàn cross-thread.
                            // TRƯỚC ĐÂY field này tồn tại (thiết kế cũ GetCurrentFrame/GetFullCurrentFrame)
                            // nhưng KHÔNG được gán ở đâu trong pipeline mới — luôn == IntPtr.Zero → mọi
                            // snapshot full-res thất bại + watchdog frmViewCamera false-positive dừng player
                            // sau 8s (tưởng nhầm là mất frame) → màn hình đứng hình.
                            lock (_frameLock)
                            {
                                if (_lastFrame != IntPtr.Zero)
                                {
                                    FFMPEG.av_frame_unref(_lastFrame);
                                    FFMPEG.av_frame_free(ref _lastFrame);
                                }
                                _lastFrame = CloneFrame(_pFrame);
                            }

                            // C2 fix (RAM leak BUG-004): FrameChannel dùng WriteableBitmap thay SKBitmap.
                            var avaloniaBmp = ConvertFrameToWriteableBitmap(_pFrame, destWidth, destHeight);

                            var now = Environment.TickCount64;
                            if (now - lastGetMotionImage >= delayGetMotion && virtualLoopType != EmVirtualLoopType.None)
                            {
                                AVFrame avframe = Marshal.PtrToStructure<AVFrame>(_pFrame);

                                if (motionBox != null && motionBox.BBox.Width != 0 && motionBox.BBox.Height != 0)
                                {
                                    int w = virtualLoopType == EmVirtualLoopType.AI ? 640 : 1280;
                                    int h = Math.Max(1, avframe.height * w / avframe.width);
                                    // C2 fix (RAM leak BUG-004): WriteableBitmap thay SKBitmap cho MdChannel.
                                    var motionBmp = ConvertFrameToWriteableBitmapForMotion(_pFrame, w, h);
                                    if (motionBmp != null)
                                    {
                                        var ratioWidth = (float)w / motionBox.Width;
                                        var ratioHeight = (float)h / motionBox.Height;
                                        var displayX = (int)(motionBox.BBox.X * ratioWidth);
                                        var displayY = (int)(motionBox.BBox.Y * ratioHeight);
                                        var displayWidth = (int)(motionBox.BBox.Width * ratioWidth);
                                        var displayHeight = (int)(motionBox.BBox.Height * ratioHeight);

                                        // KzRect thay Rectangle (ADR §6.3)
                                        var cropRect = new KzRect(displayX, displayY, displayWidth, displayHeight);
                                        var cutBmp = CropWriteableBitmap(motionBmp, cropRect);
                                        WriteToChannelSafely(MdChannel, new MotionFrame
                                        {
                                            Bitmap = cutBmp,
                                            CropRect = cropRect,
                                            FullWidth = w,
                                            FullHeight = h,
                                            Meta = frameMeta
                                        });
                                        // Trả motionBmp về pool (thay Dispose — nguyên gốc sai khi dùng pool)
                                        AvaloniaFramePool.Return(motionBmp);
                                    }
                                }
                                else
                                {
                                    int w = 640;
                                    int h = Math.Max(1, Marshal.PtrToStructure<AVFrame>(_pFrame).height * w / Marshal.PtrToStructure<AVFrame>(_pFrame).width);
                                    var motionBmp = ConvertFrameToWriteableBitmapForMotion(_pFrame, w, h);
                                    WriteToChannelSafely(MdChannel, new MotionFrame
                                    {
                                        Bitmap = motionBmp,
                                        CropRect = new KzRect(0, 0, w, h),
                                        FullWidth = w,
                                        FullHeight = h,
                                        Meta = frameMeta
                                    });
                                }
                                lastGetMotionImage = now;
                            }

                            var nowAi = Environment.TickCount;
                            if (nowAi - lastGetAiImage >= delayGetAI && this._isAiDetection)
                            {
                                // C2 fix (RAM leak BUG-004): WriteableBitmap thay SKBitmap cho AiChannel.
                                var aiBmp = ConvertFrameToWriteableBitmapFullResForAi(_pFrame);
                                if (aiBmp != null)
                                {
                                    WriteToChannelSafely(AiChannel, new AvaloniaFramePacket(aiBmp, frameMeta));
                                    lastGetAiImage = now;
                                }
                            }

                            if (avaloniaBmp != null)
                            {
                                WriteToChannelSafely(FrameChannel, new AvaloniaFramePacket(avaloniaBmp, frameMeta));
                                producedAny = true;
                            }

                            FFMPEG.av_frame_unref(_pFrame);
                            continue;
                        }
                        else if (r == AVERROR_EAGAIN)
                            break;
                        else if (r == AVERROR_EOF)
                            break;
                        else
                            break;
                    }
                }
                catch(Exception e)
                {

                }
                finally
                {
                    FFMPEG.av_packet_unref(_pPacket);
                }

                if (producedAny)
                    return true;

                Thread.Sleep(1);
            }

            throw new OperationCanceledException();
        }

        /// <summary>
        /// C2 fix (RAM leak BUG-004): Ghi an toàn vào FrameChannel/AiChannel (WriteableBitmap). Trả về
        /// AvaloniaFramePool nếu channel đã đóng.
        /// </summary>
        private void WriteToChannelSafely(Channel<AvaloniaFramePacket> channel, AvaloniaFramePacket packet)
        {
            if (packet?.Bitmap == null) return;

            while (!channel.Writer.TryWrite(packet))
            {
                if (channel.Reader.TryRead(out var oldest))
                {
                    AvaloniaFramePool.Return(oldest.Bitmap);
                }
                else
                {
                    AvaloniaFramePool.Return(packet.Bitmap);
                    break;
                }
            }
        }

        /// <summary>
        /// C2 fix (RAM leak BUG-004): Ghi an toàn vào MdChannel (WriteableBitmap). Trả về
        /// AvaloniaFramePool nếu channel đã đóng.
        /// </summary>
        private void WriteToChannelSafely(Channel<MotionFrame> channel, MotionFrame frame)
        {
            if (frame?.Bitmap == null) return;

            while (!channel.Writer.TryWrite(frame))
            {
                if (channel.Reader.TryRead(out var oldest))
                {
                    AvaloniaFramePool.Return(oldest.Bitmap);
                }
                else
                {
                    AvaloniaFramePool.Return(frame.Bitmap);
                    break;
                }
            }
        }

        public void Dispose()
        {
            // BUG-004 Fix 5: Hoàn thành channel writers TRƯỚC KHI dọn queue.
            // Nếu không gọi TryComplete(), consumer tasks (ConsumerMotionDetectionAsync,
            // ConsumerDisplayFrameAsync) sẽ bị kẹt vĩnh viễn trên ReadAllAsync(token)
            // của decoder CŨ sau mỗi lần decoder được thay thế trong PollingDecodeFrameAsync.
            // TryComplete() an toàn để gọi nhiều lần và khi writer đã đóng.
            FrameChannel.Writer.TryComplete();
            MdChannel.Writer.TryComplete();
            AiChannel.Writer.TryComplete();

            // C2 fix: FrameChannel giờ chứa AvaloniaFramePacket (WriteableBitmap) → trả về AvaloniaFramePool.
            while (FrameChannel.Reader.TryRead(out var packet))
                AvaloniaFramePool.Return(packet.Bitmap);

            // C2 fix: MdChannel/AiChannel giờ chứa WriteableBitmap → trả về AvaloniaFramePool.
            while (MdChannel.Reader.TryRead(out var mdPacket))
                AvaloniaFramePool.Return(mdPacket.Bitmap);

            while (AiChannel.Reader.TryRead(out var aiPacket))
                AvaloniaFramePool.Return(aiPacket.Bitmap);

            // Free last cloned frame
            lock (_frameLock)
            {
                if (_lastFrame != IntPtr.Zero)
                {
                    FFMPEG.av_frame_unref(_lastFrame);
                    FFMPEG.av_frame_free(ref _lastFrame);
                    _lastFrame = IntPtr.Zero;
                }
            }

            if (_pFrame != IntPtr.Zero)
            {
                FFMPEG.av_frame_unref(_pFrame);
                IntPtr pFrame = _pFrame;
                FFMPEG.av_frame_free(ref pFrame);
                _pFrame = IntPtr.Zero;
            }

            if (_pPacket != IntPtr.Zero)
            {
                FFMPEG.av_packet_unref(_pPacket);
                IntPtr pPacket = _pPacket;
                FFMPEG.av_packet_free(ref pPacket);
                _pPacket = IntPtr.Zero;
            }

            if (_pCodecContext != IntPtr.Zero)
            {
                FFMPEG.avcodec_flush_buffers(_pCodecContext);
                IntPtr pCodecContext = _pCodecContext;
                FFMPEG.avcodec_free_context(ref pCodecContext);
                _pCodecContext = IntPtr.Zero;
            }

            if (fmtCtx != IntPtr.Zero)
            {
                IntPtr pFormatContext = fmtCtx;
                FFMPEG.avformat_close_input(ref pFormatContext);
                fmtCtx = IntPtr.Zero;
            }

            GC.SuppressFinalize(this);
        }

        #region Private Function
        private IntPtr GetCodec(AVFormatContext formatContext)
        {
            IntPtr codecCtx = IntPtr.Zero;
            for (int i = 0; i < formatContext.nb_streams; i++)
            {
                IntPtr streamPtr = Marshal.ReadIntPtr(formatContext.streams, i * IntPtr.Size);
                if (streamPtr == IntPtr.Zero) continue;

                AVStream stream = Marshal.PtrToStructure<AVStream>(streamPtr);
                AVCodecParameters codecpar = Marshal.PtrToStructure<AVCodecParameters>(stream.codecpar);

                if (codecpar.codec_type != AVMediaType.AVMEDIA_TYPE_VIDEO)
                    continue;

                IntPtr codec = FFMPEG.avcodec_find_decoder(codecpar.codec_id);
                if (codec == IntPtr.Zero) continue;

                codecCtx = FFMPEG.avcodec_alloc_context3(codec);
                _pCodecContext = codecCtx;

                FFMPEG.avcodec_parameters_to_context(codecCtx, stream.codecpar);

                IntPtr temp = IntPtr.Zero;
                FFMPEG.avcodec_open2(codecCtx, codec, ref temp);

                this._streamIndex = i;
                break;
            }

            return codecCtx;
        }

        private IntPtr GetHwAcceleratedCodec(AVFormatContext formatContext)
        {
            for (int i = 0; i < formatContext.nb_streams; i++)
            {
                IntPtr streamPtr = Marshal.ReadIntPtr(formatContext.streams, i * IntPtr.Size);
                if (streamPtr == IntPtr.Zero) continue;

                AVStream stream = Marshal.PtrToStructure<AVStream>(streamPtr);
                AVCodecParameters codecpar = Marshal.PtrToStructure<AVCodecParameters>(stream.codecpar);
                if (codecpar.codec_type != AVMediaType.AVMEDIA_TYPE_VIDEO) continue;

                string codecName = codecpar.codec_id == AVCodecID.AV_CODEC_ID_HEVC ? "hevc_cuvid" :
                                   codecpar.codec_id == AVCodecID.AV_CODEC_ID_H264 ? "h264_cuvid" : null;

                IntPtr codec = IntPtr.Zero;
                codec = FFMPEG.avcodec_find_decoder_by_name(codecName);
                if (codec == IntPtr.Zero) continue;

                IntPtr codecCtx = FFMPEG.avcodec_alloc_context3(codec);
                _pCodecContext = codecCtx;
                FFMPEG.avcodec_parameters_to_context(codecCtx, stream.codecpar);

                if (FFMPEG.av_hwdevice_ctx_create(ref _pHwDeviceCtx, AVHWDeviceType.AV_HWDEVICE_TYPE_CUDA, null, IntPtr.Zero, 0) < 0)
                    return IntPtr.Zero;

                Marshal.WriteIntPtr(codecCtx, Marshal.OffsetOf<AVCodecContext>("hw_device_ctx").ToInt32(), _pHwDeviceCtx);

                IntPtr temp = IntPtr.Zero;
                if (FFMPEG.avcodec_open2(codecCtx, codec, ref temp) < 0) return IntPtr.Zero;

                _streamIndex = i;
                return codecCtx;
            }

            return IntPtr.Zero;
        }

        private readonly object _frameLock = new object();
        private readonly object _swsLock = new object();

        // C2 fix (RAM leak BUG-004, 2026-07-20): 3 cache sws RIÊNG cho display/motion/AI — mỗi luồng
        // decode ở kích thước khác nhau, dùng chung 1 cache sẽ khiến sws_getCachedContext rebuild liên
        // tục (cache thrash) mỗi lần dstW/dstH đổi qua lại giữa các luồng.
        private sealed class SwsCacheState
        {
            public IntPtr Ctx = IntPtr.Zero;
            public int SrcW, SrcH, DstW, DstH, SrcFmt;
            public int Flags;
        }
        private readonly SwsCacheState _swsCacheDisplay = new();
        private readonly SwsCacheState _swsCacheMotion = new();
        private readonly SwsCacheState _swsCacheAi = new();
        private readonly SwsCacheState _swsCacheSnapshot = new();

        /// <summary>
        /// C2 fix (RAM leak BUG-004, 2026-07-20): Convert AVFrame → Avalonia WriteableBitmap (Bgra8888)
        /// trực tiếp — dùng cho FrameChannel/MdChannel/AiChannel (KHÔNG còn dùng SKBitmap cho 3 luồng
        /// này nữa) vì đã chứng minh bằng thực nghiệm SKBitmap (SkiaSharp 4.150.1) rò rỉ native memory ở
        /// tần suất decode cao; WriteableBitmap ghi qua Lock()/ILockedFramebuffer.Address (cùng cơ chế
        /// con trỏ thô như GDI+ LockBits, đã test KHÔNG leak) thì ổn định. Rent từ AvaloniaFramePool,
        /// caller chịu trách nhiệm Return sau khi dùng. SKBitmap CHỈ còn tạo (bản sao, on-demand, tần
        /// suất thấp) ngay trước khi gọi ReadPL.Recognize()/MolexDetection.Predict() — 2 API ngoài bắt
        /// buộc nhận SKBitmap thật (xem AnvPlayerService.cs).
        /// </summary>
        private unsafe WriteableBitmap? ConvertFrameToWriteableBitmapCore(
            IntPtr frame,
            int dstW,
            int dstH,
            SwsCacheState cache,
            int scaleFlags = SwsFlags.SWS_BILINEAR)
        {
            lock (_swsLock)
            {
                if (frame == IntPtr.Zero) return null;
                var av = Marshal.PtrToStructure<AVFrame>(frame);

                if (av.data0 == IntPtr.Zero ||
                    av.width <= 0 || av.height <= 0 ||
                    dstW <= 0 || dstH <= 0)
                {
                    return null;
                }

                const AVPixelFormat DST_FMT = AVPixelFormat.AV_PIX_FMT_BGRA;

                if (cache.Ctx == IntPtr.Zero ||
                    cache.SrcW != av.width || cache.SrcH != av.height ||
                    cache.DstW != dstW || cache.DstH != dstH ||
                    cache.SrcFmt != (int)av.format ||
                    cache.Flags != scaleFlags)
                {
                    cache.Ctx = FFMPEG.sws_getCachedContext(
                        cache.Ctx,
                        av.width, av.height, (int)(AVPixelFormat)av.format,
                        dstW, dstH, (int)DST_FMT,
                        scaleFlags,
                        IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);

                    cache.SrcW = av.width; cache.SrcH = av.height;
                    cache.DstW = dstW; cache.DstH = dstH;
                    cache.SrcFmt = (int)av.format;
                    cache.Flags = scaleFlags;

                    if (cache.Ctx == IntPtr.Zero)
                        return null;
                }

                var bmp = AvaloniaFramePool.Rent(dstW, dstH);

                try
                {
                    using var fb = bmp.Lock();

                    IntPtr* srcData = stackalloc IntPtr[4];
                    int* srcStride = stackalloc int[4];

                    srcData[0] = av.data0;
                    srcData[1] = av.data1;
                    srcData[2] = av.data2;
                    srcData[3] = av.data3;

                    srcStride[0] = av.linesize0;
                    srcStride[1] = av.linesize1;
                    srcStride[2] = av.linesize2;
                    srcStride[3] = av.linesize3;

                    IntPtr* dstData = stackalloc IntPtr[1];
                    int* dstStride = stackalloc int[1];
                    dstData[0] = fb.Address;
                    dstStride[0] = fb.RowBytes;

                    int scaled = FFMPEG.sws_scale(
                        cache.Ctx,
                        srcData, srcStride,
                        0, av.height,
                        dstData, dstStride);

                    if (scaled <= 0)
                    {
                        AvaloniaFramePool.Return(bmp);
                        return null;
                    }

                    return bmp;
                }
                catch
                {
                    AvaloniaFramePool.Return(bmp);
                    return null;
                }
            }
        }

        private WriteableBitmap? ConvertFrameToWriteableBitmap(IntPtr frame, int dstW, int dstH)
            => ConvertFrameToWriteableBitmapCore(frame, dstW, dstH, _swsCacheDisplay, SwsFlags.SWS_BICUBIC);

        private WriteableBitmap? ConvertFrameToWriteableBitmapForMotion(IntPtr frame, int dstW, int dstH)
            => ConvertFrameToWriteableBitmapCore(frame, dstW, dstH, _swsCacheMotion);

        /// <summary>Full-res (không resize) — dùng cho AiChannel (MolexDetection cần ảnh gốc để detect chuẩn xác).</summary>
        private unsafe WriteableBitmap? ConvertFrameToWriteableBitmapFullResForAi(IntPtr frame)
        {
            if (frame == IntPtr.Zero) return null;
            var av = Marshal.PtrToStructure<AVFrame>(frame);
            if (av.width <= 0 || av.height <= 0) return null;
            return ConvertFrameToWriteableBitmapCore(frame, av.width, av.height, _swsCacheAi);
        }

        /// <summary>
        /// C2 fix (RAM leak BUG-004): Crop 1 vùng KzRect từ WriteableBitmap sang WriteableBitmap MỚI
        /// (rent từ AvaloniaFramePool) — thay CropSKBitmap cho MdChannel. Copy trực tiếp qua con trỏ thô
        /// theo từng hàng (tôn trọng RowBytes/stride của cả nguồn lẫn đích).
        /// </summary>
        internal static unsafe WriteableBitmap? CropWriteableBitmap(WriteableBitmap src, KzRect rect)
        {
            if (src == null || rect.Width <= 0 || rect.Height <= 0) return null;
            int x = Math.Max(0, rect.X);
            int y = Math.Max(0, rect.Y);
            int w = Math.Min(rect.Width, src.PixelSize.Width - x);
            int h = Math.Min(rect.Height, src.PixelSize.Height - y);
            if (w <= 0 || h <= 0) return null;

            var dest = AvaloniaFramePool.Rent(w, h);
            const int bytesPerPixel = 4; // Bgra8888

            using var srcFb = src.Lock();
            using var dstFb = dest.Lock();
            byte* srcBase = (byte*)srcFb.Address.ToPointer();
            byte* dstBase = (byte*)dstFb.Address.ToPointer();
            for (int row = 0; row < h; row++)
            {
                byte* srcRow = srcBase + (long)(y + row) * srcFb.RowBytes + (long)x * bytesPerPixel;
                byte* dstRow = dstBase + (long)row * dstFb.RowBytes;
                Buffer.MemoryCopy(srcRow, dstRow, dstFb.RowBytes, (long)w * bytesPerPixel);
            }
            return dest;
        }


        /// <summary>
        /// Fix (2026-07-20, loại bỏ SkiaSharp): trả WriteableBitmap (thay SKBitmap) — lấy frame tại
        /// kích thước tùy chỉnh, decode on-demand từ _lastFrame.
        /// </summary>
        public WriteableBitmap? GetCurrentFrameResizedWriteable(int destWidth, int destHeight)
        {
            lock (_frameLock)
            {
                if (_lastFrame == IntPtr.Zero || destWidth <= 0 || destHeight <= 0)
                    return null;

                try
                {
                    return ConvertFrameToWriteableBitmapCore(_lastFrame, destWidth, destHeight, _swsCacheSnapshot);
                }
                catch
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Snapshot frame hiện tại tại ĐỘ PHÂN GIẢI GỐC (native, không resize theo control) dưới dạng
        /// WriteableBitmap — dùng cho capture/snapshot (double-click focus, nút Snapshot). KHÔNG dùng
        /// SKBitmap: trước đây Camera.GetCurrentVideoFrame() lấy từ _currentAvaloniaFrame (kích thước
        /// hiển thị theo control, đã resize) — sai với yêu cầu "ảnh chụp phải đúng độ phân giải camera
        /// gốc". Giờ decode on-demand trực tiếp từ _lastFrame — chỉ tốn CPU khi caller thực sự gọi
        /// (double-click/nút chụp), không tốn thêm mỗi frame liên tục như UpdateLatestFullFrame() cũ
        /// (đã tắt hẳn từ BUG-004 vòng 5 vì gây leak khi chạy continuous).
        /// </summary>
        public WriteableBitmap? GetCurrentFrameFullResWriteable()
        {
            lock (_frameLock)
            {
                if (_lastFrame == IntPtr.Zero) return null;
                var av = Marshal.PtrToStructure<AVFrame>(_lastFrame);
                if (av.width <= 0 || av.height <= 0) return null;

                try
                {
                    return ConvertFrameToWriteableBitmapCore(_lastFrame, av.width, av.height, _swsCacheSnapshot);
                }
                catch
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Đọc nhanh kích thước native (width/height) của frame hiện tại — KHÔNG decode/convert pixel
        /// (không sws_scale, không rent buffer). Dùng cho hiển thị label thông tin (VD: frmViewCamera
        /// lblVideoSize) ở tần suất cao (300ms) mà không phải trả giá một lần full-res convert mỗi tick.
        /// </summary>
        public (int Width, int Height)? GetCurrentFrameSize()
        {
            lock (_frameLock)
            {
                if (_lastFrame == IntPtr.Zero) return null;
                var av = Marshal.PtrToStructure<AVFrame>(_lastFrame);
                if (av.width <= 0 || av.height <= 0) return null;
                return (av.width, av.height);
            }
        }

        private IntPtr CloneFrame(IntPtr srcFrame)
        {
            if (srcFrame == IntPtr.Zero) return IntPtr.Zero;

            IntPtr newFrame = FFMPEG.av_frame_alloc();
            if (newFrame == IntPtr.Zero) return IntPtr.Zero;

            int ret = FFMPEG.av_frame_ref(newFrame, srcFrame);
            if (ret < 0)
            {
                FFMPEG.av_frame_free(ref newFrame);
                return IntPtr.Zero;
            }

            return newFrame;
        }

        #endregion
    }
}
