namespace Kztek.Object
{
    /// <summary>
    /// Cấu hình cổng thanh toán QR code qua MPOS
    /// (dùng cho thanh toán quét mã QR tại làn ra bãi đỗ xe).
    /// </summary>
    public class MPOSQrConfig
    {
        /// <summary>Địa chỉ URL server MPOS xử lý thanh toán QR.</summary>
        public string MPOSQrServer { get; set; } = string.Empty;

        /// <summary>Tên đăng nhập xác thực với MPOS QR server.</summary>
        public string MPOSQrUsername { get; set; } = string.Empty;

        /// <summary>Mật khẩu xác thực với MPOS QR server.</summary>
        public string MPOSQrPassword { get; set; } = string.Empty;

        /// <summary>Mã merchant do nhà cung cấp MPOS cấp (dùng để tạo QR thanh toán).</summary>
        public string MPOSQrMerchantId { get; set; } = string.Empty;

        /// <summary>App ID xác định ứng dụng kết nối với MPOS QR server.</summary>
        public string MPOSQrAppId { get; set; } = string.Empty;

        /// <summary>Secret key dùng để ký yêu cầu tạo QR (HMAC/OAuth).</summary>
        public string MPOSQrSecretKey { get; set; } = string.Empty;
    }
}
