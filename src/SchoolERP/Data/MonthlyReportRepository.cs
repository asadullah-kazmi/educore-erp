using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;
using SchoolERP.Models;

namespace SchoolERP.Data
{
    public class MonthlyReportRepository
    {
        private static bool feePaymentColumnsEnsured;

        public async Task<MonthlyFinanceSummary> GetMonthlySummaryAsync(string month)
        {
            await EnsureFeePaymentColumnsAsync().ConfigureAwait(false);

            const string feesSql = @"
SELECT ISNULL(SUM(ISNULL(PaidAmount, CASE WHEN Status = 'Paid' THEN Amount ELSE 0 END)), 0)
FROM dbo.Fees
WHERE ISNULL(PaidAmount, CASE WHEN Status = 'Paid' THEN Amount ELSE 0 END) > 0
  AND FORMAT(PaymentDate, 'MMM yyyy') = @Month;";

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
            await EnsureFeePaymentColumnsAsync().ConfigureAwait(false);

            const string sql = @"
SELECT s.Name,
       s.RegistrationNo,
       c.ClassName,
       s.MonthlyFee,
       ISNULL(f.PaidAmount, CASE WHEN f.Status = 'Paid' THEN f.Amount ELSE 0 END) AS AmountPaid,
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

        private static async Task EnsureFeePaymentColumnsAsync()
        {
            if (feePaymentColumnsEnsured)
            {
                return;
            }

            const string sql = @"
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Fees') AND name = 'PaidAmount')
BEGIN
    ALTER TABLE dbo.Fees ADD PaidAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_Fees_PaidAmount DEFAULT 0;
    EXEC('UPDATE dbo.Fees SET PaidAmount = Amount WHERE Status = ''Paid''');
END
EXEC('UPDATE dbo.Fees
SET Status =
    CASE
        WHEN ISNULL(PaidAmount, 0) >= Amount THEN ''Paid''
        WHEN ISNULL(PaidAmount, 0) > 0 THEN ''Partial''
        ELSE ''Due''
    END');";

            using (var connection = Database.GetConnection())
            using (var command = new SqlCommand(sql, connection))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                await command.ExecuteNonQueryAsync().ConfigureAwait(false);
                feePaymentColumnsEnsured = true;
            }
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
