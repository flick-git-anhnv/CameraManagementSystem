using System;
using System.Collections.Generic;

namespace Kztek.Object
{
    /// <summary>
    /// Kiểu nhận dạng được hỗ trợ bởi thiết bị — có thể kết hợp nhiều kiểu bằng bitwise OR.
    /// </summary>
    [Flags]
    public enum EmIdentityType
    {
        /// <summary>Nhận dạng bằng thẻ từ.</summary>
        Card = 1,
        /// <summary>Nhận dạng bằng khuôn mặt.</summary>
        Face = 2,
        /// <summary>Nhận dạng bằng vân tay.</summary>
        Finger = 4,
        /// <summary>Nhận dạng bằng mật khẩu/PIN.</summary>
        Password = 8,
        /// <summary>Hỗ trợ tất cả kiểu nhận dạng (Card | Face | Finger | Password).</summary>
        All = 15,
    }

    /// <summary>
    /// Thông tin dung lượng lưu trữ người dùng và sự kiện của một thiết bị kiểm soát.
    /// </summary>
    public class DeviceCapacity
    {
        /// <summary>Các kiểu nhận dạng mà thiết bị hỗ trợ.</summary>
        public EmIdentityType IdentityType { get; set; }

        /// <summary>
        /// Số lượng người dùng tối đa thiết bị có thể lưu trữ, phân theo từng kiểu nhận dạng.
        /// </summary>
        public Dictionary<EmIdentityType, int> MaxIdentityCount { get; set; } = new Dictionary<EmIdentityType, int>();

        /// <summary>
        /// Số lượng người dùng hiện đang lưu trên thiết bị, phân theo từng kiểu nhận dạng.
        /// </summary>
        public Dictionary<EmIdentityType, int> CurrentUserCount { get; set; } = new Dictionary<EmIdentityType, int>();

        /// <summary>Số lượng sự kiện tối đa thiết bị có thể lưu trữ trong bộ nhớ nội.</summary>
        public int MaxEventCount { get; set; }

        /// <summary>Số lượng sự kiện hiện đang lưu trong bộ nhớ thiết bị.</summary>
        public int CurrentEventCount { get; set; }
    }
}
