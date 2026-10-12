using Kztek.Tool;
using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace Kztek.Tool.LogHelpers
{
    public class LogToFile : ILogger
    {
        public string SaveLogFolder { get; set; }
        public bool IsSaveLog { get; set; } = true;

        public LogToFile(string saveLogFolder)
        {
            SaveLogFolder = saveLogFolder;
            LogToFileHelper.SaveLogFolder = saveLogFolder;
            InitLogService();
        }

        public void InitLogService()
        {
            try
            {
                if (!Directory.Exists(SaveLogFolder))
                {
                    Directory.CreateDirectory(SaveLogFolder);
                }
            }
            catch (Exception)
            {
            }
        }
        public void ClearLogAfterDay(int day)
        {
            try
            {
                DateTime before15Day = DateTime.Now.AddDays(-1 * day);
                string path = Path.Combine(SaveLogFolder, $"logs/{before15Day.Year}/{before15Day.Month}/{before15Day.Day}");
                if (Directory.Exists(path))
                {
                    Directory.Delete(path, true);
                }
            }
            catch (Exception)
            {
            }

        }
        public void ClearLogBeforeTime(DateTime time) { }

        public void SaveAPILog(ApiLog apiLog, [CallerMemberName] string callerName = "", [CallerLineNumber] int lineNumber = 0, [CallerFilePath] string filePath = "")
        {
            if (!IsSaveLog)
            {
                return;
            }
            var action = apiLog.Action;
            var actionType = apiLog.ActionType;
            var actionDetail = apiLog.ActionDetail;

            LogToFileHelper.Log(action, actionType, actionDetail, hanh_dong: apiLog.EndPoint, noi_dung_hanh_dong: apiLog.Description,
                                obj: apiLog, callerName: callerName, lineNumber: lineNumber, filePath: filePath);
        }

        public void SaveAPILogDetail(ApiLogDetail apiLogDetail, [CallerMemberName] string callerName = "", [CallerLineNumber] int lineNumber = 0, [CallerFilePath] string filePath = "")
        {
            if (!IsSaveLog)
            {
                return;
            }
            var action = apiLogDetail.Action;
            var actionType = apiLogDetail.ActionType;
            var actionDetail = apiLogDetail.ActionDetail;

            LogToFileHelper.Log(action, actionType, actionDetail, hanh_dong: apiLogDetail.EndPoint, noi_dung_hanh_dong: apiLogDetail.Description,
                                obj: apiLogDetail, callerName: callerName, lineNumber: lineNumber, filePath: filePath);
        }

        public void SaveDeviceLog(DeviceLog log,
                                [CallerMemberName] string callerName = "",
                                [CallerLineNumber] int lineNumber = 0,
                                [CallerFilePath] string filePath = "")
        {
            if (!IsSaveLog)
            {
                return;
            }
            var action = log.Action;
            var actionType = log.ActionType;
            var actionDetail = log.ActionDetail;

            LogToFileHelper.Log(action, actionType, actionDetail, log.Cmd, specailName: log.DeviceName, noi_dung_hanh_dong: log.Description,
                                obj: log, callerName: callerName, lineNumber: lineNumber, filePath: filePath);
        }

        public void SaveDeviceStatusLog(DeviceStatusLog log,
                                [CallerMemberName] string callerName = "",
                                [CallerLineNumber] int lineNumber = 0,
                                [CallerFilePath] string filePath = "")
        {
            if (!IsSaveLog)
            {
                return;
            }
            var action = log.Action;
            var actionType = log.ActionType;
            var actionDetail = log.ActionDetail;

            LogToFileHelper.Log(action, actionType, actionDetail, "", noi_dung_hanh_dong: log.Description,
                                obj: log, specailName: log.DeviceName, callerName: callerName, lineNumber: lineNumber, filePath: filePath);
        }

        public void SaveSystemLog(SystemLog log,
                                [CallerMemberName] string callerName = "",
                                [CallerLineNumber] int lineNumber = 0,
                                [CallerFilePath] string filePath = "")
        {
            if (!IsSaveLog)
            {
                return;
            }
            var action = log.Action;
            var actionType = log.ActionType;
            var actionDetail = log.ActionDetail;

            LogToFileHelper.Log(action, actionType, actionDetail, "", obj: log.Ex, noi_dung_hanh_dong: log.Description,
                                callerName: callerName, lineNumber: lineNumber, filePath: filePath);
        }

        public void SaveUserLog(UserLog log,
                                [CallerMemberName] string callerName = "",
                                [CallerLineNumber] int lineNumber = 0,
                                [CallerFilePath] string filePath = "")
        {
            if (!IsSaveLog)
            {
                return;
            }
            var action = log.SystemAction;
            var actionType = log.ActionType;
            var actionDetail = log.ActionDetail;

            LogToFileHelper.Log(action, actionType, actionDetail, "", noi_dung_hanh_dong: log.Description,
                                obj: log, callerName: callerName, lineNumber: lineNumber, filePath: filePath);
        }

        public void Disconnect()
        {
        }

        public string GetLogByQuery(string query)
        {
            return "";
        }
    }
}

