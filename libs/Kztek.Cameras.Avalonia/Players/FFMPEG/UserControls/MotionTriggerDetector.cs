// G4.3: Copy nguyên xi từ Players/FFMPEG/UserControls/MotionTriggerDetector.cs (WinForms).
// Pure state machine — không có System.Drawing reference, không cần thay đổi gì.

namespace Kztek.Cameras
{
    /// <summary>Trạng thái hiện tại của pattern phát hiện xe qua barrie.</summary>
    public enum MotionTriggerPhase
    {
        /// <summary>Chưa có gì xảy ra — chờ chuyển động mạnh đầu tiên.</summary>
        Idle,
        /// <summary>Đang chuyển động mạnh (xe tiến vào vùng loop).</summary>
        Rising,
        /// <summary>Chuyển động đã hạ xuống nhẹ (xe đang đứng yên — chờ mở barrie/đọc biển).</summary>
        Still,
    }

    /// <summary>
    /// Phát hiện pattern chuyển động đặc trưng của 1 xe đi qua barrie ở chế độ
    /// <c>VirtualLoopType.MotionDetection</c>: <b>Mạnh (xe tiến vào) → Nhẹ (xe đứng yên) → Mạnh (xe đi qua)</b>.
    ///
    /// Tách riêng thành state machine thuần túy (không phụ thuộc AnvPlayerService/GDI+) để unit test
    /// độc lập bằng cách feed thẳng dãy motion level giả lập, không cần chạy camera/AI thật — giống
    /// pattern <see cref="PlateDuplicateFilter"/>/<see cref="FrameMotionDetector"/>.
    ///
    /// Cách dùng: gọi <see cref="Update(float)"/> mỗi khi có motion level mới (mỗi frame/mỗi lần
    /// ProcessFrame) — trả về <c>true</c> ĐÚNG 1 LẦN tại thời điểm pattern hoàn tất (xe đã đi qua).
    /// </summary>
    public sealed class MotionTriggerDetector
    {
        /// <summary>Motion level từ mức này trở lên được coi là "chuyển động mạnh".</summary>
        public double HighThreshold { get; set; }

        /// <summary>Motion level từ mức này trở xuống được coi là "gần như đứng yên".</summary>
        public double LowThreshold { get; set; }

        /// <summary>
        /// Số mẫu liên tiếp tối thiểu phải ở trạng thái "đứng yên" mới được công nhận là pha Still thật
        /// (chống nhiễu — 1 mẫu thấp đơn lẻ giữa lúc xe đang di chuyển không tính là đã dừng).
        /// </summary>
        public int MinStillSamples { get; set; } = 3;

        private MotionTriggerPhase _phase = MotionTriggerPhase.Idle;
        private int _stillCount;

        public MotionTriggerPhase Phase => _phase;

        /// <summary>
        /// Đưa vào 1 mẫu motion level mới. Trả về <c>true</c> đúng lúc vừa phát hiện xong pattern
        /// Mạnh→Nhẹ(đủ lâu)→Mạnh — tức xe vừa đi qua xong; sau đó tự reset về <see cref="MotionTriggerPhase.Idle"/>
        /// để sẵn sàng phát hiện lượt xe tiếp theo.
        /// </summary>
        public bool Update(float motionLevel)
        {
            switch (_phase)
            {
                case MotionTriggerPhase.Idle:
                    if (motionLevel >= HighThreshold)
                    {
                        _phase = MotionTriggerPhase.Rising;
                    }
                    break;

                case MotionTriggerPhase.Rising:
                    if (motionLevel <= LowThreshold)
                    {
                        _phase = MotionTriggerPhase.Still;
                        _stillCount = 1;
                    }
                    // Vẫn mạnh → giữ nguyên Rising (xe còn đang tiến vào).
                    break;

                case MotionTriggerPhase.Still:
                    if (motionLevel <= LowThreshold)
                    {
                        _stillCount++;
                    }
                    else if (motionLevel >= HighThreshold)
                    {
                        bool triggered = _stillCount >= MinStillSamples;
                        // Dù trigger hay chưa đủ pha Still, chuyển động mạnh trở lại luôn coi như
                        // bắt đầu 1 pha Rising mới (xe đã đi qua, hoặc pha Still trước đó chỉ là nhiễu
                        // ngắn giữa lúc xe vẫn đang di chuyển).
                        _phase = MotionTriggerPhase.Rising;
                        _stillCount = 0;
                        if (triggered)
                        {
                            return true;
                        }
                    }
                    // Ở giữa Low và High (chưa đủ thấp để tính Still, chưa đủ cao để tính Rising lại)
                    // → giữ nguyên trạng thái Still, không tăng _stillCount (không phải mẫu "đứng yên" rõ).
                    break;
            }

            return false;
        }

        /// <summary>Reset về trạng thái ban đầu — gọi khi camera reconnect hoặc muốn bắt đầu lại từ đầu.</summary>
        public void Reset()
        {
            _phase = MotionTriggerPhase.Idle;
            _stillCount = 0;
        }
    }
}
