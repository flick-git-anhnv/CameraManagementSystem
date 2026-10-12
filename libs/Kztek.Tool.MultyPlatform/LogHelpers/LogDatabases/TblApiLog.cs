using Kztek.Tool.TextFormatingHelpers;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Kztek.Tool.LogHelpers.LogDatabase
{
    public static class TblApiLog1
    {
        public static List<string> skipEndpoints = new List<string>();
        public static void CreateIfNotExist()
        {
            if (LogToSQLite.logConnection is null)
            {
                return;
            }
            using (var command = LogToSQLite.logConnection.CreateCommand())
            {
                // Create a table if it doesn't exist
                command.CommandText = @"CREATE TABLE IF NOT EXISTS tblAPILog (
                                        Id,
                                        EndPoint,
                                        StartTime,
                                        EndTime,
                                        Duration,
                                        ApiMethod,
                                        Headers,
                                        Parameters,
                                        Body,
                                        ResponseStatus,
                                        ResponseContent,
                                        ResponseErrorMessage,
                                        Description,
                                        Method,
                                        Line,
                                        Class
                                    )";
                command.ExecuteNonQuery();

                // 2?? T?o các ch? m?c (index) t?i uu cho vi?c d?c/xoá
                command.CommandText = @"
                    -- L?c/xoá log cu nhanh theo th?i gian
                    CREATE INDEX IF NOT EXISTS IX_tblAPILog_StartTime ON tblAPILog(StartTime);

                    -- Tìm theo endpoint (ví d? khi b?n l?c theo API name)
                    CREATE INDEX IF NOT EXISTS IX_tblAPILog_EndPoint ON tblAPILog(EndPoint);

                    -- Tìm theo mã ph?n h?i HTTP (vd ResponseStatus = 500)
                    CREATE INDEX IF NOT EXISTS IX_tblAPILog_ResponseStatus ON tblAPILog(ResponseStatus);

                    -- L?c nhanh theo class ho?c method (khi xem log theo v? trí code)
                    CREATE INDEX IF NOT EXISTS IX_tblAPILog_Class_Method ON tblAPILog(Class, Method);
                    ";

                command.ExecuteNonQuery();
            }
        }
        public static void ClearLogAfterDays(int day)
        {
            Task.Run(() =>
            {
                try
                {
                    lock (LogToSQLite.lockObj)
                    {
                        if (LogToSQLite.logConnection is null)
                        {
                            return;
                        }
                        DateTime cleafLogTime = DateTime.Now.AddDays(day);
                        string deleteQuery = @"DELETE FROM tblAPILog WHERE EndTime < @TargetDate";
                        using (var command = new SqliteCommand(deleteQuery, LogToSQLite.logConnection))
                        {
                            command.Parameters.AddWithValue("@TargetDate",
                                                            cleafLogTime.ToString("yyyy-MM-dd HH:mm:ss"));
                            command.ExecuteNonQuery();
                        }
                    }

                }
                catch (Exception)
                {
                }

            });
        }
        public static void ClearLogAfterTime(DateTime time)
        {
            Task.Run(() =>
            {
                try
                {
                    lock (LogToSQLite.lockObj)
                    {
                        if (LogToSQLite.logConnection is null)
                        {
                            return;
                        }
                        string deleteQuery = @"DELETE FROM tblAPILog WHERE EndTime < @TargetDate";
                        using (var command = new SqliteCommand(deleteQuery, LogToSQLite.logConnection))
                        {
                            command.Parameters.AddWithValue("@TargetDate", time.ToString("yyyy-MM-dd HH:mm:ss"));
                            command.ExecuteNonQuery();
                        }
                    }

                }
                catch (Exception)
                {
                }

            });
        }

        public static void SaveLog(string id, string endPoint,
                                   DateTime startTime, DateTime endTime,
                                   string apiMethod, Dictionary<string, string> headers,
                                   Dictionary<string, string> parameters, object? body,
                                   int responseStatus, string responseContent, object? exMessage,
                                   string description = "",
                                   [CallerMemberName] string callerName = "",
                                   [CallerLineNumber] int lineNumber = 0,
                                   [CallerFilePath] string filePath = "")
        {
            Task.Run(new Action(() =>
            {
                try
                {
                    lock (LogToSQLite.lockObj)
                    {
                        if (skipEndpoints.Contains(endPoint)) return;
                        if (LogToSQLite.logConnection is null) { return; }
                        string callerClassName = System.IO.Path.GetFileNameWithoutExtension(filePath ?? "");
                        var command = LogToSQLite.logConnection!.CreateCommand();
                        command.CommandText = @"INSERT INTO tblAPILog (
                                            Id, EndPoint, StartTime, EndTime, Duration,
                                            ApiMethod, Headers, Parameters, Body, ResponseStatus,
                                            ResponseContent, ResponseErrorMessage, Description,
                                            Method, Line, Class
                                    ) VALUES (
                                            @Id, @EndPoint, @StartTime,
                                            @EndTime, @Duration, @ApiMethod,
                                            @Headers, @Parameters, @Body,
                                            @ResponseStatus, @ResponseContent, @ResponseErrorMessage,
                                            @Description, @Method, @Line, @Class
                                    )";
                        command.Parameters.AddWithValue("@Id", id ?? "");
                        command.Parameters.AddWithValue("@EndPoint", endPoint ?? "");
                        command.Parameters.AddWithValue("@StartTime", startTime.ToString("yyyy-MM-dd HH:mm:ss.ffffff"));
                        command.Parameters.AddWithValue("@EndTime", endTime.ToString("yyyy-MM-dd HH:mm:ss.ffffff"));
                        command.Parameters.AddWithValue("@Duration", (endTime - startTime).TotalMilliseconds);
                        command.Parameters.AddWithValue("@ApiMethod", apiMethod);
                        command.Parameters.AddWithValue("@Headers", Newtonsoft.Json.JsonConvert.SerializeObject(headers ?? new Dictionary<string, string>()));
                        command.Parameters.AddWithValue("@Parameters", Newtonsoft.Json.JsonConvert.SerializeObject(parameters ?? new Dictionary<string, string>()));
                        command.Parameters.AddWithValue("@Body", TextFormatingTool.GetLogMessage(body));
                        command.Parameters.AddWithValue("@ResponseStatus", responseStatus);
                        command.Parameters.AddWithValue("@ResponseContent", responseContent ?? "");
                        command.Parameters.AddWithValue("@ResponseErrorMessage", TextFormatingTool.GetLogMessage(exMessage));
                        command.Parameters.AddWithValue("@Description", description ?? "");
                        command.Parameters.AddWithValue("@Method", callerName ?? "");
                        command.Parameters.AddWithValue("@Line", lineNumber.ToString());
                        command.Parameters.AddWithValue("@Class", callerClassName ?? "");
                        int x = command.ExecuteNonQuery();
                        command?.Dispose();
                    }

                }
                catch (Exception exx)
                {
                }
            }));
        }
        public static void SaveLog(string endPoint,
                                 DateTime startTime, DateTime endTime,
                                 string apiMethod, Dictionary<string, string> headers,
                                 Dictionary<string, string> parameters, object? body,
                                 int responseStatus, string responseContent, object? exMessage,
                                 string description = "",
                                 [CallerMemberName] string callerName = "",
                                 [CallerLineNumber] int lineNumber = 0,
                                 [CallerFilePath] string filePath = "")
        {
            SaveLog(Guid.NewGuid().ToString(), endPoint, startTime, endTime, apiMethod, headers, parameters,
                    body, responseStatus, responseContent, exMessage, description, callerName, lineNumber, filePath);
        }
    }
}

