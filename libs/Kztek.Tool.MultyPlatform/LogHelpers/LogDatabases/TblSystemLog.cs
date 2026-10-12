using Kztek.Object;
using Kztek.Tool.TextFormatingHelpers;
using Microsoft.Data.Sqlite;
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Kztek.Tool.LogHelpers.LogDatabase
{
    public static class TblSystemLog1
    {
        public static void CreateIfNotExist()
        {
            if (LogToSQLite.logConnection is null)
            {
                return;
            }
            using (var command = LogToSQLite.logConnection.CreateCommand())
            {
                // Create a table if it doesn't exist
                command.CommandText = @"CREATE TABLE IF NOT EXISTS tblSystemLog(
                                        Id Text NULL,
                                        CreatedDate Text NULL,
                                        Action Text NULL,
                                        ActionDetail Text NULL,
                                        Description Text NULL,
                                        Ex Text NULL,
                                        Method Text NULL,
                                        Line Text NULL,
                                        Class Text NULL
                                    )";
                command.ExecuteNonQuery();

                // 2?? T?o các ch? m?c giúp truy v?n / xoá nhanh
                command.CommandText = @"
    -- Xoá log cu nhanh theo th?i gian
    CREATE INDEX IF NOT EXISTS IX_tblSystemLog_CreatedDate ON tblSystemLog(CreatedDate);

    -- L?c nhanh theo Action (vd: 'RestartService', 'Login', 'SyncDatabase', ...)
    CREATE INDEX IF NOT EXISTS IX_tblSystemLog_Action ON tblSystemLog(Action);

    -- L?c nhanh theo Class/Method (d? debug theo v? trí code)
    CREATE INDEX IF NOT EXISTS IX_tblSystemLog_Class_Method ON tblSystemLog(Class, Method);

    -- H? tr? truy v?n theo Action + th?i gian
    CREATE INDEX IF NOT EXISTS IX_tblSystemLog_Action_CreatedDate ON tblSystemLog(Action, CreatedDate);
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
                        string deleteQuery = @"DELETE FROM tblSystemLog WHERE CreatedDate < @TargetDate";
                        using (var command = new SqliteCommand(deleteQuery, LogToSQLite.logConnection))
                        {
                            command.Parameters.AddWithValue("@TargetDate",
                                                            cleafLogTime.ToString("yyyy-MM-dd HH:mm:ss"));
                            int x = command.ExecuteNonQuery();
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
                        string deleteQuery = @"DELETE FROM tblSystemLog WHERE CreatedDate < @TargetDate";
                        using (var command = new SqliteCommand(deleteQuery, LogToSQLite.logConnection))
                        {
                            command.Parameters.AddWithValue("@TargetDate", time.ToString("yyyy-MM-dd HH:mm:ss"));
                            int x = command.ExecuteNonQuery();
                        }
                    }
                }
                catch (Exception)
                {
                }
            }));

        }

        public static void SaveLog(string id,
                             EmSystemAction action,
                             EmSystemActionDetail actionDetail,
                             string description, object? externalData = null,
                             [CallerMemberName] string callerName = "",
                             [CallerLineNumber] int lineNumber = 0,
                             [CallerFilePath] string filePath = "")
        {
            Task.Run(() =>
            {
                try
                {
                    lock (LogToSQLite.lockObj)
                    {
                        var exLog = TextFormatingTool.GetLogMessage(externalData);

                        //try
                        //{
                        //    // This pattern will find and clear the Base64 value
                        //    string pattern = "\"Base64\"\\s*:\\s*\"[^\"]*\"";
                        //    string result = Regex.Replace(exLog, pattern, "\"Base64\":\"\"");

                        //    string cleanedJson = Regex.Replace(
                        //        result,
                        //        @"""images"":\s*\[\s*\{.*?\}\s*\]",
                        //        @"""images"":[]",
                        //        RegexOptions.Singleline
                        //    );
                        //    exLog = cleanedJson;
                        //}
                        //catch (Exception ex)
                        //{
                        //}

                        if (LogToSQLite.logConnection is null) { return; }
                        string callerClassName = System.IO.Path.GetFileNameWithoutExtension(filePath ?? "");
                        var command = LogToSQLite.logConnection!.CreateCommand();
                        {
                            command.CommandText = @"INSERT INTO tblSystemLog (
                                            Id, CreatedDate,
                                            Action, ActionDetail, Description, Ex,
                                            Method, Line, Class
                                    ) VALUES (
                                            @Id, @CreatedDate,
                                            @Action, @ActionDetail, @Description, @Ex, @Method, @Line, @Class
                                    )";
                            command.Parameters.AddWithValue("@Id", id);
                            command.Parameters.AddWithValue("@CreatedDate", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffffff"));
                            command.Parameters.AddWithValue("@Action", action.ToString());
                            command.Parameters.AddWithValue("@ActionDetail", actionDetail.ToString());
                            command.Parameters.AddWithValue("@Description", description ?? "");
                            command.Parameters.AddWithValue("@Ex", exLog);
                            command.Parameters.AddWithValue("@Method", callerName ?? "");
                            command.Parameters.AddWithValue("@Line", lineNumber.ToString());
                            command.Parameters.AddWithValue("@Class", callerClassName ?? "");
                            command.ExecuteNonQuery();
                            if (command != null)
                            {
                                command.Dispose();
                            }
                        }
                        int x = 1;
                    }
                }
                catch (Exception ex)
                {
                }

            });
        }

        public static void SaveLog(EmSystemAction action, EmSystemActionDetail actionDetail,
                             string description, object? externalData = null,
                             [CallerMemberName] string callerName = "",
                             [CallerLineNumber] int lineNumber = 0,
                             [CallerFilePath] string filePath = "")
        {
            SaveLog(Guid.NewGuid().ToString(), action, actionDetail, description, externalData,
                    callerName, lineNumber, filePath);
        }
    }

}

