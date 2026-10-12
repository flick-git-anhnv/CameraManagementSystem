namespace Kztek.Object
{
    /// <summary>
    /// Phương thức thanh toán áp dụng cho giao dịch bãi đỗ xe.
    /// </summary>
    public enum OrderMethod
    {
        /// <summary>Thanh toán tiền mặt trực tiếp tại quầy.</summary>
        CASH,
        /// <summary>Thanh toán qua mã QR của Vimo.</summary>
        VIMO_QR_CODE,
        /// <summary>Thanh toán qua thẻ ngân hàng qua cổng Vimo.</summary>
        VIMO_CARD,
        /// <summary>Thanh toán qua hệ thống EPass (thẻ điện tử liên ngân hàng).</summary>
        EPASS,
        /// <summary>Thanh toán qua mã QR của Tingee.</summary>
        TINGEE_QR_CODE,
        /// <summary>Thanh toán QR offline (không cần kết nối server thanh toán tại thời điểm giao dịch).</summary>
        OFFLINE_QR,
        /// <summary>Thanh toán bằng voucher/phiếu giảm giá.</summary>
        Voucher
    }
}
