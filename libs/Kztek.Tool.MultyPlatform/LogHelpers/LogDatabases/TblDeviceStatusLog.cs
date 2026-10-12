using Microsoft.Data.Sqlite;
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Kztek.Tool.LogHelpers.LogDatabase
{
    public static class TblDeviceStatusLog1
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
                command.CommandText = @"CREATE TABLE IF NOT EXISTS tblDeviceStatusLog(
                                        Id Text NULL,
                                        DeviceId Text NULL,
                                        DeviceName Text NULL,
                                        CreatedDate Text NULL,
                                        DeviceStatus Text NULL,
                                        Description Text NULL,
                                        Method Text NULL,
                                        Line Text NULL,
                                        Class Text NULL
                                    )";
                command.ExecuteNonQuery();
            }
        }
        public static void ClearLogAfterDays(int day)
        {
            //if (LogHelper.logConnection is null)
            //{
            //    return;
            //}
            //DateTime cleafLogTime = DateTime.Now.AddDays(day);
            //string deleteQuery = @"DELETE FROM tblDeviceStatusLog WHERE CreatedDate < @TargetDate";
            //using (var command = new SqliteCommand(deleteQuery, LogHelper.logConnection))
            //{
            //    command.Parameters.AddWithValue("@TargetDate",
            //                                    cleafLogTime.ToString("yyyy-MM-dd HH:mm:ss"));
            //    command.ExecuteNonQuery();
            //}
        }
        public static void ClearLogAfterTime(DateTime time)
        {
            //if (LogHelper.logConnection is null)
            //{
            //    return;
            //}
            //DateTime cleafLogTime = DateTime.Now.AddDays(day);
            //string deleteQuery = @"DELETE FROM tblDeviceStatusLog WHERE CreatedDate < @TargetDate";
            //using (var command = new SqliteCommand(deleteQuery, LogHelper.logConnection))
            //{
            //    command.Parameters.AddWithValue("@TargetDate",
            //                                    cleafLogTime.ToString("yyyy-MM-dd HH:mm:ss"));
            //    command.ExecuteNonQuery();
            //}
        }

        public static void SaveLog(string id, string deviceId, string deviceName,
                                   string DeviceStatus, string description,
                                   [CallerMemberName] string callerName = "",
                                   [CallerLineNumber] int lineNumber = 0,
                                   [CallerFilePath] string filePath = "")
        {
            try
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
                                command.CommandText = @"INSERT INTO tblDeviceStatusLog (
                                            Id, Deviceid, DeviceName, CreatedDate,
                                            DeviceStatus, Description,
                                            Method, Line, Class
                                    ) VALUES (
                                            @Id, @Deviceid, @DeviceName, @CreatedDate,
                                            @DeviceStatus, @Description, 
                                            @Method, @Line, @Class
                                    )";
                                command.Parameters.AddWithValue("@Id", id);
                                command.Parameters.AddWithValue("@DeviceId", deviceId ?? "");
                                command.Parameters.AddWithValue("@DeviceName", deviceName ?? "");
                                command.Parameters.AddWithValue("@CreatedDate", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffffff"));
                                command.Parameters.AddWithValue("@DeviceStatus", DeviceStatus ?? "");
                                command.Parameters.AddWithValue("@Description", description ?? "");
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
            catch (Exception)
            {
            }
        }

        public static void SaveLog(string deviceId, string deviceName,
                                  string DeviceStatus, string description,
                                  [CallerMemberName] string callerName = "",
                                  [CallerLineNumber] int lineNumber = 0,
                                  [CallerFilePath] string filePath = "")
        {
            SaveLog(Guid.NewGuid().ToString(), deviceId, deviceName,
                    DeviceStatus, description, callerName, lineNumber, filePath);
        }
    }

}

