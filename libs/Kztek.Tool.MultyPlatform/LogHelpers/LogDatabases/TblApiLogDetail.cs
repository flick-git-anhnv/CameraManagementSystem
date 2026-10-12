using Kztek.Tool.TextFormatingHelpers;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Kztek.Tool.LogHelpers.LogDatabase
{
    public static class TblApiLogDetail1
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
                command.CommandText = @"CREATE TABLE IF NOT EXISTS tblAPILogDetail (
                                        Id Text NULL,
                                        EndPoint Text NULL,
                                        ApiLogId Text NULL,
                                        CreatedDate Text NULL,
                                        ApiMethod Text NULL,
                                        Headers Text NULL,
                                        Parameters Text NULL,
                                        Body Text NULL,
                                        ResponseStatus Text NULL,
                                        ResponseContent Text NULL,
                                        ResponseErrorMessage Text NULL,
                                        Description Text NULL,
                                        Method Text NULL,
                                        Line Text NULL,
                                        Class Text NULL
                                    )";
                command.ExecuteNonQuery();

                // 2?? Thêm các INDEX t?i uu
                command.CommandText = @"
                    -- L?c/xóa log chi ti?t cu nhanh theo CreatedDate
                    CREATE INDEX IF NOT EXISTS IX_tblAPILogDetail_CreatedDate ON tblAPILogDetail(CreatedDate);

                    -- L?c nhanh theo ApiLogId (khi xem chi ti?t c?a 1 log cha)
                    CREATE INDEX IF NOT EXISTS IX_tblAPILogDetail_ApiLogId ON tblAPILogDetail(ApiLogId);

                    -- L?c nhanh theo EndPoint (vd khi tìm log c?a 1 API c? th?)
                    CREATE INDEX IF NOT EXISTS IX_tblAPILogDetail_EndPoint ON tblAPILogDetail(EndPoint);

                    -- L?c nhanh theo ResponseStatus (tìm l?i, 200/500, v.v.)
                    CREATE INDEX IF NOT EXISTS IX_tblAPILogDetail_ResponseStatus ON tblAPILogDetail(ResponseStatus);

                    -- H? tr? truy v?n theo th?i gian và endpoint k?t h?p
                    CREATE INDEX IF NOT EXISTS IX_tblAPILogDetail_CreatedDate_EndPoint ON tblAPILogDetail(CreatedDate, EndPoint);
                    ";
                command.ExecuteNonQuery();
            }
        }
        public static void ClearLogAfterDays(int day)
        {
            Task.Run(new Action(() =>
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
                        string deleteQuery = @"DELETE FROM tblAPILogDetail WHERE CreatedDate < @TargetDate";
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

            }));


        }
        public static void ClearLogAfterTime(DateTime time)
        {
            Task.Run(new Action(() =>
            {
                try
                {
                    lock (LogToSQLite.lockObj)
                    {
                        if (LogToSQLite.logConnection is null)
                        {
                            return;
                        }
                        string deleteQuery = @"DELETE FROM tblAPILogDetail WHERE CreatedDate < @TargetDate";
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

            }));


        }

        public static void SaveLog(string id, string endPoint,
                                   DateTime createdDate, string apiLogId,
                                   string apiMethod, Dictionary<string, string> headers,
                                   Dictionary<string, string> parameters, object? body,
                                   int responseCode, string responseContent, object? exMessage,
                                   string description,
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
                        string logResponseContent = responseContent ?? "";
                        //try
                        //{
                        //    string cleanedJson = Regex.Replace(
                        //        logResponseContent,
                        //        @"""images"":\s*\[\s*\{.*?\}\s*\]",
                        //        @"""images"":[]",
                        //        RegexOptions.Singleline
                        //    );
                        //    logResponseContent = cleanedJson;
                        //}
                        //catch (Exception)
                        //{
                        //}

                        if (skipEndpoints.Contains(endPoint)) return;
                        if (LogToSQLite.logConnection is null) { return; }
                        string callerClassName = System.IO.Path.GetFileNameWithoutExtension(filePath ?? "");
                        var command = LogToSQLite.logConnection!.CreateCommand();
                        command.CommandText = @"INSERT INTO tblAPILogDetail (
                                            Id, EndPoint, CreatedDate, ApiLogId,
                                            ApiMethod, Headers, Parameters, Body, ResponseStatus,
                                            ResponseContent, ResponseErrorMessage, Description,
                                            Method, Line, Class
                                    ) VALUES (
                                            @Id, @EndPoint, @CreatedDate,
                                            @ApiLogId, @ApiMethod,
                                            @Headers, @Parameters, @Body,
                                            @ResponseStatus, @ResponseContent, @ResponseErrorMessage,
                                            @Description, @Method, @Line, @Class
                                    )";
                        command.Parameters.AddWithValue("@Id", id ?? "");
                        command.Parameters.AddWithValue("@EndPoint", endPoint ?? "");
                        command.Parameters.AddWithValue("@CreatedDate", createdDate.ToString("yyyy-MM-dd HH:mm:ss.ffffff"));
                        command.Parameters.AddWithValue("@ApiLogId", apiLogId ?? "");
                        command.Parameters.AddWithValue("@ApiMethod", apiMethod ?? "");
                        command.Parameters.AddWithValue("@Headers", Newtonsoft.Json.JsonConvert.SerializeObject(headers ?? new Dictionary<string, string>()));
                        command.Parameters.AddWithValue("@Parameters", Newtonsoft.Json.JsonConvert.SerializeObject(parameters ?? new Dictionary<string, string>()));
                        command.Parameters.AddWithValue("@Body", TextFormatingTool.GetLogMessage(body));
                        command.Parameters.AddWithValue("@ResponseStatus", responseCode);
                        command.Parameters.AddWithValue("@ResponseContent", logResponseContent ?? "");
                        command.Parameters.AddWithValue("@ResponseErrorMessage", TextFormatingTool.GetLogMessage(exMessage));
                        command.Parameters.AddWithValue("@Description", description ?? "");
                        command.Parameters.AddWithValue("@Method", callerName ?? "_");
                        command.Parameters.AddWithValue("@Line", lineNumber.ToString());
                        command.Parameters.AddWithValue("@Class", callerClassName ?? "_");
                        int x = command.ExecuteNonQuery();
                        command?.Dispose();
                    }

                }
                catch (Exception ex)
                {
                }

            }));
        }
        public static void SaveLog(string endPoint,
                                DateTime createdDate, string apiLogId,
                                string apiMethod, Dictionary<string, string> headers,
                                Dictionary<string, string> parameters, object? body,
                                int responseCode = 0, string responseContent = "", object? exMessage = null,
                                string description = "",
                                [CallerMemberName] string callerName = "",
                                [CallerLineNumber] int lineNumber = 0,
                                [CallerFilePath] string filePath = "")
        {
            SaveLog(Guid.NewGuid().ToString(), endPoint, createdDate, apiLogId,
                    apiMethod, headers, parameters, body,
                    responseCode, responseContent, exMessage, description,
                    callerName, lineNumber, filePath);
        }
    }

}

