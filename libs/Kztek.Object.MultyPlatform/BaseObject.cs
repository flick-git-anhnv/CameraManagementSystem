
using System;

namespace Kztek.Object
{
    /// <summary>
    /// Lớp cơ sở chung cho các đối tượng nghiệp vụ trong hệ thống KZTEK.
    /// Cung cấp các trường metadata chuẩn (audit trail, soft-delete, enabled flag).
    /// </summary>
    public class BaseObject
    {
        /// <summary>ID định danh duy nhất (thường là GUID hoặc ObjectId từ server).</summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>Tên hiển thị của đối tượng.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Mã định danh ngắn (code) — thường dùng để tra cứu nhanh.</summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>Mô tả chi tiết về đối tượng.</summary>
        public object Description { get; set; } = string.Empty;

        /// <summary>
        /// Thời điểm tạo theo UTC dạng chuỗi ISO 8601 (ví dụ: "2024-01-15T08:30:00").
        /// Dùng <see cref="CreatedTime"/> để lấy giá trị <see cref="DateTime"/> đã đổi sang giờ Việt Nam (UTC+7).
        /// </summary>
        public string CreatedUtc { get; set; } = string.Empty;

        /// <summary>
        /// Thời điểm cập nhật cuối theo UTC dạng chuỗi ISO 8601.
        /// Dùng <see cref="UpdatedTime"/> để lấy giá trị <see cref="DateTime"/> đã đổi sang giờ Việt Nam (UTC+7).
        /// </summary>
        public string UpdatedUtc { get; set; } = string.Empty;

        /// <summary>Đối tượng đang hoạt động hay bị vô hiệu hóa. Mặc định <c>true</c> (đang hoạt động).</summary>
        public bool Enabled { get; set; } = true;

        /// <summary>Đánh dấu soft-delete — <c>true</c> nghĩa là đã xóa nhưng vẫn giữ bản ghi trong DB.</summary>
        public bool Deleted { get; set; } = false;

        /// <summary>
        /// Thời điểm tạo đã chuyển sang giờ Việt Nam (UTC+7). Trả về <c>null</c> nếu <see cref="CreatedUtc"/> không hợp lệ.
        /// </summary>
        public DateTime? CreatedTime
        {
            get
            {
                try
                {
                    if (CreatedUtc.Contains("T"))
                    {
                        return DateTime.ParseExact(CreatedUtc.Substring(0, "yyyy-MM-ddTHH:mm:ss".Length), "yyyy-MM-ddTHH:mm:ss", null).AddHours(7);
                    }
                    else
                    {
                        return DateTime.Parse(CreatedUtc).AddHours(7);
                    }
                }
                catch
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Thời điểm cập nhật cuối đã chuyển sang giờ Việt Nam (UTC+7). Trả về <c>null</c> nếu <see cref="UpdatedUtc"/> không hợp lệ.
        /// </summary>
        public DateTime? UpdatedTime
        {
            get
            {
                try
                {
                    if (UpdatedUtc.Contains("T"))
                    {
                        return DateTime.ParseExact(UpdatedUtc.Substring(0, "yyyy-MM-ddTHH:mm:ss".Length), "yyyy-MM-ddTHH:mm:ss", null).AddHours(7);
                    }
                    else
                    {
                        return DateTime.Parse(UpdatedUtc).AddHours(7);
                    }
                }
                catch
                {
                    return null;
                }
            }
        }
    }
}
