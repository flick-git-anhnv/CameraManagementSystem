using Kztek.Tool;
using Newtonsoft.Json.Linq;
using RestSharp;
using RestSharp.Authenticators;
using System.Runtime.CompilerServices;

namespace Kztek.Api
{
    /// <summary>
    /// Client HTTP đa năng sử dụng RestSharp, tích hợp ghi log API và redact header nhạy cảm.
    /// Mặc định retry tối đa <see cref="max_send_times"/> lần trước khi trả về lỗi.
    /// </summary>
    public class ApiHelper
    {
        /// <summary>Thư mục khởi động ứng dụng (dùng để resolve đường dẫn tương đối).</summary>
        public static string startupPath = "";

        /// <summary>Timeout mặc định cho mỗi lần gọi API (millisecond). Mặc định 10 000 ms.</summary>
        public static int timeOut = 10000;

        /// <summary>Tên header chứa token xác thực (mặc định "token").</summary>
        public static string tokenHeader = "token";

        /// <summary>Số lần retry tối đa khi gọi API thất bại.</summary>
        public const int max_send_times = 2;

        /// <summary>
        /// Gọi API với xác thực HTTP Basic (username/password), hỗ trợ headers và query params tùy chỉnh.
        /// </summary>
        /// <param name="apiUrl">URL endpoint đầy đủ.</param>
        /// <param name="username">Tên đăng nhập Basic Auth.</param>
        /// <param name="password">Mật khẩu Basic Auth.</param>
        /// <param name="data">Body object (serialize thành JSON).</param>
        /// <param name="headerValues">Dictionary header bổ sung.</param>
        /// <param name="requiredParams">Query parameters bổ sung.</param>
        /// <param name="timeOut">Timeout millisecond.</param>
        /// <param name="method">HTTP method (GET, POST, PUT, ...).</param>
        /// <returns>Phản hồi thô từ RestSharp.</returns>
        // Fix: async method now correctly awaits ExecuteAsync; added null-checks for
        // headerValues and requiredParams; wrapped client in using.
        public static async Task<RestResponse> GeneralBasicAuthJsonAPI(string apiUrl, string username, string password, object data,
                                                                       Dictionary<string, string> headerValues, Dictionary<string, string> requiredParams,
                                                                       int timeOut, Method method,
                                                                       [CallerMemberName] string callerName = "", [CallerLineNumber] int lineNumber = 0, [CallerFilePath] string filePath = "")
        {
            using var client = new RestClient(apiUrl);
            var request = new RestRequest()
            {
                RequestFormat = DataFormat.Json,
                Method = method,
                Timeout = TimeSpan.FromMilliseconds(timeOut),
                Authenticator = new HttpBasicAuthenticator(username, password),
            };
            if (data != null)
                request.AddJsonBody(data);

            if (headerValues != null)
                request.AddHeaders(headerValues);

            if (requiredParams != null)
            {
                foreach (KeyValuePair<string, string> kvp in requiredParams)
                {
                    request.AddQueryParameter(kvp.Key, kvp.Value);
                }
            }

            var response = await client.ExecuteAsync(request);
            return response;
        }

        /// <summary>
        /// Gọi API JSON đồng bộ (không có form-data). Retry tối đa <see cref="max_send_times"/> lần.
        /// Không ghi log — dùng <see cref="GeneralJsonAPIAsync(string,object?,Dictionary{string,string}?,Dictionary{string,string}?,int,Method,string,int,string)"/> nếu cần log.
        /// </summary>
        /// <param name="apiUrl">URL endpoint.</param>
        /// <param name="data">Body object (serialize thành JSON).</param>
        /// <param name="headerValues">Headers bổ sung (nullable).</param>
        /// <param name="requiredParams">Query params bổ sung (nullable).</param>
        /// <param name="timeOutInMilisecond">Timeout millisecond.</param>
        /// <param name="method">HTTP method.</param>
        /// <returns><see cref="ApiResponse"/> chứa trạng thái, status code và nội dung phản hồi.</returns>
        public static ApiResponse GeneralJsonAPI(string apiUrl, object data, Dictionary<string, string>? headerValues,
                                            Dictionary<string, string>? requiredParams, int timeOutInMilisecond, Method method,
                                            [CallerMemberName] string callerName = "", [CallerLineNumber] int lineNumber = 0, [CallerFilePath] string filePath = "")
        {
            for (int i = 0; i < max_send_times; i++)
            {
                using var client = new RestClient(apiUrl);
                var request = new RestRequest
                {
                    Method = method,
                    Timeout = TimeSpan.FromMilliseconds(timeOutInMilisecond),
                    RequestFormat = DataFormat.Json
                };
                if (data != null)
                    request.AddJsonBody(data);

                if (headerValues != null)
                {
                    foreach (KeyValuePair<string, string> item in headerValues)
                    {
                        request.AddHeader(item.Key, item.Value);
                    }
                }

                if (requiredParams != null)
                {
                    foreach (KeyValuePair<string, string> kvp in requiredParams)
                    {
                        request.AddQueryParameter(kvp.Key, kvp.Value);
                    }
                }

                var response = client.Execute(request);
                if (!response.IsSuccessful)
                {
                    if (i == max_send_times - 1)
                    {
                        return new ApiResponse()
                        {
                            IsSuccess = false,
                            ApiStatusCode = (int)response.StatusCode,
                            ErrorMessage = response.ErrorMessage ?? "",
                            Response = response.Content ?? ""
                        };
                    }
                    continue;
                }

                return new ApiResponse()
                {
                    IsSuccess = true,
                    ApiStatusCode = (int)response.StatusCode,
                    ErrorMessage = response.ErrorMessage ?? "",
                    Response = response.Content ?? ""
                };
            }

            return new ApiResponse()
            {
                IsSuccess = false,
                ApiStatusCode = -1,
                ErrorMessage = "",
                Response = ""
            };

        }

        // Fix: added using for RestClient.
        public static ApiResponse GeneralStringJsonAPI(string apiUrl, string data, Dictionary<string, string>? headerValues,
                                            Dictionary<string, string> requiredParams, int timeOut, Method method, bool isSaveLog = true,
                                            [CallerMemberName] string callerName = "", [CallerLineNumber] int lineNumber = 0, [CallerFilePath] string filePath = "")
        {

            for (int i = 0; i < max_send_times; i++)
            {
                using var client = new RestClient(apiUrl);
                var request = new RestRequest
                {
                    Timeout = TimeSpan.FromMilliseconds(timeOut),
                    Method = method
                };
                if (data != null)
                    request.AddParameter("application/json", data, ParameterType.RequestBody);

                if (headerValues != null)
                    request.AddHeaders(headerValues);

                if (requiredParams != null)
                {
                    foreach (KeyValuePair<string, string> kvp in requiredParams)
                    {
                        request.AddQueryParameter(kvp.Key, kvp.Value);
                    }
                }

                var response = client.Execute(request);
                if (!response.IsSuccessful)
                {
                    if (i == max_send_times - 1)
                    {
                        return new ApiResponse()
                        {
                            IsSuccess = false,
                            ApiStatusCode = (int)response.StatusCode,
                            ErrorMessage = response.ErrorMessage ?? "",
                            Response = response.Content ?? ""
                        };
                    }
                    continue;
                }

                return new ApiResponse()
                {
                    IsSuccess = true,
                    ApiStatusCode = (int)response.StatusCode,
                    ErrorMessage = response.ErrorMessage ?? "",
                    Response = response.Content ?? ""
                };
            }
            return new ApiResponse()
            {
                IsSuccess = false,
                ApiStatusCode = -1,
                ErrorMessage = "",
                Response = ""
            };
        }

        /// <summary>
        /// Xóa dữ liệu ảnh Base64 khỏi JSON trước khi ghi log (tránh log quá lớn).
        /// Xóa field <c>images</c> ở root và trong <c>entry.images</c> nếu có.
        /// Trả về JSON gốc nếu parse lỗi.
        /// </summary>
        /// <param name="json">JSON string cần làm sạch.</param>
        /// <returns>JSON đã xóa ảnh Base64.</returns>
        public static string RemoveImageBase64(string json)
        {
            try
            {
                if (string.IsNullOrEmpty(json))
                {
                    return string.Empty;
                }
                var jObj = JObject.Parse(json);

                // Xóa toàn bộ mảng images
                if (jObj["images"] != null)
                {
                    jObj["images"] = new JArray(); // hoặc jObj.Remove("images");
                }

                // Nếu trong entry có nested images
                if (jObj["entry"] != null && jObj["entry"]["images"] != null)
                {
                    jObj["entry"]["images"] = new JArray();
                }
                return Newtonsoft.Json.JsonConvert.SerializeObject(jObj);
            }
            catch
            {
                return json; // Trả lại nguyên gốc nếu có lỗi khi parse
            }
        }

        public static async Task<ApiResponse> GeneralJsonAPIAsync(string apiUrl, object? data,
                                                                  Dictionary<string, string>? headerValues,
                                                                  Dictionary<string, string>? requiredParams,
                                                                  int timeOutInMilisecond, Method method,
                                                                  [CallerMemberName] string callerName = "", [CallerLineNumber] int lineNumber = 0, [CallerFilePath] string filePath = "")
        {
            string apiId = Guid.NewGuid().ToString();
            string errorMessage = string.Empty;

            for (int i = 0; i < max_send_times; i++)
            {
                DateTime startTime = DateTime.Now;
                ApiLogDetail apiLogDetail = new ApiLogDetail()
                {
                    EndPoint = apiUrl,
                    ApiLogId = apiId,
                    CreatedDate = startTime,
                    ApiMethod = method.ToString(),
                    Headers = RedactSensitiveHeaders(headerValues),
                    Params = requiredParams,
                    Body = data,
                    Description = $"START {i + 1}",
                    Action = Object.EmSystemAction.MainServer,
                    ActionType = Object.EmSystemActionType.INFO,
                };
                SystemUtils.logger.SaveAPILogDetail(apiLogDetail, callerName, lineNumber, filePath);

                using var client = new RestClient(apiUrl);
                var request = new RestRequest
                {
                    Method = method,
                    RequestFormat = DataFormat.Json
                };
                if (timeOutInMilisecond > 0)
                {
                    request.Timeout = TimeSpan.FromMilliseconds(timeOutInMilisecond);
                }

                if (data != null)
                {
                    request.AddJsonBody(data);
                }

                if (headerValues != null)
                {
                    foreach (KeyValuePair<string, string> item in headerValues)
                    {
                        request.AddHeader(item.Key, item.Value);
                    }
                }

                if (requiredParams != null)
                {
                    foreach (KeyValuePair<string, string> kvp in requiredParams)
                    {
                        request.AddQueryParameter(kvp.Key, kvp.Value);
                    }
                }

                var response = await client.ExecuteAsync(request);

                DateTime endTime = DateTime.Now;

                ApiLogDetail apiLogDetail2 = new ApiLogDetail()
                {
                    EndPoint = apiUrl,
                    ApiLogId = apiId,
                    CreatedDate = endTime,
                    ApiMethod = method.ToString(),
                    Headers = RedactSensitiveHeaders(headerValues),
                    Params = requiredParams,
                    Body = data,
                    ResponseStatus = (int)response.StatusCode,
                    ResponseContent = response.Content,
                    Ex = response.ErrorException,
                    Description = $"End {i + 1}",
                    Action = Object.EmSystemAction.MainServer,
                    ActionType = Object.EmSystemActionType.INFO,
                };
                SystemUtils.logger.SaveAPILogDetail(apiLogDetail2, callerName, lineNumber, filePath);

                ApiLog apiLog = new ApiLog()
                {
                    Id = apiId,
                    EndPoint = apiUrl,
                    StartTime = startTime,
                    EndTime = endTime,
                    ApiMethod = method.ToString(),
                    Headers = RedactSensitiveHeaders(headerValues),
                    Params = requiredParams,
                    Body = data,
                    ResponseStatus = (int)response.StatusCode,
                    ResponseContent = response.Content,
                    Ex = response.ErrorException,
                    // TODO(typo): MainServer_Sumary should be MainServer_Summary — defined in
                    // Kztek.Tool (external project, source not available in this workspace).
                    // Fix requires updating the enum member in Kztek.Tool and all callers.
                    Action = Object.EmSystemAction.MainServer_Sumary,
                    ActionType = Object.EmSystemActionType.INFO,
                };
                SystemUtils.logger.SaveAPILog(apiLog, callerName, lineNumber, filePath);

                if (response.IsSuccessful)
                {
                    return new ApiResponse()
                    {
                        IsSuccess = true,
                        ApiStatusCode = (int)response.StatusCode,
                        ErrorMessage = response.ErrorMessage ?? "",
                        Response = response.Content ?? ""
                    };
                }
                var logResponse = response;
                //Không nhận được phản hồi từ server (Time out, ...)
                if (string.IsNullOrEmpty(response.Content))
                {
                    if (i == max_send_times - 1)
                    {
                        return new ApiResponse()
                        {
                            IsSuccess = false,
                            ApiStatusCode = (int)response.StatusCode,
                            ErrorMessage = response.ErrorMessage ?? "",
                            Response = ""
                        };
                    }
                    errorMessage = "Empty Response";
                    continue;
                }
                //Nhận được phản hồi từ server
                return new ApiResponse()
                {
                    IsSuccess = false,
                    ApiStatusCode = (int)response.StatusCode,
                    ErrorMessage = response.ErrorMessage ?? "",
                    Response = response.Content
                };

            }
            return new ApiResponse()
            {
                IsSuccess = false,
                ApiStatusCode = -1,
                ErrorMessage = "",
                Response = ""
            };
        }

        // Fix: added using for RestClient; removed empty if(InternalServerError){} block;
        // replaced MIME hardcode with GetFileTypeFromBytes helper;
        // redact sensitive headers before logging.
        public static async Task<ApiResponse> GeneralJsonAPIAsync(string apiUrl, object? data,
                                                                     Dictionary<string, string>? headerValues,
                                                                     Dictionary<string, string>? requiredParams,
                                                                     Dictionary<string, object>? formData,
                                                                     int timeOutInMilisecond, Method method, bool isSaveLog = true,
                                                                     [CallerMemberName] string callerName = "", [CallerLineNumber] int lineNumber = 0, [CallerFilePath] string filePath = "")
        {
            string apiId = Guid.NewGuid().ToString();
            string errorMessage = string.Empty;

            for (int i = 0; i < max_send_times; i++)
            {
                DateTime startTime = DateTime.Now;
                if (isSaveLog)
                {
                    ApiLogDetail apiLogDetail = new ApiLogDetail()
                    {
                        EndPoint = apiUrl,
                        ApiLogId = apiId,
                        CreatedDate = startTime,
                        ApiMethod = method.ToString(),
                        Headers = RedactSensitiveHeaders(headerValues),
                        Params = requiredParams,
                        Body = formData,
                        Description = $"START {i + 1}",
                        Action = Object.EmSystemAction.MainServer,
                        ActionType = Object.EmSystemActionType.INFO,
                    };
                    SystemUtils.logger.SaveAPILogDetail(apiLogDetail, callerName, lineNumber, filePath);
                }

                using var client = new RestClient(apiUrl);
                var request = new RestRequest
                {
                    Method = method,
                    RequestFormat = DataFormat.Json
                };
                if (timeOutInMilisecond > 0)
                {
                    request.Timeout = TimeSpan.FromMilliseconds(timeOutInMilisecond);
                }

                if (data != null)
                {
                    request.AddJsonBody(data);
                }


                if (formData != null)
                {
                    foreach (var kv in formData)
                    {
                        if (kv.Value is byte[] fileBytes)
                        {
                            var (ext, mime) = GetFileTypeFromBytes(fileBytes);
                            request.AddFile(kv.Key, fileBytes, $"{kv.Key}{ext}", mime);
                        }
                        else
                        {
                            request.AddParameter(kv.Key, kv.Value?.ToString());
                        }
                    }
                }

                if (headerValues != null)
                {
                    foreach (KeyValuePair<string, string> item in headerValues)
                    {
                        request.AddHeader(item.Key, item.Value);
                    }
                }

                if (requiredParams != null)
                {
                    foreach (KeyValuePair<string, string> kvp in requiredParams)
                    {
                        request.AddQueryParameter(kvp.Key, kvp.Value);
                    }
                }

                var response = await client.ExecuteAsync(request);

                if (isSaveLog)
                {
                    DateTime endTime = DateTime.Now;

                    ApiLogDetail apiLogDetail = new ApiLogDetail()
                    {
                        EndPoint = apiUrl,
                        ApiLogId = apiId,
                        CreatedDate = endTime,
                        ApiMethod = method.ToString(),
                        Headers = RedactSensitiveHeaders(headerValues),
                        Params = requiredParams,
                        Body = data,
                        ResponseStatus = (int)response.StatusCode,
                        ResponseContent = response.Content,
                        Ex = response.ErrorException,
                        Description = $"End {i + 1}",
                        Action = Object.EmSystemAction.MainServer,
                        ActionType = Object.EmSystemActionType.INFO,
                    };
                    SystemUtils.logger.SaveAPILogDetail(apiLogDetail, callerName, lineNumber, filePath);

                    ApiLog apiLog = new ApiLog()
                    {
                        Id = apiId,
                        EndPoint = apiUrl,
                        StartTime = startTime,
                        EndTime = endTime,
                        ApiMethod = method.ToString(),
                        Headers = RedactSensitiveHeaders(headerValues),
                        Params = requiredParams,
                        Body = data,
                        ResponseStatus = (int)response.StatusCode,
                        ResponseContent = response.Content,
                        Ex = response.ErrorException,
                        // TODO(typo): MainServer_Sumary — see note in first overload above.
                        Action = Object.EmSystemAction.MainServer_Sumary,
                        ActionType = Object.EmSystemActionType.INFO,
                    };
                    SystemUtils.logger.SaveAPILog(apiLog, callerName, lineNumber, filePath);
                }

                if (response.IsSuccessful)
                {
                    return new ApiResponse()
                    {
                        IsSuccess = true,
                        ApiStatusCode = (int)response.StatusCode,
                        ErrorMessage = response.ErrorMessage ?? "",
                        Response = response.Content ?? ""
                    };
                }
                //Không nhận được phản hồi từ server (Time out, ...)
                if (string.IsNullOrEmpty(response.Content))
                {
                    if (i == max_send_times - 1)
                    {
                        return new ApiResponse()
                        {
                            IsSuccess = false,
                            ApiStatusCode = (int)response.StatusCode,
                            ErrorMessage = response.ErrorMessage ?? "",
                            Response = ""
                        };
                    }
                    errorMessage = "Empty Response";
                    continue;
                }
                //Nhận được phản hồi từ server
                return new ApiResponse()
                {
                    IsSuccess = false,
                    ApiStatusCode = (int)response.StatusCode,
                    ErrorMessage = response.ErrorMessage ?? "",
                    Response = response.Content
                };

            }
            return new ApiResponse()
            {
                IsSuccess = false,
                ApiStatusCode = -1,
                ErrorMessage = "",
                Response = ""
            };
        }

        // Fix: added using for RestClient; removed empty if(InternalServerError){} block;
        // replaced MIME hardcode with GetFileTypeFromBytes helper;
        // redact sensitive headers before logging.
        public static ApiResponse GeneralJsonAPI(string apiUrl, object? data,
                                                                    Dictionary<string, string>? headerValues,
                                                                    Dictionary<string, string>? requiredParams,
                                                                    Dictionary<string, object>? formData,
                                                                    int timeOutInMilisecond, Method method, bool isSaveLog = true,
                                                                    [CallerMemberName] string callerName = "", [CallerLineNumber] int lineNumber = 0, [CallerFilePath] string filePath = "")
        {
            string apiId = Guid.NewGuid().ToString();
            string errorMessage = string.Empty;

            for (int i = 0; i < max_send_times; i++)
            {
                DateTime startTime = DateTime.Now;
                if (isSaveLog)
                {
                    ApiLogDetail apiLogDetail = new ApiLogDetail()
                    {
                        EndPoint = apiUrl,
                        ApiLogId = apiId,
                        CreatedDate = startTime,
                        ApiMethod = method.ToString(),
                        Headers = RedactSensitiveHeaders(headerValues),
                        Params = requiredParams,
                        Body = formData,
                        Description = $"START {i + 1}",
                        Action = Object.EmSystemAction.MainServer,
                        ActionType = Object.EmSystemActionType.INFO,
                    };
                    SystemUtils.logger.SaveAPILogDetail(apiLogDetail, callerName, lineNumber, filePath);
                }

                using var client = new RestClient(apiUrl);
                var request = new RestRequest
                {
                    Method = method,
                    RequestFormat = DataFormat.Json
                };
                if (timeOutInMilisecond > 0)
                {
                    request.Timeout = TimeSpan.FromMilliseconds(timeOutInMilisecond);
                }

                if (data != null)
                {
                    request.AddJsonBody(data);
                }


                if (formData != null)
                {
                    foreach (var kv in formData)
                    {
                        if (kv.Value is byte[] fileBytes)
                        {
                            var (ext, mime) = GetFileTypeFromBytes(fileBytes);
                            request.AddFile(kv.Key, fileBytes, $"{kv.Key}{ext}", mime);
                        }
                        else
                        {
                            request.AddParameter(kv.Key, kv.Value?.ToString());
                        }
                    }
                }

                if (headerValues != null)
                {
                    foreach (KeyValuePair<string, string> item in headerValues)
                    {
                        request.AddHeader(item.Key, item.Value);
                    }
                }

                if (requiredParams != null)
                {
                    foreach (KeyValuePair<string, string> kvp in requiredParams)
                    {
                        request.AddQueryParameter(kvp.Key, kvp.Value);
                    }
                }

                var response = client.Execute(request);

                if (isSaveLog)
                {
                    DateTime endTime = DateTime.Now;

                    ApiLogDetail apiLogDetail = new ApiLogDetail()
                    {
                        EndPoint = apiUrl,
                        ApiLogId = apiId,
                        CreatedDate = endTime,
                        ApiMethod = method.ToString(),
                        Headers = RedactSensitiveHeaders(headerValues),
                        Params = requiredParams,
                        Body = data,
                        ResponseStatus = (int)response.StatusCode,
                        ResponseContent = response.Content,
                        Ex = response.ErrorException,
                        Description = $"End {i + 1}",
                        Action = Object.EmSystemAction.MainServer,
                        ActionType = Object.EmSystemActionType.INFO,
                    };
                    SystemUtils.logger.SaveAPILogDetail(apiLogDetail, callerName, lineNumber, filePath);

                    ApiLog apiLog = new ApiLog()
                    {
                        Id = apiId,
                        EndPoint = apiUrl,
                        StartTime = startTime,
                        EndTime = endTime,
                        ApiMethod = method.ToString(),
                        Headers = RedactSensitiveHeaders(headerValues),
                        Params = requiredParams,
                        Body = data,
                        ResponseStatus = (int)response.StatusCode,
                        ResponseContent = response.Content,
                        Ex = response.ErrorException,
                        // TODO(typo): MainServer_Sumary — see note in first overload above.
                        Action = Object.EmSystemAction.MainServer_Sumary,
                        ActionType = Object.EmSystemActionType.INFO,
                    };
                    SystemUtils.logger.SaveAPILog(apiLog, callerName, lineNumber, filePath);
                }

                if (response.IsSuccessful)
                {
                    return new ApiResponse()
                    {
                        IsSuccess = true,
                        ApiStatusCode = (int)response.StatusCode,
                        ErrorMessage = response.ErrorMessage ?? "",
                        Response = response.Content ?? ""
                    };
                }
                //Không nhận được phản hồi từ server (Time out, ...)
                if (string.IsNullOrEmpty(response.Content))
                {
                    if (i == max_send_times - 1)
                    {
                        return new ApiResponse()
                        {
                            IsSuccess = false,
                            ApiStatusCode = (int)response.StatusCode,
                            ErrorMessage = response.ErrorMessage ?? "",
                            Response = ""
                        };
                    }
                    errorMessage = "Empty Response";
                    continue;
                }
                //Nhận được phản hồi từ server
                return new ApiResponse()
                {
                    IsSuccess = false,
                    ApiStatusCode = (int)response.StatusCode,
                    ErrorMessage = response.ErrorMessage ?? "",
                    Response = response.Content
                };

            }
            return new ApiResponse()
            {
                IsSuccess = false,
                ApiStatusCode = -1,
                ErrorMessage = "",
                Response = ""
            };
        }

        /// <summary>
        /// Detect file type from the first bytes (magic bytes) of a binary buffer.
        /// Returns extension (with leading dot) and MIME type.
        /// Falls back to ".bin" / "application/octet-stream" when unrecognised.
        /// Visibility: internal (not private) to allow direct unit testing via Kztek.Api.Tests.
        /// </summary>
        internal static (string extension, string mimeType) GetFileTypeFromBytes(byte[] bytes)
        {
            if (bytes != null && bytes.Length >= 2)
            {
                // JPEG: FF D8
                if (bytes[0] == 0xFF && bytes[1] == 0xD8)
                    return (".jpg", "image/jpeg");

                if (bytes.Length >= 4)
                {
                    // PNG: 89 50 4E 47
                    if (bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47)
                        return (".png", "image/png");

                    // PDF: 25 50 44 46  (%PDF)
                    if (bytes[0] == 0x25 && bytes[1] == 0x50 && bytes[2] == 0x44 && bytes[3] == 0x46)
                        return (".pdf", "application/pdf");
                }
            }
            return (".bin", "application/octet-stream");
        }

        /// <summary>
        /// Returns a shallow copy of the headers dictionary with values for known
        /// sensitive keys replaced by "***REDACTED***". The original dictionary is
        /// not modified and is still used for the actual HTTP request.
        /// Sensitive keys (case-insensitive): authorization, token, apikey, api-key,
        /// x-api-key, secret.
        /// Visibility: internal (not private) to allow direct unit testing via Kztek.Api.Tests.
        /// </summary>
        internal static Dictionary<string, string>? RedactSensitiveHeaders(Dictionary<string, string>? headers)
        {
            if (headers == null) return null;

            var sensitiveKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "authorization",
                "token",
                "apikey",
                "api-key",
                "x-api-key",
                "secret"
            };

            var redacted = new Dictionary<string, string>(headers.Count, StringComparer.OrdinalIgnoreCase);
            foreach (var kv in headers)
            {
                redacted[kv.Key] = sensitiveKeys.Contains(kv.Key) ? "***REDACTED***" : kv.Value;
            }
            return redacted;
        }

    }
}
