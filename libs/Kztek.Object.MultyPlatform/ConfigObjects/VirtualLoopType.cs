namespace Kztek.Object.MultyPlatform.ConfigObjects
{
    /// <summary>Nhóm kiểu vòng từ ảo — phát hiện xe bằng phần mềm thay cho vòng từ cứng.</summary>
    public class VirtualLoopType
    {
        /// <summary>
        /// Kiểu vòng từ ảo (virtual loop) dùng để kích hoạt làn xe mà không cần lắp vòng từ vật lý.
        /// </summary>
        public enum EmVirtualLoopType
        {
            /// <summary>Không dùng vòng từ ảo — chỉ dùng vòng từ vật lý (hoặc không dùng vòng từ).</summary>
            None,
            /// <summary>Phát hiện xe bằng cảm biến chuyển động (motion detection) trên camera.</summary>
            MotionDetection,
            /// <summary>Phát hiện xe bằng AI (nhận dạng phương tiện qua mô hình học máy).</summary>
            AI
        }

        /// <summary>Trả về chuỗi hiển thị tiếng Việt của kiểu vòng từ ảo.</summary>
        public static string GetDisplayString(EmVirtualLoopType type)
        {
            switch (type)
            {
                case EmVirtualLoopType.None:
                    return "Không sử dụng";
                case EmVirtualLoopType.MotionDetection:
                    return "Chuyển động";
                case EmVirtualLoopType.AI:
                    return "AI";
                default:
                    return "";
            }
        }
    }
}
