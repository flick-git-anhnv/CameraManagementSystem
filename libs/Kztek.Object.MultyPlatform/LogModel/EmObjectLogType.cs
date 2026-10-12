using System;
using System.Collections.Generic;
using System.Text;

namespace Kztek.Object.Entity.Log
{
    /// <summary>Loại đối tượng phát sinh log — dùng để phân loại nguồn log trong hệ thống.</summary>
    public enum EmObjectLogType
    {
        /// <summary>Log từ hệ thống (khởi động, tắt, lỗi hệ thống).</summary>
        System,
        /// <summary>Log từ form/giao diện người dùng.</summary>
        Form,
        /// <summary>Log từ control UI cụ thể.</summary>
        Control,
        /// <summary>Log từ thiết bị kiểm soát ra vào (controller).</summary>
        Controller,
        /// <summary>Log từ lời gọi API (HTTP client/server).</summary>
        Api,
        /// <summary>Log từ camera (kết nối, chụp ảnh, nhận dạng).</summary>
        Camera,
        /// <summary>Log từ truy vấn SQL / cơ sở dữ liệu.</summary>
        SQL,
        /// <summary>Log từ bảng LED hiển thị.</summary>
        LED,
        /// <summary>Log liên quan đến khách hàng (tra cứu, xác thực).</summary>
        Customer
    }

}
