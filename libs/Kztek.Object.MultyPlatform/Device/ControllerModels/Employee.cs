using System;
using System.Collections.Generic;

namespace Kztek.Object
{
    /// <summary>
    /// Thông tin nhân viên được nạp xuống thiết bị kiểm soát ra vào.
    /// Lưu ý: đây là model thiết bị (Device layer), khác với <see cref="Staff"/> (model hệ thống).
    /// </summary>
    public class Employee
    {
        /// <summary>
        /// Vị trí lưu nhân viên trong bộ nhớ thiết bị (Memory ID). Mặc định 0 (tự động phân bổ).
        /// </summary>
        public int MemoryID { get; set; } = 0;

        /// <summary>Số thẻ từ của nhân viên (đã định dạng theo cấu hình reader).</summary>
        public string CardNumber { get; set; } = "";

        /// <summary>Loại thẻ — giá trị int tương ứng với <see cref="EmCardType"/>. Mặc định Mifare.</summary>
        public int CardType { get; set; } = (int)EmCardType.Mifare;

        /// <summary>ID timezone kiểm soát giờ ra vào trên thiết bị (0 = không giới hạn giờ).</summary>
        public int TimezoneID { get; set; } = 0;

        /// <summary>Danh sách dữ liệu vân tay (base64 hoặc template binary) để nạp xuống thiết bị.</summary>
        public List<string> fingerDatas { get; set; } = new List<string>();

        /// <summary>Tên hiển thị của nhân viên trên thiết bị.</summary>
        public string Name { get; set; } = "";

        private string password = string.Empty;

        /// <summary>Danh sách chỉ số cửa/làn mà nhân viên được phép ra vào.</summary>
        public List<int> Doors { get; set; } = new List<int>();

        /// <summary>
        /// Mật khẩu PIN của nhân viên trên thiết bị — tối đa 8 ký tự.
        /// Ném <see cref="ArgumentException"/> nếu mật khẩu dài hơn 8 ký tự.
        /// </summary>
        public string Password
        {
            get { return this.password; }
            set
            {
                if (value.Length > 8)
                    throw new ArgumentException("Password length must be smaller then 8 characters");
                else
                    this.password = value;
            }
        }
    }

}
