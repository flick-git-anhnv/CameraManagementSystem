namespace Kztek.Object
{
    /// <summary>
    /// Hành động thực hiện trên thiết bị kiểm soát ra vào (controller).
    /// </summary>
    public enum EmControllerAction
    {
        /// <summary>Thêm mới thiết bị vào hệ thống.</summary>
        Add,
        /// <summary>Cập nhật thông tin cấu hình của thiết bị.</summary>
        UpdateInfo,
        /// <summary>Xóa thiết bị khỏi hệ thống.</summary>
        Delete,
        /// <summary>Tải dữ liệu thẻ xuống thiết bị.</summary>
        DownloadCard
    }
}
