namespace Kztek.Object
{
    /// <summary>
    /// Kiểu lưu trữ log của dịch vụ — xác định nơi ghi log khi hoạt động offline.
    /// </summary>
    public enum EmLogServiceType
    {
        /// <summary>Ghi log vào file văn bản trên đĩa.</summary>
        OFFLINE_FILE,
        /// <summary>Ghi log vào cơ sở dữ liệu cục bộ.</summary>
        OFFLINE_DB,
    }

    /// <summary>
    /// Thành phần hệ thống phát sinh hành động được ghi log.
    /// </summary>
    public enum EmSystemAction
    {
        /// <summary>Hành động từ ứng dụng (app-level).</summary>
        Application,
        /// <summary>Hành động liên quan đến máy chủ chính.</summary>
        MainServer,
        /// <summary>Hành động tổng hợp từ máy chủ chính.</summary>
        MainServer_Sumary,
        /// <summary>Hành động từ hệ thống bên thứ ba (third-party).</summary>
        ThirtParty,
        /// <summary>Hành động qua hàng đợi tin nhắn (message queue).</summary>
        MessageQueue,
        /// <summary>Hành động qua kết nối socket.</summary>
        SOCKET,
        /// <summary>Hành động từ thiết bị phần cứng.</summary>
        DEVICE,
        /// <summary>Hành động liên quan đến người dùng/nhân viên.</summary>
        USER
    }

    /// <summary>
    /// Mức độ của hành động được ghi log.
    /// </summary>
    public enum EmSystemActionType
    {
        /// <summary>Thông tin thông thường.</summary>
        INFO,
        /// <summary>Cảnh báo — cần chú ý nhưng chưa gây lỗi.</summary>
        WARNING,
        /// <summary>Lỗi xảy ra trong quá trình thực thi.</summary>
        ERROR,
    }

    /// <summary>
    /// Chi tiết loại hành động được ghi log trong hệ thống bãi đỗ xe.
    /// </summary>
    public enum EmSystemActionDetail
    {
        /// <summary>Lấy dữ liệu (truy vấn).</summary>
        GET,
        /// <summary>Tạo mới bản ghi.</summary>
        CREATE,
        /// <summary>Xóa bản ghi.</summary>
        DELETE,
        /// <summary>Cập nhật bản ghi.</summary>
        UPDATE,
        /// <summary>Xử lý nghiệp vụ (process).</summary>
        PROCESS,
        /// <summary>Sự kiện từ vòng từ phát hiện xe.</summary>
        LOOP_EVENT,
        /// <summary>Sự kiện quẹt thẻ vào.</summary>
        CARD_EVENT,
        /// <summary>Sự kiện xe ra.</summary>
        EXIT_EVENT,
        /// <summary>Sự kiện thẻ được rút ra khỏi đầu đọc (card dispenser).</summary>
        CARD_BE_TAKEN
    }
}
