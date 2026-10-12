using Kztek.Object;
using Kztek.Tool.FileHelpers;
using Kztek.Tool.TextFormatingHelpers;
using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Kztek.Tool.LogHelpers
{
    internal class LogToFileHelper
    {
        public static bool isSaveLog = true;
        public class FunctionExcecute
        {
            public string FunctionName { get; set; } = string.Empty;

            public string FileName { get; set; } = string.Empty;

            public int LineNumber { get; set; } = 0;
        }

        #region Properties
        private static readonly object lockObject = new object();
        public static string SaveLogFolder = "";
        #endregion End Properties

        #region: Public Function

        public static void Log(EmSystemAction action, EmSystemActionType actionType, EmSystemActionDetail actionDetail, string hanh_dong = "",
                               string noi_dung_hanh_dong = "", object? mo_ta_them = null,
                               object? obj = null, string specailName = "",
                               [CallerMemberName] string callerName = "",
                               [CallerLineNumber] int lineNumber = 0,
                               [CallerFilePath] string filePath = "")
        {

            if (!isSaveLog && actionType == EmSystemActionType.INFO)
            {
                return;
            }

            FunctionExcecute function = new FunctionExcecute();
            function.FunctionName = callerName;
            function.LineNumber = lineNumber;
            function.FileName = filePath;

            if (string.IsNullOrEmpty(hanh_dong))
            {
                hanh_dong = callerName;
            }

            if (string.IsNullOrEmpty(noi_dung_hanh_dong))
            {
                noi_dung_hanh_dong = hanh_dong;
            }

            Task.Run(() =>
            {
                DateTime logTime = DateTime.Now;
                string str_mo_ta = "";
                if (mo_ta_them != null)
                {
                    if (mo_ta_them is string)
                    {
                        str_mo_ta = mo_ta_them as string;
                    }
                    else
                    {
                        str_mo_ta = TextFormatingTool.BeautyJson(mo_ta_them);
                    }
                }
                string message = GetLogMessage(action, actionDetail, hanh_dong, noi_dung_hanh_dong, logTime, function, str_mo_ta);
                if (message.ToUpper().Contains("reporting/parking".ToUpper()))
                {
                    return;
                }
                Log(action, actionType, message, logTime, obj, specailName: specailName);
            });
        }
        #endregion:End Public Function

        #region: Private Function
        private static void Log(EmSystemAction action, EmSystemActionType actionType, string message, DateTime logTime, object? obj = null, int limitSize = 1000 * 1024, string specailName = "")
        {
            lock (lockObject)
            {
                if (string.IsNullOrEmpty(SaveLogFolder))
                {
                    SaveLogFolder = ".";
                }

                try
                {
                    message = message.Replace("Ex_Message", TextFormatingTool.BeautyJson(obj));

                    string InitPath = GetSavePath();
                    Directory.CreateDirectory(InitPath);

                    string pathFile = "";
                    string fileName = string.IsNullOrEmpty(specailName)
                        ? $"{action.ToString().ToUpper()}_{actionType}_{DateTime.Now.Hour}_1.html"
                        : $"{specailName}_{action}_{actionType}_{DateTime.Now.Hour}_1.html";

                    string basePath = Path.Combine(InitPath, fileName);

                    pathFile = GetLogFilePathByLimitSize(basePath, limitSize);

                    string writeLogContent = string.Empty;
                    if (File.Exists(pathFile))
                    {
                        string oldContent = "";
                        using (StreamReader reader = new StreamReader(pathFile))
                        {
                            oldContent = reader.ReadToEnd();
                            oldContent = oldContent.Replace("<a href=\"#{next-id}\" class=\"move\">next</a>",
                                                          $@"<a href=""#{logTime:yyyy-MM-dd HH:mm:ss.fff}"" class = ""btn"">Next {logTime:yyyy-MM-dd HH:mm:ss.fff}</a>");
                            reader.Dispose();
                        }
                        string logContent = GetLogContent(message, logTime).TrimEnd(); ;
                        writeLogContent = oldContent.Replace("<p style=\"display: none;\">{log_content}</p>", logContent);
                    }
                    else
                    {
                        string initContent = InitContent();
                        string logContent = GetLogContent(message, logTime);
                        writeLogContent = initContent.Replace("<p style=\"display: none;\">{log_content}</p>", logContent);
                    }

                    using (StreamWriter writer = new StreamWriter(pathFile, false))
                    {
                        try
                        {
                            writer.WriteLine(writeLogContent);
                            writer.Dispose();
                        }
                        catch
                        {
                        }
                    }
                }
                finally
                {
                    GC.Collect();
                }
            }
        }

        private static string GetLogFilePathByLimitSize(string basePath, int limitSizeInByte)
        {
            if (FileHelper.GetFileSize(basePath) > limitSizeInByte)
            {
                var temp = basePath.Split('_');
                int currentIndex = int.Parse(temp[temp.Length - 1].Split('.')[0]);
                int nextIndex = currentIndex + 1;
                temp[temp.Length - 1] = nextIndex + ".html";
                string updatePath = string.Join("_", temp);
                return GetLogFilePathByLimitSize(updatePath, limitSizeInByte);
            }
            else
            {
                return basePath;
            }
        }
        private static string GetSavePath()
        {
            var now = DateTime.Now;
            return Path.Combine(SaveLogFolder, "logs", now.Year.ToString(), now.Month.ToString(), now.Day.ToString());
        }
        private static string InitContent()
        {
            try
            {
                string path = Path.Combine(SaveLogFolder + "/LogFormat/log.html");
                if (System.IO.File.Exists(path))
                {
                    using (StreamReader reader = new StreamReader(path))
                    {
                        return reader.ReadToEnd();
                    }
                }
            }
            catch {}
            finally
            {
                GC.Collect();
            }

            return @"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta http-equiv=""X-UA-Compatible"" content=""IE=edge"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Log</title>
</head>
<body style=""width: 100%"">
    <div class=""log-content"">
        <p style=""display: none;"">{log_content}</p>
    </div>
</body>
</html>";
        }
        private static string GetLogItemContent()
        {
            try
            {
                string path = Path.Combine(SaveLogFolder + "/LogFormat/log_item.html");
                if (System.IO.File.Exists(path))
                {
                    using (StreamReader reader = new StreamReader(path))
                    {
                        return reader.ReadToEnd();
                    }
                }
            }
            catch {}
            finally
            {
                GC.Collect();
            }

            return @"<div id=""{current-time}"" style=""border: 1px solid green; padding-left:10px; margin-bottom:5px"">
    {message}
    <a href=""#{next-id}"" class=""move"">next</a>
</div>
<p style=""display: none;"">{log_content}</p>";
        }
        private static string GetLogContent(string logMessage, DateTime logTime)
        {
            string content = GetLogItemContent();
            content = content.Replace("{current-time}", logTime.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            content = content.Replace("{message}", logMessage);
            return content;
        }
        private static string GetLogMessage(EmSystemAction aciton, EmSystemActionDetail actionDetail, string hanh_dong,
                                            string noi_dung_hanh_dong, DateTime logTime,
                                            FunctionExcecute? functionExcecute, string mo_ta_them = "")
        {

            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine($@"<table >
                        <tr>
                            <td><b>Thời gian: </b></td>
                            <td style=""width: 100%;"">{logTime:dd/MM/yyyy HH:mm:ss}</td>
                        </tr>
                        <tr>
                          <td><b>Đối tượng: </b></td>
                          <td>{aciton}{actionDetail}</td>
                        </tr>
                        <tr>
                            <td><b>Hành động: </b></td>
                            <td>{hanh_dong}</td>
                        </tr>
                        <tr>
                            <td><b>Nội dung hành động: </b></td>
                            <td><p style=""color: red;"">{noi_dung_hanh_dong}</p></td>
                        </tr>");
            if (!string.IsNullOrEmpty(mo_ta_them))
            {
                stringBuilder.AppendLine($@"<tr>
                            <td><b>Thông tin thêm: </b></td>
                            <td><pre>{mo_ta_them}</pre></td>
                        </tr>");
            }
            if (!string.IsNullOrEmpty(functionExcecute?.FunctionName))
            {
                stringBuilder.AppendLine($@" <tr>
                            <td><b>Function Name: </b></td>
                            <td>{functionExcecute?.FunctionName}</td>
                        </tr>");
            }
            if (!string.IsNullOrEmpty(functionExcecute?.FileName))
            {
                stringBuilder.AppendLine($@" <tr>
                            <td><b>File Name: </b></td>
                            <td>{functionExcecute?.FileName}</td>
                        </tr>");
            }
            if (!string.IsNullOrEmpty(functionExcecute?.LineNumber.ToString()))
            {
                stringBuilder.AppendLine($@"<tr>
                            <td><b>Line Number: </b></td>
                            <td>{functionExcecute?.LineNumber}</td>
                        </tr>");
            }
            stringBuilder.AppendLine($@"<tr>
                            <td><b>Object: </b></td>
                            <td><pre style=""white-space: pre-wrap; word-wrap: break-word; overflow-wrap: break-word;"">Ex_Message</pre></td>
                        </tr>
                      </table>");
            return stringBuilder.ToString().TrimEnd();
        }
        #endregion: End Private Function
    }

}

