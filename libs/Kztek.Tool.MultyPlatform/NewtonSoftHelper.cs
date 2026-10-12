using Kztek.Object;
using Kztek.Object.Entity.Log;
using Kztek.Tool.LogHelpers.LogDatabase;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace Kztek.Tool
{
    /// <summary>
    /// Tiện ích generic serialize/deserialize JSON qua Newtonsoft.Json, tích hợp ghi log lỗi tự động.
    /// </summary>
    /// <typeparam name="T">Kiểu đối tượng cần serialize/deserialize.</typeparam>
    public class NewtonSoftHelper<T> where T : class
    {
        /// <summary>
        /// Deserialize chuỗi JSON thành đối tượng <typeparamref name="T"/>.
        /// Ghi log lỗi khi JSON null/rỗng hoặc parse thất bại; trả về <c>null</c> thay vì ném exception.
        /// </summary>
        /// <param name="jsonData">Chuỗi JSON cần parse.</param>
        /// <param name="logType">Mức log khi parse thất bại.</param>
        /// <param name="doi_tuong_tac_dong">Loại đối tượng tác động (dùng cho phân loại log).</param>
        /// <returns>Đối tượng đã parse, hoặc <c>null</c> nếu thất bại.</returns>
        public static T? GetBaseResponse(string jsonData,
                                        EmLogType logType = EmLogType.ERROR,
                                        EmObjectLogType doi_tuong_tac_dong = EmObjectLogType.Api,
                                        [CallerMemberName] string callerName = "",
                                        [CallerLineNumber] int lineNumber = 0,
                                        [CallerFilePath] string filePath = "")
        {
            try
            {
                T response = JsonConvert.DeserializeObject<T>(jsonData, new JsonSerializerSettings()
                {
                    DateTimeZoneHandling = DateTimeZoneHandling.Utc,
                })!;
                if (response == null)
                {
                    TblSystemLog1.SaveLog(EmSystemAction.Application, EmSystemActionDetail.GET,
                                         $"{typeof(T).Name} - {jsonData}", null, callerName, lineNumber, filePath);
                }
                return response;
            }
            catch (Exception ex)
            {
                TblSystemLog1.SaveLog(EmSystemAction.Application, EmSystemActionDetail.GET,
                                     $"{typeof(T).Name} - {jsonData}", ex, callerName, lineNumber, filePath);
                return null;
            }

        }

        /// <summary>
        /// Đọc file JSON tại <paramref name="path"/> và deserialize thành <typeparamref name="T"/>.
        /// Trả về <c>null</c> nếu file không tồn tại, rỗng, hoặc parse lỗi.
        /// </summary>
        /// <param name="path">Đường dẫn tuyệt đối đến file JSON.</param>
        public static T? DeserializeObjectFromPath(string path)
        {
            if (!File.Exists(path)) return null;
            string configContent = File.ReadAllText(path, encoding: System.Text.Encoding.UTF8);
            if (string.IsNullOrEmpty(configContent)) return null;
            try
            {
                return JsonConvert.DeserializeObject<T>(configContent);
            }
            catch (Exception ex)
            {
                TblSystemLog1.SaveLog(EmSystemAction.Application, EmSystemActionDetail.GET,
                                     $"{typeof(T).Name} - Deseiralize", ex);
                return null;
            }
        }

        /// <summary>
        /// Serialize <paramref name="obj"/> thành JSON và lưu vào file tại <paramref name="savePath"/> (UTF-8, Indented).
        /// Tự động tạo thư mục và file nếu chưa tồn tại.
        /// </summary>
        /// <param name="obj">Đối tượng cần lưu; trả về <c>false</c> ngay nếu null.</param>
        /// <param name="savePath">Đường dẫn file đích.</param>
        /// <returns><c>true</c> nếu lưu thành công.</returns>
        public static bool SaveConfig(T? obj, string savePath)
        {
            if (obj == null)
            {
                return false;
            }
            string directoryName = Path.GetDirectoryName(savePath);
            if (!Directory.Exists(directoryName))
            {
                Directory.CreateDirectory(directoryName);
            }

            if (!File.Exists(savePath))
            {
                using (File.Create(savePath)) { }
            }

            using (StreamWriter writer = new StreamWriter(savePath, false, encoding: System.Text.Encoding.UTF8))
            {
                try
                {
                    string value = JsonConvert.SerializeObject(obj, Formatting.Indented);
                    writer.WriteLine(value);
                    writer.Dispose();
                }
                catch
                {
                    return false;
                }
            }
            return true;
        }
    }
}

