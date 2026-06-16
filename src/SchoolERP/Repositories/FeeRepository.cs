using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;
using SchoolERP.Models;
using SchoolERP.Data;

namespace SchoolERP.Repositories
{
    public class FeeRepository
    {
        private const string SelectBase = @"
SELECT f.FeeID,
       f.StudentID,
       s.Name AS StudentName,
       s.RegistrationNo,
       c.ClassName,
       f.Month,
       f.FeeType,
       f.Amount,
       f.Status,
       f.PaymentDate
FROM dbo.Fees f
INNER JOIN dbo.Students s ON f.StudentID = s.StudentID
LEFT JOIN dbo.Classes c ON s.ClassID = c.ClassID";

        public async Task<List<FeeRecord>> GetAllFeesAsync(int? studentId = null, string month = null, string status = null)
        {
            var sql = SelectBase;
            var conditions = new List<string>();

            if (studentId.HasValue)
            {
                conditions.Add("f.StudentID = @StudentID");
            }
            if (!string.IsNullOrEmpty(month))
            {
                conditions.Add("f.Month = @Month");
            }
            if (!string.IsNullOrEmpty(status))
            {
                conditions.Add("f.Status = @Status");
            }

            if (conditions.Count > 0)
            {
                sql += " WHERE " + string.Join(" AND ", conditions);
            }

            sql += " ORDER BY f.FeeID DESC;";

            using (var connection = Database.GetConnection())
            using (var command = new SqlCommand(sql, connection))
            {
                if (studentId.HasValue)
                {
                    command.Parameters.AddWithValue("@StudentID", studentId.Value);
                }
                if (!string.IsNullOrEmpty(month))
                {
                    command.Parameters.AddWithValue("@Month", month);
                }
                if (!string.IsNullOrEmpty(status))
                {
                    command.Parameters.AddWithValue("@Status", status);
                }

                await connection.OpenAsync().ConfigureAwait(false);
                var fees = new List<FeeRecord>();

                using (var reader = await command.ExecuteReaderAsync().ConfigureAwait(false))
                {
                    while (await reader.ReadAsync().ConfigureAwait(false))
                    {
                        fees.Add(MapFeeRecord(reader));
                    }
                }

                return fees;
            }
        }

        public async Task<FeeRecord> GetFeeByIdAsync(int feeId)
        {
            var sql = SelectBase + " WHERE f.FeeID = @FeeID;";

            using (var connection = Database.GetConnection())
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@FeeID", feeId);
                await connection.OpenAsync().ConfigureAwait(false);

                using (var reader = await command.ExecuteReaderAsync().ConfigureAwait(false))
                {
                    if (await reader.ReadAsync().ConfigureAwait(false))
                    {
                        return MapFeeRecord(reader);
                    }
                }
            }

            return null;
        }

        public async Task<bool> AddFeeAsync(FeeRecord fee)
        {
            if (fee == null)
            {
                throw new ArgumentNullException(nameof(fee));
            }

            const string sql = @"
INSERT INTO dbo.Fees (StudentID, Month, Amount, Status, PaymentDate, FeeType)
VALUES (@StudentID, @Month, @Amount, @Status, @PaymentDate, @FeeType);";

            using (var connection = Database.GetConnection())
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@StudentID", fee.StudentID);
                command.Parameters.AddWithValue("@Month", fee.Month ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Amount", fee.Amount);
                command.Parameters.AddWithValue("@Status", fee.Status ?? "Due");
                command.Parameters.AddWithValue("@PaymentDate", (object)fee.PaymentDate ?? DBNull.Value);
                command.Parameters.AddWithValue("@FeeType", (object)fee.FeeType ?? DBNull.Value);

                await connection.OpenAsync().ConfigureAwait(false);
                return await command.ExecuteNonQueryAsync().ConfigureAwait(false) > 0;
            }
        }

        public async Task<bool> UpdateFeeAsync(FeeRecord fee)
        {
            if (fee == null)
            {
                throw new ArgumentNullException(nameof(fee));
            }

            const string sql = @"
UPDATE dbo.Fees
SET StudentID = @StudentID,
    Month = @Month,
    Amount = @Amount,
    Status = @Status,
    PaymentDate = @PaymentDate,
    FeeType = @FeeType
WHERE FeeID = @FeeID;";

            using (var connection = Database.GetConnection())
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@FeeID", fee.FeeID);
                command.Parameters.AddWithValue("@StudentID", fee.StudentID);
                command.Parameters.AddWithValue("@Month", fee.Month ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Amount", fee.Amount);
                command.Parameters.AddWithValue("@Status", fee.Status ?? "Due");
                command.Parameters.AddWithValue("@PaymentDate", (object)fee.PaymentDate ?? DBNull.Value);
                command.Parameters.AddWithValue("@FeeType", (object)fee.FeeType ?? DBNull.Value);

                await connection.OpenAsync().ConfigureAwait(false);
                return await command.ExecuteNonQueryAsync().ConfigureAwait(false) > 0;
            }
        }

        public async Task<bool> DeleteFeeAsync(int feeId)
        {
            const string sql = "DELETE FROM dbo.Fees WHERE FeeID = @FeeID;";

            using (var connection = Database.GetConnection())
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@FeeID", feeId);

                await connection.OpenAsync().ConfigureAwait(false);
                return await command.ExecuteNonQueryAsync().ConfigureAwait(false) > 0;
            }
        }

        public async Task<bool> MarkAsPaidAsync(int feeId, DateTime paymentDate)
        {
            const string sql = @"
UPDATE dbo.Fees
SET Status = 'Paid',
    PaymentDate = @PaymentDate
WHERE FeeID = @FeeID;";

            using (var connection = Database.GetConnection())
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@FeeID", feeId);
                command.Parameters.AddWithValue("@PaymentDate", paymentDate);

                await connection.OpenAsync().ConfigureAwait(false);
                return await command.ExecuteNonQueryAsync().ConfigureAwait(false) > 0;
            }
        }

        public async Task<List<FeeRecord>> GetFeesByStudentAsync(int studentId)
        {
            var sql = SelectBase + @"
WHERE f.StudentID = @StudentID
ORDER BY f.Month DESC;";

            using (var connection = Database.GetConnection())
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@StudentID", studentId);

                await connection.OpenAsync().ConfigureAwait(false);
                var fees = new List<FeeRecord>();

                using (var reader = await command.ExecuteReaderAsync().ConfigureAwait(false))
                {
                    while (await reader.ReadAsync().ConfigureAwait(false))
                    {
                        fees.Add(MapFeeRecord(reader));
                    }
                }

                return fees;
            }
        }

        public async Task<bool> GenerateMonthlyFeesAsync(string month, string feeType)
        {
            if (string.IsNullOrEmpty(month))
            {
                throw new ArgumentException("Month cannot be null or empty.", nameof(month));
            }
            if (string.IsNullOrEmpty(feeType))
            {
                throw new ArgumentException("FeeType cannot be null or empty.", nameof(feeType));
            }

            const string sql = @"
INSERT INTO dbo.Fees (StudentID, Month, FeeType, Amount, Status, PaymentDate)
SELECT s.StudentID, @Month, @FeeType, COALESCE(c.MonthlyFee, 0), 'Due', NULL
FROM dbo.Students s
LEFT JOIN dbo.Classes c ON s.ClassID = c.ClassID
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.Fees f
    WHERE f.StudentID = s.StudentID
      AND f.Month = @Month
      AND f.FeeType = @FeeType
);";

            using (var connection = Database.GetConnection())
            {
                await connection.OpenAsync().ConfigureAwait(false);
                using (var transaction = connection.BeginTransaction())
                using (var command = new SqlCommand(sql, connection, transaction))
                {
                    command.Parameters.AddWithValue("@Month", month);
                    command.Parameters.AddWithValue("@FeeType", feeType);

                    try
                    {
                        int rowsInserted = await command.ExecuteNonQueryAsync().ConfigureAwait(false);
                        transaction.Commit();
                        return rowsInserted > 0;
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        public async Task<decimal> GetTotalCollectedAsync(string month)
        {
            const string sql = @"
SELECT COALESCE(SUM(Amount), 0)
FROM dbo.Fees
WHERE Status = 'Paid'
  AND Month = @Month;";

            using (var connection = Database.GetConnection())
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@Month", month ?? (object)DBNull.Value);

                await connection.OpenAsync().ConfigureAwait(false);
                var result = await command.ExecuteScalarAsync().ConfigureAwait(false);
                return Convert.ToDecimal(result ?? 0);
            }
        }

        public async Task<decimal> GetTotalOutstandingAsync()
        {
            const string sql = @"
SELECT COALESCE(SUM(Amount), 0)
FROM dbo.Fees
WHERE Status = 'Due';";

            using (var connection = Database.GetConnection())
            using (var command = new SqlCommand(sql, connection))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                var result = await command.ExecuteScalarAsync().ConfigureAwait(false);
                return Convert.ToDecimal(result ?? 0);
            }
        }

        private static FeeRecord MapFeeRecord(SqlDataReader reader)
        {
            return new FeeRecord
            {
                FeeID = reader.GetInt32(reader.GetOrdinal("FeeID")),
                StudentID = reader.GetInt32(reader.GetOrdinal("StudentID")),
                StudentName = reader["StudentName"] as string,
                RegistrationNo = reader["RegistrationNo"] as string,
                ClassName = reader["ClassName"] as string,
                Month = reader["Month"] as string,
                FeeType = reader["FeeType"] as string,
                Amount = reader.GetDecimal(reader.GetOrdinal("Amount")),
                Status = reader["Status"] as string,
                PaymentDate = reader.IsDBNull(reader.GetOrdinal("PaymentDate")) 
                    ? (DateTime?)null 
                    : reader.GetDateTime(reader.GetOrdinal("PaymentDate"))
            };
        }
    }
}
