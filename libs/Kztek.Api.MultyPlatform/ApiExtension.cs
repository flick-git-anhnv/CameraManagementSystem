namespace Kztek.Api
{
    /// <summary>
    /// Extension method tiện ích cho URL server API.
    /// </summary>
    public static class ApiExtension
    {
        /// <summary>
        /// Đảm bảo URL server kết thúc bằng <c>/</c> (chuẩn hóa trước khi nối endpoint).
        /// </summary>
        /// <param name="serverUrl">URL server cần chuẩn hóa.</param>
        /// <returns>URL đã có dấu <c>/</c> ở cuối.</returns>
        public static string StandardlizeServerName(this string serverUrl)
        {
            if (serverUrl[^1] != '/' && serverUrl[^1] != '\\')
            {
                serverUrl += "/";
            }
            return serverUrl;
        }
    }
}
