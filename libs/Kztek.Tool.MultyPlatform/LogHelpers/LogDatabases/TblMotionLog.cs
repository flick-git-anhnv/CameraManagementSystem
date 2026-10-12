using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace Kztek.Tool.LogHelpers.LogDatabase
{
    public static class TblMotionLog1
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
                command.CommandText = @"CREATE TABLE IF NOT EXISTS tblMotionLog (
                                        Id,
                                        CreatedDate,
                                        VehicleImage,
                                        LprImage,
                                        DetectPlate
                                    )";
                command.ExecuteNonQuery();
            }
        }
        public static DataTable GetLogData(DateTime startTime, DateTime endTime)
        {
            try
            {
                if (LogToSQLite.logConnection is null)
                {
                    return new DataTable();
                }
                string startTimeStr = startTime.ToString("yyyy-MM-dd HH:mm:ss");
                string endTimeSTr = endTime.ToString("yyyy-MM-dd HH:mm:ss");
                string commandText = $@"SELECT * from tblMotionLog WHERE CreatedDate between '{startTimeStr}' AND '{endTimeSTr}'";
                using (var command = new SqliteCommand(commandText, LogToSQLite.logConnection))
                {
                    // Create a DataTable to hold the retrieved data
                    DataTable dataTable = new DataTable();

                    // Execute the query and load the result into the DataTable
                    using (var reader = command.ExecuteReader())
                    {
                        dataTable.Load(reader);
                    }
                    return dataTable;
                }
            }
            catch (Exception)
            {
                return new DataTable();
            }
        }

        public static void SaveLog(string id, DateTime createdDate,
                                   string vehicleImage, string lprImage, string plate)
        {
            Task.Run(new Action(() =>
            {
                try
                {
                    if (LogToSQLite.logConnection is null) { return; }
                    using (var command = LogToSQLite.logConnection!.CreateCommand())
                    {
                        command.CommandText = @"INSERT INTO tblMotionLog (
                                            Id, CreatedDate, VehicleImage, LprImage, DetectPlate
                                    ) VALUES (
                                            @Id, @CreatedDate, @VehicleImage,
                                            @LprImage, @DetectPlate
                                    )";
                        command.Parameters.AddWithValue("@Id", id);
                        command.Parameters.AddWithValue("@CreatedDate", createdDate.ToString("yyyy-MM-dd HH:mm:ss.ffffff"));
                        command.Parameters.AddWithValue("@VehicleImage", vehicleImage);
                        command.Parameters.AddWithValue("@LprImage", lprImage);
                        command.Parameters.AddWithValue("@DetectPlate", plate);
                        command.ExecuteNonQuery();
                    }
                }
                catch (Exception e)
                {
                }
            }));
        }
    }

}

