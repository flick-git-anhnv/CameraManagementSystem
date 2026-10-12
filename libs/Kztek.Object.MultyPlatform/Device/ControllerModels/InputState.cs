namespace Kztek.Object.Entity.Device.ControllerModels
{
    /// <summary>
    /// Trạng thái của đầu vào (input) trên thiết bị kiểm soát (vòng từ, cảm biến, nút bấm).
    /// </summary>
    public enum InputState
    {
        /// <summary>Đầu vào ở trạng thái mở / kích hoạt (logic true).</summary>
        Open = 0,
        /// <summary>Đầu vào ở trạng thái đóng / không kích hoạt (logic false).</summary>
        Close = 1,
        /// <summary>Đầu vào mất kết nối với thiết bị.</summary>
        Disconnected = 2,
        /// <summary>Đầu vào báo lỗi.</summary>
        Error = 3
    }
}
