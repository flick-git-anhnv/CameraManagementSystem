using System;
using System.Collections.Generic;
using System.Text;

namespace Kztek.Api
{
    /// <summary>
    /// Kết quả trả về sau mỗi lần gọi API qua <see cref="ApiHelper"/>.
    /// </summary>
    public class ApiResponse
    {
        /// <summary>Cho biết HTTP call có thành công (2xx) không.</summary>
        public bool IsSuccess { get; set; } = false;

        /// <summary>Thông báo lỗi từ RestSharp hoặc server (rỗng nếu thành công).</summary>
        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>Nội dung phản hồi từ server (JSON string).</summary>
        public string Response { get; set; } = string.Empty;

        /// <summary>HTTP status code nhận được (VD: 200, 401, 500); -1 nếu không có phản hồi.</summary>
        public int ApiStatusCode { get; set; }

        /// <summary>HTTP status code thứ cấp (dùng nội bộ, thường trùng với <see cref="ApiStatusCode"/>).</summary>
        public int ResponseStatusCode { get; set; }
    }
}
