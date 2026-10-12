namespace Kztek.Object
{
    /// <summary>
    /// Nhà cung cấp dịch vụ thanh toán điện tử tích hợp vào hệ thống bãi đỗ xe.
    /// </summary>
    public enum OrderProvider
    {
        /// <summary>Không dùng nhà cung cấp thanh toán điện tử (tiền mặt hoặc chưa cấu hình).</summary>
        None,
        /// <summary>Cổng thanh toán MoMo.</summary>
        Momo,
        /// <summary>Cổng thanh toán Tingee.</summary>
        Tingee,
        /// <summary>Cổng thanh toán Vimo.</summary>
        Vimo,
        /// <summary>Cổng thanh toán Viettel Pay.</summary>
        Viettel,
    }
}
