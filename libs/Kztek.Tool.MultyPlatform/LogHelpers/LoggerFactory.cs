using Kztek.Object;
using Kztek.Tool.LogHelpers.LogDatabase;

namespace Kztek.Tool.LogHelpers
{
    /// <summary>
    /// Factory tạo dịch vụ log phù hợp theo backend được cấu hình (file hoặc SQLite).
    /// </summary>
    public class LoggerFactory
    {
        /// <summary>
        /// Tạo instance <see cref="ILogger"/> tương ứng với loại backend log.
        /// Mặc định (và khi không nhận dạng được loại) trả về <c>LogToSQLite</c>.
        /// </summary>
        /// <param name="logServiceType">Loại backend: <c>OFFLINE_FILE</c> hoặc <c>OFFLINE_DB</c>.</param>
        /// <param name="saveLogFolder">Thư mục lưu log (file hoặc SQLite database).</param>
        /// <returns>Instance logger đã khởi tạo.</returns>
        public static ILogger CreateLoggerService(EmLogServiceType logServiceType, string saveLogFolder)
        {
            switch (logServiceType)
            {
                case EmLogServiceType.OFFLINE_FILE:
                    return new LogToFile(saveLogFolder);
                case EmLogServiceType.OFFLINE_DB:
                    return new LogToSQLite(saveLogFolder);
                default:
                    return new LogToSQLite(saveLogFolder);
            }
        }
    }
}

