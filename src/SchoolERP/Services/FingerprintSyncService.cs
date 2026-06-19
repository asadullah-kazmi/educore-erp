using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using SchoolERP.Data;

namespace SchoolERP.Services
{
    public class FingerprintSyncService
    {
        private readonly IFingerprintDeviceClient deviceClient;

        public FingerprintSyncService(IFingerprintDeviceClient deviceClient)
        {
            this.deviceClient = deviceClient ?? throw new ArgumentNullException(nameof(deviceClient));
        }

        public FingerprintSyncResult SyncTeacherAttendance()
        {
            var result = new FingerprintSyncResult();

            if (!deviceClient.Connect())
            {
                result.Message = "Unable to connect to the fingerprint device.";
                return result;
            }

            try
            {
                var logs = deviceClient.GetAttendanceLogs();
                result.ReceivedLogs = logs.Count;

                foreach (var log in logs)
                {
                    if (!log.IsSuccessfulScan)
                    {
                        result.SkippedLogs++;
                        continue;
                    }

                    if (InsertAttendanceIfKnownTeacher(log))
                    {
                        result.InsertedAttendanceRows++;
                    }
                    else
                    {
                        result.SkippedLogs++;
                    }
                }

                result.Message = "Fingerprint logs synchronized.";
                return result;
            }
            finally
            {
                deviceClient.Disconnect();
            }
        }

        private static bool InsertAttendanceIfKnownTeacher(FingerprintLogEntry log)
        {
            const string teacherLookupSql = @"
SELECT TOP 1 TeacherID
FROM dbo.Teachers
WHERE FingerprintID = @FingerprintID;";

            const string attendanceExistsSql = @"
SELECT COUNT(1)
FROM dbo.Attendance
WHERE TeacherID = @TeacherID AND [Date] = @Date AND CAST(InTime AS DATE) = @Date;";

            using (var connection = Database.GetConnection())
            {
                connection.Open();

                // Check if Source and DeviceSerial columns exist
                bool sourceColumnExists = false;
                bool deviceSerialColumnExists = false;
                using (var checkColCommand = new SqlCommand(
                    @"SELECT name FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Attendance') AND name IN ('Source', 'DeviceSerial')",
                    connection))
                {
                    using (var reader = checkColCommand.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var colName = reader["name"] as string;
                            if (colName == "Source") sourceColumnExists = true;
                            if (colName == "DeviceSerial") deviceSerialColumnExists = true;
                        }
                    }
                }

                // Build insert SQL based on existing columns
                var columns = new List<string> { "TeacherID", "[Date]", "InTime", "Status" };
                var values = new List<string> { "@TeacherID", "@Date", "@InTime", "@Status" };

                if (sourceColumnExists)
                {
                    columns.Add("Source");
                    values.Add("@Source");
                }
                if (deviceSerialColumnExists)
                {
                    columns.Add("DeviceSerial");
                    values.Add("@DeviceSerial");
                }

                string insertAttendanceSql = $@"
INSERT INTO dbo.Attendance ({string.Join(", ", columns)})
VALUES ({string.Join(", ", values)});";

                int? teacherId = null;
                using (var lookup = new SqlCommand(teacherLookupSql, connection))
                {
                    lookup.Parameters.AddWithValue("@FingerprintID", log.FingerprintId);
                    var lookupResult = lookup.ExecuteScalar();
                    if (lookupResult != null && lookupResult != DBNull.Value)
                    {
                        teacherId = Convert.ToInt32(lookupResult);
                    }
                }

                if (!teacherId.HasValue)
                {
                    return false;
                }

                using (var exists = new SqlCommand(attendanceExistsSql, connection))
                {
                    exists.Parameters.AddWithValue("@TeacherID", teacherId.Value);
                    exists.Parameters.AddWithValue("@Date", log.CapturedAt.Date);

                    var existingCount = Convert.ToInt32(exists.ExecuteScalar());
                    if (existingCount > 0)
                    {
                        return false;
                    }
                }

                using (var insert = new SqlCommand(insertAttendanceSql, connection))
                {
                    insert.Parameters.AddWithValue("@TeacherID", teacherId.Value);
                    insert.Parameters.AddWithValue("@Date", log.CapturedAt.Date);
                    insert.Parameters.AddWithValue("@InTime", log.CapturedAt);
                    insert.Parameters.AddWithValue("@Status", "Present");
                    if (sourceColumnExists)
                    {
                        insert.Parameters.AddWithValue("@Source", "Biometric");
                    }
                    if (deviceSerialColumnExists)
                    {
                        insert.Parameters.AddWithValue("@DeviceSerial", (object)log.DeviceSerial ?? DBNull.Value);
                    }
                    insert.ExecuteNonQuery();
                    return true;
                }
            }
        }
    }
}
