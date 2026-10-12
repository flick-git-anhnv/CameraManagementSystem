namespace Kztek.Object.MultyPlatform.Device
{
    /// <summary>
    /// Chế độ mở barrier — xác định loại phương tiện được phép mở thanh chắn tự động.
    /// </summary>
    public enum EmBarrieOpenMode
    {
        /// <summary>Mở cho tất cả loại phương tiện.</summary>
        ALL,
        /// <summary>Chỉ mở cho ô tô (xe có 4 bánh trở lên).</summary>
        CAR_ONLY,
        /// <summary>Chỉ mở cho phương tiện không phải ô tô (xe máy, xe đạp, v.v.).</summary>
        NOT_CAR
    }

    /// <summary>Extension method cho <see cref="EmBarrieOpenMode"/>.</summary>
    public static class EmBarrieOpenModeExtension
    {
        /// <summary>
        /// Trả về chuỗi hiển thị tiếng Việt của chế độ mở barrier.
        /// </summary>
        /// <param name="value">Giá trị chế độ mở.</param>
        /// <returns>Chuỗi hiển thị (ví dụ: "Tất cả", "Ô tô", "Không phải ô tô").</returns>
        public static string ToDisplayString(this EmBarrieOpenMode value)
        {
            return value switch
            {
                EmBarrieOpenMode.ALL => "Tất cả",
                EmBarrieOpenMode.CAR_ONLY => "Ô tô",
                EmBarrieOpenMode.NOT_CAR => "Không phải ô tô",
                _ => "Unknown",
            };
        }
    }
}
