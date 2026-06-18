using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;
using SchoolERP.Models;

namespace SchoolERP.Data
{
    public class MonthlyReportRepository
    {
        public async Task<MonthlyFinanceSummary> GetMonthlySummaryAsync(string month)
        {
            const string feesSql = @"
SELECT ISNULL(SUM(Amount), 0)
FROM dbo.Fees
WHERE Status = 'Paid'
  AND LTRIM(RTRIM(Month)) = @Month;";

            const string expensesSql = @"
SELECT ISNULL(SUM(Amount), 0)
FROM dbo.Expenses
WHERE FORMAT([Date], 'MMM yyyy') = @Month;";

            const string salariesSql = @"
SELECT ISNULL(SUM(Amount), 0)
FROM dbo.SalaryPayments
WHERE FORMAT(PaymentDate, 'MMM yyyy') = @Month;";

            using (var connection = Database.GetConnection())
            {
                await connection.OpenAsync().ConfigureAwait(false);

                decimal totalFeesCollected;
                using (var command = new SqlCommand(feesSql, connection))
                {
                    command.Parameters.AddWithValue("@Month", month ?? (object)DBNull.Value);
                    var result = await command.ExecuteScalarAsync().ConfigureAwait(false);
                    totalFeesCollected = result == null || result == DBNull.Value ? 0m : Convert.ToDecimal(result);
                }

                decimal totalExpenses;
                using (var command = new SqlCommand(expensesSql, connection))
                {
                    command.Parameters.AddWithValue("@Month", month ?? (object)DBNull.Value);
                    var result = await command.ExecuteScalarAsync().ConfigureAwait(false);
                    totalExpenses = result == null || result == DBNull.Value ? 0m : Convert.ToDecimal(result);
                }

                decimal totalSalariesPaid;
                using (var command = new SqlCommand(salariesSql, connection))
                {
                    command.Parameters.AddWithValue("@Month", month ?? (object)DBNull.Value);
                    var result = await command.ExecuteScalarAsync().ConfigureAwait(false);
                    totalSalariesPaid = result == null || result == DBNull.Value ? 0m : Convert.ToDecimal(result);
                }

                return new MonthlyFinanceSummary
                {
                    Month = month,
                    TotalFeesCollected = totalFeesCollected,
                    TotalExpenses = totalExpenses,
                    TotalSalariesPaid = totalSalariesPaid
                };
            }
        }

        public async Task<List<FeeCollectionReportRow>> GetFeeCollectionReportAsync(string month)
        {
            const string sql = @"
SELECT s.Name,
       s.RegistrationNo,
       c.ClassName,
       s.MonthlyFee,
       ISNULL(f.Amount, 0) AS AmountPaid,
       ISNULL(f.Status, 'Due') AS Status,
       f.PaymentDate
FROM dbo.Students s
LEFT JOIN dbo.Classes c ON s.ClassID = c.ClassID
LEFT JOIN dbo.Fees f
  ON f.StudentID = s.StudentID
  AND LTRIM(RTRIM(f.Month)) = @Month
  AND f.FeeType = 'Monthly Tuition'
ORDER BY c.ClassName, s.Name;";

            var rows = new List<FeeCollectionReportRow>();

            using (var connection = Database.GetConnection())
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@Month", month ?? (object)DBNull.Value);
                await connection.OpenAsync().ConfigureAwait(false);

                using (var reader = await command.ExecuteReaderAsync().ConfigureAwait(false))
                {
                    while (await reader.ReadAsync().ConfigureAwait(false))
                    {
                        rows.Add(new FeeCollectionReportRow
                        {
                            StudentName = reader["Name"] as string,
                            RegistrationNo = reader["RegistrationNo"] as string,
                            ClassName = reader["ClassName"] as string,
                            MonthlyFee = reader["MonthlyFee"] == DBNull.Value ? 0m : Convert.ToDecimal(reader["MonthlyFee"]),
                            AmountPaid = reader["AmountPaid"] == DBNull.Value ? 0m : Convert.ToDecimal(reader["AmountPaid"]),
                            Status = reader["Status"] as string,
                            PaymentDate = reader["PaymentDate"] == DBNull.Value
                                ? (DateTime?)null
                                : Convert.ToDateTime(reader["PaymentDate"])
                        });
                    }
                }
            }

            return rows;
        }

        public async Task<List<AttendanceSummaryRow>> GetAttendanceSummaryAsync(DateTime from, DateTime to, string personType)
        {
            string sql;

            if (personType == "Teacher")
            {
                sql = @"
SELECT t.Name,
       SUM(CASE WHEN a.Status = 'Present' THEN 1 ELSE 0 END) AS PresentDays,
       SUM(CASE WHEN a.Status = 'Absent' THEN 1 ELSE 0 END) AS AbsentDays,
       COUNT(DISTINCT a.[Date]) AS TotalDays
FROM dbo.Teachers t
LEFT JOIN dbo.Attendance a ON a.TeacherID = t.TeacherID
  AND a.[Date] BETWEEN @From AND @To
GROUP BY t.TeacherID, t.Name
ORDER BY t.Name;";
            }
            else if (personType == "Student")
            {
                sql = @"
SELECT s.Name,
       SUM(CASE WHEN a.Status = 'Present' THEN 1 ELSE 0 END) AS PresentDays,
       SUM(CASE WHEN a.Status = 'Absent' THEN 1 ELSE 0 END) AS AbsentDays,
       COUNT(DISTINCT a.[Date]) AS TotalDays
FROM dbo.Students s
LEFT JOIN dbo.Attendance a ON a.StudentID = s.StudentID
  AND a.[Date] BETWEEN @From AND @To
GROUP BY s.StudentID, s.Name
ORDER BY s.Name;";
            }
            else
            {
                throw new ArgumentException("personType must be 'Teacher' or 'Student'.", nameof(personType));
            }

            var rows = new List<AttendanceSummaryRow>();

            using (var connection = Database.GetConnection())
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@From", from.Date);
                command.Parameters.AddWithValue("@To", to.Date);
                await connection.OpenAsync().ConfigureAwait(false);

                using (var reader = await command.ExecuteReaderAsync().ConfigureAwait(false))
                {
                    while (await reader.ReadAsync().ConfigureAwait(false))
                    {
                        rows.Add(new AttendanceSummaryRow
                        {
                            Name = reader["Name"] as string,
                            PresentDays = reader["PresentDays"] == DBNull.Value ? 0 : Convert.ToInt32(reader["PresentDays"]),
                            AbsentDays = reader["AbsentDays"] == DBNull.Value ? 0 : Convert.ToInt32(reader["AbsentDays"]),
                            TotalDays = reader["TotalDays"] == DBNull.Value ? 0 : Convert.ToInt32(reader["TotalDays"])
                        });
                    }
                }
            }

            return rows;
        }
    }
}
