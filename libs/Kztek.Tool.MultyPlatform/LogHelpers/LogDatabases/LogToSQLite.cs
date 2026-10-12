using Kztek.Tool;
using Microsoft.Data.Sqlite;
using System;
using System.Data;
using System.IO;
using System.Runtime.CompilerServices;

namespace Kztek.Tool.LogHelpers.LogDatabase
{
    public class LogToSQLite : ILogger
    {
        private const string dbName = "logv2.db";
        public static SqliteConnection? logConnection = null;
        public string SaveLogFolder { get; set; }

        public bool IsSaveLog { get; set; } = true;
        public static object lockObj = new object();

        public LogToSQLite(string saveLogFolder)
        {
            SaveLogFolder = saveLogFolder;
            InitLogService();
        }

        public void InitLogService()
        {
            string logDirectory = Path.Combine(SaveLogFolder, "logs");
            Directory.CreateDirectory(logDirectory);
            string dbPath = Path.Combine(logDirectory, dbName);
            logConnection = new SqliteConnection($"Data Source={dbPath}");
            logConnection.Open();
            // 1. Kiểm tra chế độ auto_vacuum hiện tại
            //using (var cmd = logConnection.CreateCommand())
            //{
            //    cmd.CommandText = "PRAGMA journal_mode=WAL;"; cmd.ExecuteNonQuery();
            //    cmd.CommandText = "PRAGMA synchronous=NORMAL;"; cmd.ExecuteNonQuery();
            //    cmd.CommandText = "PRAGMA temp_store=MEMORY;"; cmd.ExecuteNonQuery();
            //    cmd.CommandText = "PRAGMA mmap_size=268435456;"; cmd.ExecuteNonQuery(); // 256MB
            //    cmd.CommandText = "PRAGMA wal_autocheckpoint=1000;"; cmd.ExecuteNonQuery();
            //    cmd.CommandText = "PRAGMA cache_size=-20000;"; cmd.ExecuteNonQuery();   // ~20MB
            //}

            //long currentAutoVacuumMode = 0;
            //using (var command = logConnection.CreateCommand())
            //{
            //    try
            //    {
            //        command.CommandText = "PRAGMA auto_vacuum;";
            //        var result = command.ExecuteScalar();
            //        if (result != null && result != DBNull.Value)
            //        {
            //            currentAutoVacuumMode = (long)result;
            //        }
            //    }
            //    catch (Exception)
            //    {
            //    }
            //}

            //if (currentAutoVacuumMode == 0)
            //{
            //    using (var command = logConnection.CreateCommand())
            //    {
            //        command.CommandText = "PRAGMA auto_vacuum = FULL;";
            //        command.ExecuteNonQuery();
            //    }
            //    using (var command = logConnection.CreateCommand())
            //    {
            //        command.CommandText = "Vacuum;";
            //        command.ExecuteNonQuery();
            //    }
            //}
            ApplyPragmasForWriter(logConnection);

            long mode = 0;
            using (var cmd = logConnection.CreateCommand())
            {
                cmd.CommandText = "PRAGMA auto_vacuum;";
                var r = cmd.ExecuteScalar();
                if (r is long l) mode = l;
            }

            if (mode == 0) // NONE -> d?i sang INCREMENTAL và VACUUM 1 l?n
            {
                using var cmd = logConnection.CreateCommand();
                cmd.CommandText = "PRAGMA auto_vacuum=INCREMENTAL;"; cmd.ExecuteNonQuery();
                cmd.CommandText = "VACUUM;"; cmd.ExecuteNonQuery();
            }

            TblApiLog1.CreateIfNotExist();
            TblApiLogDetail1.CreateIfNotExist();
            TblDeviceLog1.CreateIfNotExist();
            TblDeviceStatusLog1.CreateIfNotExist();
            TblSystemLog1.CreateIfNotExist();
            TblUserLog1.CreateIfNotExist();
            TblMotionLog1.CreateIfNotExist();
        }

        private static void ApplyPragmasForWriter(SqliteConnection conn)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                PRAGMA journal_mode=WAL;
                PRAGMA synchronous=NORMAL;
                PRAGMA temp_store=MEMORY;
                PRAGMA mmap_size=268435456;         -- 256MB
                PRAGMA wal_autocheckpoint=1000;     -- 1000 pages
                PRAGMA cache_size=-20000;           -- ~20MB
                PRAGMA busy_timeout=5000;           -- 5s tránh 'database is locked'
                ";
            cmd.ExecuteNonQuery();
        }
        private static void ReclaimFreePages(SqliteConnection c, int pages = 2000)
        {
            lock (lockObj)
            {
                using var cmd = c.CreateCommand();
                cmd.CommandText = $"PRAGMA incremental_vacuum({pages});";
                cmd.ExecuteNonQuery();

                using var cmd2 = c.CreateCommand();
                cmd.CommandText = $"PRAGMA wal_checkpoint(TRUNCATE);";
                cmd.ExecuteNonQuery();
            }
        }

        public void ClearLogAfterDay(int day)
        {
            TblApiLog1.ClearLogAfterDays(day);
            TblApiLogDetail1.ClearLogAfterDays(day);
            TblDeviceLog1.ClearLogAfterDays(day);
            TblDeviceStatusLog1.ClearLogAfterDays(day);
            TblSystemLog1.ClearLogAfterDays(day);
            TblUserLog1.ClearLogAfterDays(day);

            if (logConnection is not null)
            {
                ReclaimFreePages(logConnection);
            }

        }
        public void ClearLogBeforeTime(DateTime time)
        {
            TblApiLog1.ClearLogAfterTime(time);
            TblApiLogDetail1.ClearLogAfterTime(time);
            TblDeviceLog1.ClearLogAfterTime(time);
            TblDeviceStatusLog1.ClearLogAfterTime(time);
            TblSystemLog1.ClearLogAfterTime(time);
            TblUserLog1.ClearLogAfterTime(time);

            if (logConnection is not null)
            {
                ReclaimFreePages(logConnection);
            }
        }

        public void SaveAPILog(ApiLog apiLog,
                                [CallerMemberName] string callerName = "",
                                [CallerLineNumber] int lineNumber = 0,
                                [CallerFilePath] string filePath = "")
        {
            if (!IsSaveLog)
            {
                return;
            }
            //for (int i = 0; i < 10000; i++)
            {
                TblApiLog1.SaveLog(apiLog.EndPoint, apiLog.StartTime, apiLog.EndTime, apiLog.ApiMethod,
                                   apiLog.Headers, apiLog.Params, apiLog.Body,
                                   apiLog.ResponseStatus, apiLog.ResponseContent, apiLog.Ex, apiLog.Description, callerName, lineNumber, filePath);
            }
        }

        public void SaveAPILogDetail(ApiLogDetail apiLogDetail,
                                [CallerMemberName] string callerName = "",
                                [CallerLineNumber] int lineNumber = 0,
                                [CallerFilePath] string filePath = "")
        {
            if (!IsSaveLog)
            {
                return;
            }
            TblApiLogDetail1.SaveLog(apiLogDetail.EndPoint, apiLogDetail.CreatedDate,
                                    apiLogDetail.ApiLogId, apiLogDetail.ApiMethod,
                                    apiLogDetail.Headers, apiLogDetail.Params,
                                    apiLogDetail.Body, apiLogDetail.ResponseStatus,
                                    apiLogDetail.ResponseContent, apiLogDetail.Ex, apiLogDetail.Description, callerName, lineNumber, filePath);
        }

        public void SaveDeviceLog(DeviceLog deviceLog,
                                [CallerMemberName] string callerName = "",
                                [CallerLineNumber] int lineNumber = 0,
                                [CallerFilePath] string filePath = "")
        {
            if (!IsSaveLog)
            {
                return;
            }
            TblDeviceLog1.SaveLog(deviceLog.DeviceId, deviceLog.DeviceName, deviceLog.Cmd,
                                 deviceLog.Response, deviceLog.Description, callerName, lineNumber, filePath);
        }

        public void SaveDeviceStatusLog(DeviceStatusLog deviceStatusLog,
                                [CallerMemberName] string callerName = "",
                                [CallerLineNumber] int lineNumber = 0,
                                [CallerFilePath] string filePath = "")
        {
            if (!IsSaveLog)
            {
                return;
            }
            TblDeviceStatusLog1.SaveLog(deviceStatusLog.DeviceId, deviceStatusLog.DeviceName,
                                       deviceStatusLog.DeviceStatus ? "Connect" : "Disconnect",
                                       deviceStatusLog.Description, callerName, lineNumber, filePath);
        }

        public void SaveSystemLog(SystemLog systemLog,
                                [CallerMemberName] string callerName = "",
                                [CallerLineNumber] int lineNumber = 0,
                                [CallerFilePath] string filePath = "")
        {
            if (!IsSaveLog)
            {
                return;
            }
            TblSystemLog1.SaveLog(systemLog.Action, systemLog.ActionDetail, systemLog.Description, systemLog.Ex, callerName, lineNumber, filePath);
        }

        public void SaveUserLog(UserLog userLog,
                                [CallerMemberName] string callerName = "",
                                [CallerLineNumber] int lineNumber = 0,
                                [CallerFilePath] string filePath = "")
        {
            if (!IsSaveLog)
            {
                return;
            }
            TblUserLog1.SaveLog(userLog.Action, userLog.Description, callerName, lineNumber, filePath);
        }

        public void Disconnect()
        {
            logConnection?.Close();
        }

        public string GetLogByQuery(string query)
        {
            lock (lockObj)
            {
                if (logConnection == null || string.IsNullOrEmpty(query))
                {
                    return "";
                }
                using (var command = new SqliteCommand(query, logConnection))
                {
                    try
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            DataTable dataTable = new DataTable();
                            dataTable.Load(reader);
                            var data = Newtonsoft.Json.JsonConvert.SerializeObject(dataTable);
                            return data;
                        }
                    }
                    catch (Exception ex)
                    {
                        return ex.Message;
                    }
                }
            }

        }
    }
}
