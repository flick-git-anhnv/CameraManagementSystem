using System;
using System.IO;

namespace Kztek.Tool.FileHelpers
{
    /// <summary>
    /// Tiện ích thao tác file cross-platform (không phụ thuộc Windows-specific API).
    /// </summary>
    public static class FileHelper
    {
        /// <summary>
        /// Lấy kích thước file theo byte.
        /// </summary>
        /// <param name="filePath">Đường dẫn tuyệt đối đến file.</param>
        /// <returns>Kích thước file (byte), hoặc <c>-1</c> nếu file không tồn tại hoặc có lỗi.</returns>
        public static long GetFileSize(string filePath)
        {
            try
            {
                FileInfo fileInfo = new FileInfo(filePath);
                if (fileInfo.Exists)
                {
                    return fileInfo.Length;
                }
            }
            finally
            {
                GC.Collect();
            }
            return -1;
        }
    }
}

