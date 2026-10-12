using System;

namespace Kztek.Object
{
    /// <summary>Chế độ đăng ký nhận dạng nhân viên trên thiết bị.</summary>
    public class RegisterMode
    {
        /// <summary>
        /// Chế độ đăng ký nhận dạng — có thể kết hợp nhiều phương thức bằng bitwise OR.
        /// </summary>
        [Flags]
        public enum EmRegisterMode
        {
            /// <summary>Nhận dạng bằng thẻ từ.</summary>
            Card = 1,
            /// <summary>Nhận dạng bằng vân tay.</summary>
            Finger = 2,
            /// <summary>Nhận dạng bằng khuôn mặt.</summary>
            Face = 4
        }
    }
}
