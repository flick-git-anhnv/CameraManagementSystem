using Microsoft.Data.Sqlite;
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Kztek.Tool.LogHelpers.LogDatabase
{
    public static class TblUserLog1
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
                command.CommandText = @"CREATE TABLE IF NOT EXISTS tblUserLog(
                                        Id Text NULL,
                                        CreatedDate Text NULL,
                                        Action Text NULL,
                                        Description Text NULL,
                                        Method Text NULL,
                                        Line Text NULL,
                                        Class Text NULL
                                    )";
                command.ExecuteNonQuery();

                // 2) Index ph?c v? d?c/xoá nhanh
                command.CommandText = @"
    -- Xóa log cu nhanh theo th?i gian
    CREATE INDEX IF NOT EXISTS IX_tblUserLog_CreatedDate ON tblUserLog(CreatedDate);

    -- L?c theo hành d?ng (d?m/tra c?u nhanh)
    CREATE INDEX IF NOT EXISTS IX_tblUserLog_Action ON tblUserLog(Action);

    -- Truy v?n theo action + th?i gian (dashboard/th?ng kê)
    CREATE INDEX IF NOT EXISTS IX_tblUserLog_Action_CreatedDate ON tblUserLog(Action, CreatedDate);

    -- Debug theo v? trí code
    CREATE INDEX IF NOT EXISTS IX_tblUserLog_Class_Method ON tblUserLog(Class, Method);
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
                        string deleteQuery = @"DELETE FROM tblUserLog WHERE CreatedDate < @TargetDate";
                        using (var command = new SqliteCommand(deleteQuery, LogToSQLite.logConnection))
                        {
                            command.Parameters.AddWithValue("@TargetDate", cleafLogTime.ToString("yyyy-MM-dd HH:mm:ss"));
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
                        string deleteQuery = @"DELETE FROM tblUserLog WHERE CreatedDate < @TargetDate";
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

        public static void SaveLog(string id, string action, string description,
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
                        if (LogToSQLite.logConnection is null) { return; }
                        string callerClassName = System.IO.Path.GetFileNameWithoutExtension(filePath ?? "");
                        var command = LogToSQLite.logConnection!.CreateCommand();
                        {
                            command.CommandText = @"INSERT INTO tblUserLog (
                                            Id, CreatedDate,
                                            Action, Description,
                                            Method, Line, Class
                                    ) VALUES (
                                            @Id, @CreatedDate,
                                            @Action, @Description, @Method, @Line, @Class
                                    )";
                            command.Parameters.AddWithValue("@Id", id);
                            command.Parameters.AddWithValue("@CreatedDate", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffffff"));
                            command.Parameters.AddWithValue("@Action", action);
                            command.Parameters.AddWithValue("@Description", description ?? "");
                            command.Parameters.AddWithValue("@Method", callerName ?? "");
                            command.Parameters.AddWithValue("@Line", lineNumber.ToString());
                            command.Parameters.AddWithValue("@Class", callerClassName ?? "");
                            command.ExecuteNonQuery();
                            command?.Dispose();
                        }
                    }
                }
                catch (Exception)
                {
                }
            }));
        }
        public static void SaveLog(string action, string description,
                       [CallerMemberName] string callerName = "",
                       [CallerLineNumber] int lineNumber = 0,
                       [CallerFilePath] string filePath = "")
        {
            SaveLog(Guid.NewGuid().ToString(), action, description,
                    callerName, lineNumber, filePath);
        }
    }
}

