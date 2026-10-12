using Microsoft.Data.Sqlite;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Kztek.Tool.LogHelpers.LocalData
{
    public class tblExitLog
    {
        public static SqliteConnection? logConnection = null;
        public string tblName => $"tblExitLog";

        private string laneName = string.Empty;
        private string SaveLogFolder = string.Empty;
        private string dbName => $"{laneName}.db";
        public static object lockObj = new object();

        public tblExitLog(string laneName, string saveLogFolder)
        {
            this.laneName = laneName;
            SaveLogFolder = saveLogFolder;
            InitLogService();
        }

        public void InitLogService()
        {
            string dbPath = Path.Combine(SaveLogFolder, dbName);
            logConnection = new SqliteConnection($"Data Source={dbPath}");
            logConnection.Open();
            CreateIfNotExist();

            // 1. Ki?m tra ch? d? auto_vacuum hi?n t?i
            long currentAutoVacuumMode = 0;
            using (var command = logConnection.CreateCommand())
            {
                try
                {
                    command.CommandText = "PRAGMA auto_vacuum;";
                    var result = command.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                    {
                        currentAutoVacuumMode = (long)result;
                    }
                }
                catch (Exception)
                {
                }
            }

            if (currentAutoVacuumMode == 0)
            {
                using (var command = logConnection.CreateCommand())
                {
                    command.CommandText = "PRAGMA auto_vacuum = FULL;";
                    command.ExecuteNonQuery();
                }
                using (var command = logConnection.CreateCommand())
                {
                    command.CommandText = "Vacuum;";
                    command.ExecuteNonQuery();
                }
            }
        }

        public void CreateIfNotExist()
        {
            if (logConnection is null)
            {
                return;
            }
            using (var command = logConnection.CreateCommand())
            {
                command.CommandText = $@"CREATE TABLE IF NOT EXISTS {tblName} (
                                        CreatedDate Text NULL,
                                        ExitId Text NULL,
                                        AccessKeyId Text NULL,
                                        LaneId Text NULL,
                                        ControllerId Text NULL,
                                        PlateNumber Text NULL,
                                        Status Text NULL,
                                        Money Text NULL
                                    )";
                command.ExecuteNonQuery();
            }
        }
        public void ClearLogAfterDays(int day)
        {
            Task.Run(new Action(() =>
            {
                try
                {
                    lock (lockObj)
                    {
                        if (logConnection is null)
                        {
                            return;
                        }
                        DateTime cleafLogTime = DateTime.Now.AddDays(day);
                        string deleteQuery = $@"DELETE FROM {tblName} WHERE CreatedDate < @TargetDate and Status = '{EmEntryLocalDataLogStatus.Entry}'";
                        using (var command = new SqliteCommand(deleteQuery, logConnection))
                        {
                            command.Parameters.AddWithValue("@TargetDate", cleafLogTime.ToString("yyyy-MM-dd HH:mm:ss"));
                            command.ExecuteNonQuery();
                        }
                    }
                }
                catch (Exception)
                {
                }
            }));

        }
        public void ClearLogAfterTime(DateTime time)
        {
            Task.Run(new Action(() =>
            {
                try
                {
                    lock (lockObj)
                    {
                        if (logConnection is null)
                        {
                            return;
                        }
                        string deleteQuery = $@"DELETE FROM {tblName} WHERE CreatedDate < @TargetDate and Status = '{EmEntryLocalDataLogStatus.Entry}'";
                        using (var command = new SqliteCommand(deleteQuery, logConnection))
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

        public void InitEvent(string exitId, string accessKeyId, string laneId,
                              string controllerId, string plateNumber)
        {
            Task.Run(new Action(() =>
            {

                try
                {
                    lock (lockObj)
                    {
                        if (logConnection is null) { return; }
                        var command = logConnection!.CreateCommand();
                        {
                            command.CommandText = $@"INSERT INTO {tblName} (
                                        ExitId, AccessKeyId, LaneId, ControllerId,
                                        PlateNumber, Status,CreatedDate, Money
                                    ) VALUES (
                                            @ExitId, @AccessKeyId, @LaneId, @ControllerId,
                                            @PlateNumber, @Status, @CreatedDate, @Money
                                    )";
                            command.Parameters.AddWithValue("@ExitId", exitId);
                            command.Parameters.AddWithValue("@AccessKeyId", accessKeyId);
                            command.Parameters.AddWithValue("@LaneId", laneId);
                            command.Parameters.AddWithValue("@ControllerId", controllerId ?? "");
                            command.Parameters.AddWithValue("@PlateNumber", plateNumber);
                            command.Parameters.AddWithValue("@Status", EmExitLocalDataLogStatus.Processing.ToString());
                            command.Parameters.AddWithValue("@CreatedDate", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffffff"));
                            command.Parameters.AddWithValue("@Money", 0);
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

        public void UpdateEvent(string exitId, EmExitLocalDataLogStatus status, string money)
        {
            Task.Run(new Action(() =>
            {
                try
                {
                    lock (lockObj)
                    {
                        if (logConnection is null) { return; }
                        var command = logConnection!.CreateCommand();
                        {
                            command.CommandText = $@"UPDATE {tblName} SET Status = @Status, Money = @Money WHERE ExitId = @ExitId";
                            command.Parameters.AddWithValue("@ExitId", exitId);
                            command.Parameters.AddWithValue("@Status", status.ToString());
                            command.Parameters.AddWithValue("@Money", money);
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
        public void UpdateEvent(string exitId, EmExitLocalDataLogStatus status)
        {
            Task.Run(new Action(() =>
            {
                try
                {
                    lock (lockObj)
                    {
                        if (logConnection is null) { return; }
                        var command = logConnection!.CreateCommand();
                        {
                            command.CommandText = $@"UPDATE {tblName} SET Status = @Status,  WHERE ExitId = @ExitId";
                            command.Parameters.AddWithValue("@ExitId", exitId);
                            command.Parameters.AddWithValue("@Status", status.ToString());
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
        public void DeleteEvent(string exitId)
        {
            try
            {
                lock (lockObj)
                {
                    if (logConnection is null) { return; }
                    var command = logConnection!.CreateCommand();
                    {
                        command.CommandText = $@"DELETE FROM {tblName} WHERE ExitId = @ExitId";
                        command.Parameters.AddWithValue("@ExitId", exitId);
                        int respone = command.ExecuteNonQuery();
                        command?.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
            }
        }

        public string GetTop1ExitIdWaitForConfirm()
        {
            if (logConnection is null)
                return string.Empty;

            using (var command = logConnection.CreateCommand())
            {
                command.CommandText = $@"SELECT ExitId FROM {tblName} WHERE Status = 'WaitForConfirm' ORDER BY CreatedDate ASC LIMIT 1";

                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return reader["ExitId"]?.ToString();
                    }
                }
            }

            return string.Empty;
        }
        public string GetTop1EntryIdWaitForConfirm()
        {
            if (logConnection is null)
                return string.Empty;

            using (var command = logConnection.CreateCommand())
            {
                command.CommandText = $@" SELECT ExitId, LaneId, PlateNumber FROM {tblName} WHERE Status = 'CreateTransaction' ORDER BY CreatedDate ASC LIMIT 1";

                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return reader["ExitId"]?.ToString();
                    }
                }
            }
            return string.Empty;
        }

    }
}

