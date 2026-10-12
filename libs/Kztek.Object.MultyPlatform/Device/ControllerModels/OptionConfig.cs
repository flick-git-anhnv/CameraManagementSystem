using System;

namespace Kztek.Object
{
    public class OptionConfig
    {
        /// <summary>
        /// Thời điểm tự động khởi động lại thiết bị<br/>
        /// Đặt giá trị bằng null nếu không muốn sử dụng chức năng này
        /// </summary>
        public DateTime? AutoRebootDeviceTime { get; set; } = null;

        /// <summary>
        /// Khoảng thời gian giữ log hệ thống với đơn vị là ngày <br/>
        /// Đặt giá trị bằng null nếu không muốn sử dụng chức năng này!
        /// </summary>
        public int? AutoClearLogDuration { get; set; } = null;
    }
}
