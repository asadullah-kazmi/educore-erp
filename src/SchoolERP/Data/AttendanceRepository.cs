using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using SchoolERP.Models;

namespace SchoolERP.Data
{
    public class AttendanceRepository
    {
        public async Task<List<TeacherAttendanceRow>> GetTeacherAttendanceByDateAsync(DateTime date)
        {
            const string sql = @"
SELECT 
    t.TeacherID, 
    t.Name, 
    t.Designation, 
    t.FingerprintID, 
    a.AttendanceID, 
    a.Date, 
    ISNULL(a.Status, 'Not Marked') AS Status, 
    a.InTime
FROM dbo.Teachers t
LEFT JOIN dbo.Attendance a ON t.TeacherID = a.TeacherID AND a.Date = @Date
ORDER BY t.Name;";

            var results = new List<TeacherAttendanceRow>();

            using (var connection = Database.GetConnection())
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@Date", date.Date);
                await connection.OpenAsync().ConfigureAwait(false);

                using (var reader = await command.ExecuteReaderAsync().ConfigureAwait(false))
                {
                    while (await reader.ReadAsync().ConfigureAwait(false))
                    {
                        results.Add(MapTeacherAttendanceRow(reader));
                    }
                }
            }

            return results;
        }

        public async Task<bool> UpsertAttendanceAsync(int teacherId, DateTime date, string status, DateTime? inTime, string source = "Manual")
        {
            const string checkSql = @"
SELECT COUNT(1) 
FROM dbo.Attendance 
WHERE TeacherID = @TeacherID AND Date = @Date;";

            const string updateSql = @"
UPDATE dbo.Attendance 
SET Status = @Status, InTime = @InTime 
WHERE TeacherID = @TeacherID AND Date = @Date;";

            // First check if Source column exists
            bool sourceColumnExists = false;
            using (var checkConnection = Database.GetConnection())
            {
                await checkConnection.OpenAsync().ConfigureAwait(false);
                using (var checkColCommand = new SqlCommand(
                    @"SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Attendance') AND name = 'Source'", 
                    checkConnection))
                {
                    var result = await checkColCommand.ExecuteScalarAsync().ConfigureAwait(false);
                    sourceColumnExists = result != null && result != DBNull.Value;
                }
            }

            string insertSql = sourceColumnExists 
                ? @"INSERT INTO dbo.Attendance (TeacherID, Date, Status, InTime, Source) 
                    VALUES (@TeacherID, @Date, @Status, @InTime, @Source);" 
                : @"INSERT INTO dbo.Attendance (TeacherID, Date, Status, InTime) 
                    VALUES (@TeacherID, @Date, @Status, @InTime);";

            using (var connection = Database.GetConnection())
            {
                await connection.OpenAsync().ConfigureAwait(false);

                // Check if row exists
                int count;
                using (var checkCommand = new SqlCommand(checkSql, connection))
                {
                    checkCommand.Parameters.AddWithValue("@TeacherID", teacherId);
                    checkCommand.Parameters.AddWithValue("@Date", date.Date);
                    count = (int)await checkCommand.ExecuteScalarAsync().ConfigureAwait(false);
                }

                using (var command = new SqlCommand(count > 0 ? updateSql : insertSql, connection))
                {
                    command.Parameters.AddWithValue("@TeacherID", teacherId);
                    command.Parameters.AddWithValue("@Date", date.Date);
                    command.Parameters.AddWithValue("@Status", status);
                    command.Parameters.AddWithValue("@InTime", (object)inTime ?? DBNull.Value);
                    if (count == 0 && sourceColumnExists) // Only set source on insert if column exists
                    {
                        command.Parameters.AddWithValue("@Source", source);
                    }
                    return await command.ExecuteNonQueryAsync().ConfigureAwait(false) > 0;
                }
            }
        }

        public async Task<List<TeacherAttendanceRow>> GetTeacherAttendanceByMonthAsync(int teacherId, int year, int month)
        {
            const string sql = @"
SELECT 
    t.TeacherID, 
    t.Name, 
    t.Designation, 
    t.FingerprintID, 
    a.AttendanceID, 
    a.Date, 
    ISNULL(a.Status, 'Not Marked') AS Status, 
    a.InTime
FROM dbo.Teachers t
LEFT JOIN dbo.Attendance a ON t.TeacherID = a.TeacherID 
    AND YEAR(a.Date) = @Year AND MONTH(a.Date) = @Month
WHERE t.TeacherID = @TeacherID
ORDER BY a.Date;";

            var results = new List<TeacherAttendanceRow>();

            using (var connection = Database.GetConnection())
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@TeacherID", teacherId);
                command.Parameters.AddWithValue("@Year", year);
                command.Parameters.AddWithValue("@Month", month);
                await connection.OpenAsync().ConfigureAwait(false);

                using (var reader = await command.ExecuteReaderAsync().ConfigureAwait(false))
                {
                    while (await reader.ReadAsync().ConfigureAwait(false))
                    {
                        results.Add(MapTeacherAttendanceRow(reader));
                    }
                }
            }

            return results;
        }

        public async Task<AttendanceSummary> GetMonthlyAttendanceSummaryAsync(int year, int month)
        {
            var summary = new AttendanceSummary
            {
                Year = year,
                Month = new DateTime(year, month, 1).ToString("MMMM yyyy")
            };

            // Get all teachers
            var staffRepo = new StaffRepository();
            var teachers = await staffRepo.GetAllStaffAsync().ConfigureAwait(false);

            // Get all attendance for the month
            const string attendanceSql = @"
SELECT TeacherID, Status 
FROM dbo.Attendance 
WHERE TeacherID IS NOT NULL 
AND YEAR(Date) = @Year AND MONTH(Date) = @Month;";

            var attendanceRecords = new Dictionary<int, List<string>>();

            using (var connection = Database.GetConnection())
            using (var command = new SqlCommand(attendanceSql, connection))
            {
                command.Parameters.AddWithValue("@Year", year);
                command.Parameters.AddWithValue("@Month", month);
                await connection.OpenAsync().ConfigureAwait(false);

                using (var reader = await command.ExecuteReaderAsync().ConfigureAwait(false))
                {
                    while (await reader.ReadAsync().ConfigureAwait(false))
                    {
                        var teacherId = reader.GetInt32(reader.GetOrdinal("TeacherID"));
                        var status = reader["Status"] as string;

                        if (!attendanceRecords.ContainsKey(teacherId))
                        {
                            attendanceRecords[teacherId] = new List<string>();
                        }
                        attendanceRecords[teacherId].Add(status);
                    }
                }
            }

            // Calculate working days in the month (excluding weekends, but we'll count all days for now)
            var totalWorkingDays = DateTime.DaysInMonth(year, month);

            foreach (var teacher in teachers)
            {
                var row = new TeacherAttendanceSummary
                {
                    TeacherID = teacher.TeacherID,
                    Name = teacher.Name,
                    TotalWorkingDays = totalWorkingDays
                };

                if (attendanceRecords.TryGetValue(teacher.TeacherID, out var statuses))
                {
                    row.PresentDays = statuses.Count(s => s == "Present");
                    row.AbsentDays = statuses.Count(s => s == "Absent");
                    row.NotMarkedDays = totalWorkingDays - statuses.Count;
                }
                else
                {
                    row.NotMarkedDays = totalWorkingDays;
                }

                summary.Rows.Add(row);
            }

            return summary;
        }

        public async Task<bool> DeleteAttendanceAsync(int attendanceId)
        {
            const string sql = "DELETE FROM dbo.Attendance WHERE AttendanceID = @AttendanceID;";

            using (var connection = Database.GetConnection())
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@AttendanceID", attendanceId);
                await connection.OpenAsync().ConfigureAwait(false);
                return await command.ExecuteNonQueryAsync().ConfigureAwait(false) > 0;
            }
        }

        private static TeacherAttendanceRow MapTeacherAttendanceRow(SqlDataReader reader)
        {
            return new TeacherAttendanceRow
            {
                TeacherID = reader.GetInt32(reader.GetOrdinal("TeacherID")),
                Name = reader["Name"] as string,
                Designation = reader["Designation"] as string,
                FingerprintID = reader["FingerprintID"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["FingerprintID"]),
                AttendanceID = reader["AttendanceID"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["AttendanceID"]),
                Date = reader["Date"] == DBNull.Value ? DateTime.Today : Convert.ToDateTime(reader["Date"]),
                Status = reader["Status"] as string ?? "Not Marked",
                InTime = reader["InTime"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["InTime"])
            };
        }
    }
}
