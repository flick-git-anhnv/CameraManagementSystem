namespace Kztek.Object
{
    /// <summary>
    /// Cấu hình cổng thanh toán thẻ ngân hàng qua MPOS
    /// (dùng cho thanh toán thẻ vật lý tại quầy thu phí bãi đỗ xe).
    /// </summary>
    public class MPOSCardConfig
    {
        /// <summary>Địa chỉ URL server MPOS xử lý thanh toán thẻ.</summary>
        public string MPOSCardServer { get; set; } = string.Empty;

        /// <summary>Mã merchant (đơn vị chấp nhận thanh toán) do nhà cung cấp MPOS cấp.</summary>
        public string MPOSCardMerchantId { get; set; } = string.Empty;

        /// <summary>App ID xác định ứng dụng kết nối với MPOS server.</summary>
        public string MPOSCardAppId { get; set; } = string.Empty;

        /// <summary>Secret key dùng để ký yêu cầu thanh toán (HMAC/OAuth).</summary>
        public string MPOSCardSecretKey { get; set; } = string.Empty;
    }
}
