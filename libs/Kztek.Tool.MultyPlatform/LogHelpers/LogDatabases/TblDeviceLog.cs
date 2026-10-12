using Microsoft.Data.Sqlite;
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Kztek.Tool.LogHelpers.LogDatabase
{
    public static class TblDeviceLog1
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
                command.CommandText = @"CREATE TABLE IF NOT EXISTS tblDeviceLog (
                                        Id Text NULL,
                                        DeviceId Text NULL,
                                        DeviceName Text NULL,
                                        CreatedDate Text NULL,
                                        Cmd Text NULL,
                                        Response Text NULL,
                                        Description Text NULL,
                                        Method Text NULL,
                                        Line Text NULL,
                                        Class Text NULL
                                    )";
                command.ExecuteNonQuery();

                // 2?? Thêm các INDEX t?i uu hóa cho vi?c d?c / xoá / th?ng kê
                command.CommandText = @"
    -- L?c / xoá log cu nhanh theo CreatedDate
    CREATE INDEX IF NOT EXISTS IX_tblDeviceLog_CreatedDate ON tblDeviceLog(CreatedDate);

    -- L?c nhanh theo DeviceId (thi?t b? c? th?)
    CREATE INDEX IF NOT EXISTS IX_tblDeviceLog_DeviceId ON tblDeviceLog(DeviceId);

    -- L?c nhanh theo DeviceName (tùy tru?ng h?p)
    CREATE INDEX IF NOT EXISTS IX_tblDeviceLog_DeviceName ON tblDeviceLog(DeviceName);

    -- K?t h?p DeviceId và th?i gian d? l?c theo thi?t b? + kho?ng th?i gian
    CREATE INDEX IF NOT EXISTS IX_tblDeviceLog_DeviceId_CreatedDate ON tblDeviceLog(DeviceId, CreatedDate);
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
                        string deleteQuery = @"DELETE FROM tblDeviceLog WHERE CreatedDate < @TargetDate";
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
                        string deleteQuery = @"DELETE FROM tblDeviceLog WHERE CreatedDate < @TargetDate";
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

        public static void SaveLog(string id, string deviceId, string deviceName,
                                   string cmd, string response, string description,
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
                            command.CommandText = @"INSERT INTO tblDeviceLog (
                                            Id, Deviceid, DeviceName, CreatedDate,
                                            Cmd, Response, Description,
                                            Method, Line, Class
                                    ) VALUES (
                                            @Id, @Deviceid, @DeviceName, @CreatedDate,
                                            @Cmd, @Response,
                                            @Description, @Method, @Line, @Class
                                    )";
                            command.Parameters.AddWithValue("@Id", id);
                            command.Parameters.AddWithValue("@Deviceid", deviceId);
                            command.Parameters.AddWithValue("@DeviceName", deviceName);
                            command.Parameters.AddWithValue("@CreatedDate", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffffff"));
                            command.Parameters.AddWithValue("@Cmd", cmd ?? "");
                            command.Parameters.AddWithValue("@Response", response);
                            command.Parameters.AddWithValue("@Description", description);
                            command.Parameters.AddWithValue("@Method", callerName ?? "");
                            command.Parameters.AddWithValue("@Line", lineNumber.ToString());
                            command.Parameters.AddWithValue("@Class", callerClassName ?? "");
                            command.ExecuteNonQuery();
                            command?.Dispose();
                        }

                    }
                }
                catch (Exception ex)
                {
                }
            }));
        }

        public static void SaveLog(string deviceId, string deviceName,
                            string cmd, string response, string description,
                            [CallerMemberName] string callerName = "",
                            [CallerLineNumber] int lineNumber = 0,
                            [CallerFilePath] string filePath = "")
        {
            SaveLog(Guid.NewGuid().ToString(), deviceId, deviceName,
                    cmd, response, description, callerName, lineNumber, filePath);
        }
    }
}

