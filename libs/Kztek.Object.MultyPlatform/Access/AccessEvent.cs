using System;

namespace Kztek.Object
{
    /// <summary>
    /// Sự kiện kiểm soát ra vào được phát sinh từ thiết bị khi nhân viên xác thực.
    /// Lưu thông tin đủ để đối chiếu với dữ liệu nhân viên và chấm công.
    /// </summary>
    public class AccessEvent
    {
        /// <summary>ID định danh duy nhất của sự kiện.</summary>
        public string id { get; set; } = string.Empty;

        /// <summary>ID của thiết bị phát sinh sự kiện.</summary>
        public string ControllerId { get; set; } = string.Empty;

        /// <summary>Tên thiết bị phát sinh sự kiện (để hiển thị).</summary>
        public string ControllerName { get; set; } = string.Empty;

        /// <summary>ID nhân viên được nhận dạng (khớp với hệ thống nhân sự).</summary>
        public string EmployeeId { get; set; } = string.Empty;

        /// <summary>Mã định danh thực tế (số thẻ, ID khuôn mặt, ID vân tay) được đọc từ thiết bị.</summary>
        public string IdentityCode { get; set; } = string.Empty;

        /// <summary>Phương thức nhận dạng: 1 = thẻ, 2 = QR, 3 = vân tay, 4 = khuôn mặt.</summary>
        public int IdentityMethod { get; set; }

        /// <summary>Thời điểm sự kiện xảy ra trên thiết bị.</summary>
        public DateTime EventTime { get; set; }

        /// <summary>Đường dẫn ảnh chụp tại thời điểm xác thực (nếu có camera).</summary>
        public string ImagePath { get; set; } = string.Empty;

        /// <summary>
        /// <c>true</c> nếu đây là sự kiện ra (check-out); <c>false</c> nếu là sự kiện vào (check-in).
        /// </summary>
        public bool IsCheckOut { get; set; } = false;
    }
}
