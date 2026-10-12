// Fix (2026-07-20, loại bỏ SkiaSharp toàn thư viện): GetCurrentVideoFrame() / (int,int) /
// GetCurrentVideoFrame2() đều WriteableBitmap? — không còn SKBitmap ở API này.
using Avalonia.Media.Imaging;
using Kztek.Cameras.Handlers;
using Kztek.Cameras.TrackingObjects;
using static Kztek.Object.MultyPlatform.ConfigObjects.VirtualLoopType;

namespace Kztek.Cameras
{
    public interface iCameraSourcePlayer
    {
        List<AIBoxDetect> AiDetectBoxes { get; set; }
        event NewFrameHandler NewFrame;
        event MotionAnalyzingEventHandler MotionAnalyzing;
        event AIDetectAnalyzingEventHandler AIDetectAnalyzing;

        // Mouse Events
        event OnMouseDownHandler OnMouseDownEvent;
        event OnMouseUpHandler OnMouseUpEvent;
        event OnMouseWheelHandler OnMouseWheelEvent;
        event OnMouseMoveHandler OnMouseMoveEvent;
        event OnDoubleClickHandler OnDoubleClickEvent;

        event EventHandler OnCameraMouseDown;
        event EventHandler OnCameraDoubleClick;

        #region Lấy hình ảnh
        WriteableBitmap? GetCurrentVideoFrame();
        WriteableBitmap? GetCurrentVideoFrame(int destWidth, int destHeight);
        WriteableBitmap? GetCurrentVideoFrame2();
        void SaveCurrentVideoFrame(string path);
        #endregion

        #region Chuyển động
        EmVirtualLoopType VirtualLoopType { get; set; }
        List<ObjectBox> MotionRegions { set; }
        bool EnableDisplayMotionRegion { set; }
        double FromMotionAlarmLevel { set; }
        double ToMotionAlarmLevel { set; }
        int MotionDetectionInterval { set; }

        bool CollectCalibrationSamples { get; set; }
        IReadOnlyList<(float MotionLevel, float Confidence)> CalibrationSamples { get; }
        void ClearCalibrationSamples();
        (double From, double To) SuggestMotionAlarmBand(double minConfidence = 95.0, double binSize = 5.0, int minSamplesPerBin = 3);

        bool CollectMotionLevelSamples { get; set; }
        IReadOnlyList<float> MotionLevelSamples { get; }
        void ClearMotionLevelSamples();
        (double HighThreshold, double LowThreshold) SuggestMotionTriggerThresholds(
            double lowPercentile = 25.0, double highPercentile = 75.0, int numBins = 64);

        bool EnableMotionEventGate { get; set; }
        int MotionTriggerMinStillSamples { get; set; }
        MotionTriggerPhase MotionPhase { get; }
        bool IsVehicleWaiting { get; }
        event EventHandler MotionTriggerDetected;
        #endregion

        #region Nhận dạng biển số
        List<ObjectBox> lprDetectRegions { get; set; }
        int DuplicatePlateSuppressSeconds { get; set; }
        #endregion

        #region Nhận dạng mặt
        List<ObjectBox> faceDetectRegions { get; set; }
        #endregion

        #region Recording
        int RecordingRate { set; }
        string RecordingResolution { set; }
        string RecordingFolder { set; }
        int RecordingVideoLength { set; }
        bool EnableRecording { get; set; }
        void StartManualRecord();
        void StopManualRecord();
        void SaveVideo(string pathSave);
        #endregion

        #region Tracking & Counting
        TrackingConfig? TrackingConfig { get; set; }
        List<ObjectBox> ObjectBoxes { get; set; }
        #endregion

        CameraStreamInfo VideoSource { get; set; }

        bool IsRunning { get; }
        void Start();
        void SignalToStop();
        void Stop();
        void DisplayItems(Dictionary<string, int> countInItems, Dictionary<string, int> countOutItems, decimal detectTime, Dictionary<string, decimal> confidences);
        void DisplayDescription(string description);
    }
}
