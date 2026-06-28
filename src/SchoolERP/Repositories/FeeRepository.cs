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
        private static bool feePaymentColumnsEnsured;

        private const string SelectBase = @"
SELECT f.FeeID,
       f.StudentID,
       s.Name AS StudentName,
       s.RegistrationNo,
       s.ClassID,
       c.ClassName,
       s.Section,
       f.Month,
       f.FeeType,
       f.Amount,
       ISNULL(f.PaidAmount, CASE WHEN f.Status = 'Paid' THEN f.Amount ELSE 0 END) AS PaidAmount,
       f.Status,
       f.PaymentDate
FROM dbo.Fees f
INNER JOIN dbo.Students s ON f.StudentID = s.StudentID
LEFT JOIN dbo.Classes c ON s.ClassID = c.ClassID";

        public async Task<List<FeeRecord>> GetAllFeesAsync(int? studentId = null, string month = null, string status = null)
        {
            await EnsureFeePaymentColumnsAsync().ConfigureAwait(false);

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
            await EnsureFeePaymentColumnsAsync().ConfigureAwait(false);

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
            await EnsureFeePaymentColumnsAsync().ConfigureAwait(false);

            if (fee == null)
            {
                throw new ArgumentNullException(nameof(fee));
            }

            const string sql = @"
INSERT INTO dbo.Fees (StudentID, Month, Amount, PaidAmount, Status, PaymentDate, FeeType)
VALUES (@StudentID, @Month, @Amount, @PaidAmount, @Status, @PaymentDate, @FeeType);";

            using (var connection = Database.GetConnection())
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@StudentID", fee.StudentID);
                command.Parameters.AddWithValue("@Month", fee.Month ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Amount", fee.Amount);
                command.Parameters.AddWithValue("@PaidAmount", GetPaidAmountForSave(fee.Amount, fee.PaidAmount, fee.Status));
                command.Parameters.AddWithValue("@Status", fee.Status ?? "Due");
                command.Parameters.AddWithValue("@PaymentDate", (object)fee.PaymentDate ?? DBNull.Value);
                command.Parameters.AddWithValue("@FeeType", (object)fee.FeeType ?? DBNull.Value);

                await connection.OpenAsync().ConfigureAwait(false);
                return await command.ExecuteNonQueryAsync().ConfigureAwait(false) > 0;
            }
        }

        public async Task<int> AddFeesForStudentsAsync(IEnumerable<int> studentIds, string month, string feeType, decimal amount, string status, DateTime? paymentDate)
        {
            await EnsureFeePaymentColumnsAsync().ConfigureAwait(false);

            if (studentIds == null)
            {
                throw new ArgumentNullException(nameof(studentIds));
            }
            if (string.IsNullOrWhiteSpace(month))
            {
                throw new ArgumentException("Month cannot be null or empty.", nameof(month));
            }
            if (string.IsNullOrWhiteSpace(feeType))
            {
                throw new ArgumentException("FeeType cannot be null or empty.", nameof(feeType));
            }

            var ids = new List<int>(studentIds);
            if (ids.Count == 0)
            {
                return 0;
            }

            const string sql = @"
IF NOT EXISTS (
    SELECT 1 FROM dbo.Fees
    WHERE StudentID = @StudentID
      AND LTRIM(RTRIM(Month)) = LTRIM(RTRIM(@Month))
      AND LTRIM(RTRIM(ISNULL(FeeType, ''))) = LTRIM(RTRIM(@FeeType))
)
BEGIN
    INSERT INTO dbo.Fees (StudentID, Month, Amount, PaidAmount, Status, PaymentDate, FeeType)
    VALUES (@StudentID, @Month, @Amount, @PaidAmount, @Status, @PaymentDate, @FeeType);
END";

            using (var connection = Database.GetConnection())
            {
                await connection.OpenAsync().ConfigureAwait(false);
                using (var transaction = connection.BeginTransaction())
                using (var command = new SqlCommand(sql, connection, transaction))
                {
                    command.Parameters.Add("@StudentID", System.Data.SqlDbType.Int);
                    command.Parameters.Add("@Month", System.Data.SqlDbType.NVarChar, 20).Value = month.Trim();
                    var amountParameter = command.Parameters.Add("@Amount", System.Data.SqlDbType.Decimal);
                    amountParameter.Precision = 18;
                    amountParameter.Scale = 2;
                    amountParameter.Value = amount;
                    command.Parameters.Add("@Status", System.Data.SqlDbType.NVarChar, 20).Value = string.IsNullOrWhiteSpace(status) ? "Due" : status.Trim();
                    command.Parameters.Add("@PaymentDate", System.Data.SqlDbType.DateTime);
                    command.Parameters.Add("@FeeType", System.Data.SqlDbType.NVarChar, 100).Value = feeType.Trim();
                    var paidAmountParameter = command.Parameters.Add("@PaidAmount", System.Data.SqlDbType.Decimal);
                    paidAmountParameter.Precision = 18;
                    paidAmountParameter.Scale = 2;
                    paidAmountParameter.Value = string.Equals(status, "Paid", StringComparison.OrdinalIgnoreCase) ? amount : 0m;

                    try
                    {
                        var inserted = 0;
                        foreach (var studentId in ids)
                        {
                            command.Parameters["@StudentID"].Value = studentId;
                            command.Parameters["@PaymentDate"].Value = (object)paymentDate ?? DBNull.Value;
                            inserted += await command.ExecuteNonQueryAsync().ConfigureAwait(false);
                        }

                        transaction.Commit();
                        return inserted;
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        public async Task<bool> UpdateFeeAsync(FeeRecord fee)
        {
            await EnsureFeePaymentColumnsAsync().ConfigureAwait(false);

            if (fee == null)
            {
                throw new ArgumentNullException(nameof(fee));
            }

            const string sql = @"
UPDATE dbo.Fees
SET StudentID = @StudentID,
    Month = @Month,
    Amount = @Amount,
    PaidAmount = @PaidAmount,
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
                command.Parameters.AddWithValue("@PaidAmount", GetPaidAmountForSave(fee.Amount, fee.PaidAmount, fee.Status));
                command.Parameters.AddWithValue("@Status", fee.Status ?? "Due");
                command.Parameters.AddWithValue("@PaymentDate", (object)fee.PaymentDate ?? DBNull.Value);
                command.Parameters.AddWithValue("@FeeType", (object)fee.FeeType ?? DBNull.Value);

                await connection.OpenAsync().ConfigureAwait(false);
                return await command.ExecuteNonQueryAsync().ConfigureAwait(false) > 0;
            }
        }

        public async Task<bool> DeleteFeeAsync(int feeId)
        {
            await EnsureFeePaymentColumnsAsync().ConfigureAwait(false);

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
            await EnsureFeePaymentColumnsAsync().ConfigureAwait(false);

            const string sql = @"
UPDATE dbo.Fees
SET Status = 'Paid',
    PaidAmount = Amount,
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
            await EnsureFeePaymentColumnsAsync().ConfigureAwait(false);

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

        public async Task<List<string>> GetFeeTypesAsync()
        {
            await EnsureFeePaymentColumnsAsync().ConfigureAwait(false);

            const string sql = @"
SELECT DISTINCT LTRIM(RTRIM(FeeType)) AS FeeType
FROM dbo.Fees
WHERE FeeType IS NOT NULL
  AND LTRIM(RTRIM(FeeType)) <> ''
ORDER BY FeeType;";

            using (var connection = Database.GetConnection())
            using (var command = new SqlCommand(sql, connection))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                var feeTypes = new List<string>();

                using (var reader = await command.ExecuteReaderAsync().ConfigureAwait(false))
                {
                    while (await reader.ReadAsync().ConfigureAwait(false))
                    {
                        feeTypes.Add(reader["FeeType"] as string);
                    }
                }

                return feeTypes;
            }
        }

        public async Task<bool> ApplyPaymentAsync(IList<FeeRecord> fees, decimal paymentAmount, DateTime paymentDate)
        {
            await EnsureFeePaymentColumnsAsync().ConfigureAwait(false);

            if (fees == null)
            {
                throw new ArgumentNullException(nameof(fees));
            }
            if (paymentAmount <= 0)
            {
                throw new ArgumentException("Payment amount must be greater than 0.", nameof(paymentAmount));
            }

            const string sql = @"
UPDATE dbo.Fees
SET PaidAmount = @PaidAmount,
    Status = @Status,
    PaymentDate = @PaymentDate
WHERE FeeID = @FeeID;";

            using (var connection = Database.GetConnection())
            {
                await connection.OpenAsync().ConfigureAwait(false);
                using (var transaction = connection.BeginTransaction())
                using (var command = new SqlCommand(sql, connection, transaction))
                {
                    command.Parameters.Add("@FeeID", System.Data.SqlDbType.Int);
                    var paidAmountParameter = command.Parameters.Add("@PaidAmount", System.Data.SqlDbType.Decimal);
                    paidAmountParameter.Precision = 18;
                    paidAmountParameter.Scale = 2;
                    command.Parameters.Add("@Status", System.Data.SqlDbType.NVarChar, 20);
                    command.Parameters.Add("@PaymentDate", System.Data.SqlDbType.DateTime).Value = paymentDate;

                    try
                    {
                        var remainingPayment = paymentAmount;
                        var updated = 0;

                        foreach (var fee in fees)
                        {
                            if (remainingPayment <= 0)
                            {
                                break;
                            }

                            var balance = Math.Max(fee.Amount - fee.PaidAmount, 0);
                            if (balance <= 0)
                            {
                                continue;
                            }

                            var amountToApply = Math.Min(balance, remainingPayment);
                            var newPaidAmount = fee.PaidAmount + amountToApply;
                            var newStatus = GetStatusFromPayment(fee.Amount, newPaidAmount);

                            command.Parameters["@FeeID"].Value = fee.FeeID;
                            command.Parameters["@PaidAmount"].Value = newPaidAmount;
                            command.Parameters["@Status"].Value = newStatus;

                            updated += await command.ExecuteNonQueryAsync().ConfigureAwait(false);
                            remainingPayment -= amountToApply;
                        }

                        transaction.Commit();
                        return updated > 0;
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        public async Task<List<FeeRecord>> GetMonthlyTuitionStatusAsync(string month, int? classId = null, string section = null)
        {
            await EnsureFeePaymentColumnsAsync().ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(month))
            {
                throw new ArgumentException("Month cannot be null or empty.", nameof(month));
            }

            var sql = @"
SELECT ISNULL(f.FeeID, 0) AS FeeID,
       s.StudentID,
       s.Name AS StudentName,
       s.RegistrationNo,
       s.ClassID,
       c.ClassName,
       s.Section,
       @Month AS Month,
       ISNULL(f.FeeType, 'Monthly Tuition') AS FeeType,
       ISNULL(f.Amount, COALESCE(s.MonthlyFee, 0)) AS Amount,
       ISNULL(f.PaidAmount, CASE WHEN f.Status = 'Paid' THEN f.Amount ELSE 0 END) AS PaidAmount,
       ISNULL(f.Status, 'Due') AS Status,
       f.PaymentDate
FROM dbo.Students s
LEFT JOIN dbo.Classes c ON s.ClassID = c.ClassID
LEFT JOIN dbo.Fees f
  ON f.StudentID = s.StudentID
 AND LTRIM(RTRIM(f.Month)) = LTRIM(RTRIM(@Month))
 AND LTRIM(RTRIM(ISNULL(f.FeeType, 'Monthly Tuition'))) = 'Monthly Tuition'
WHERE (@ClassID IS NULL OR s.ClassID = @ClassID)
  AND (@Section IS NULL OR LTRIM(RTRIM(ISNULL(s.Section, ''))) = @Section)
ORDER BY c.ClassName, s.Section, s.Name;";

            using (var connection = Database.GetConnection())
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@Month", month.Trim());
                command.Parameters.AddWithValue("@ClassID", (object)classId ?? DBNull.Value);
                command.Parameters.AddWithValue("@Section", string.IsNullOrWhiteSpace(section) ? (object)DBNull.Value : section.Trim());

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
            await EnsureFeePaymentColumnsAsync().ConfigureAwait(false);

            if (string.IsNullOrEmpty(month))
            {
                throw new ArgumentException("Month cannot be null or empty.", nameof(month));
            }
            if (string.IsNullOrEmpty(feeType))
            {
                throw new ArgumentException("FeeType cannot be null or empty.", nameof(feeType));
            }

            const string sql = @"
INSERT INTO dbo.Fees (StudentID, Month, FeeType, Amount, PaidAmount, Status, PaymentDate)
SELECT s.StudentID, @Month, @FeeType, COALESCE(s.MonthlyFee, 0), 0, 'Due', NULL
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
            await EnsureFeePaymentColumnsAsync().ConfigureAwait(false);

            const string sql = @"
SELECT ISNULL(SUM(ISNULL(PaidAmount, CASE WHEN Status = 'Paid' THEN Amount ELSE 0 END)), 0)
FROM dbo.Fees
WHERE ISNULL(PaidAmount, CASE WHEN Status = 'Paid' THEN Amount ELSE 0 END) > 0
  AND LTRIM(RTRIM(Month)) = LTRIM(RTRIM(@Month));";

            using (var connection = Database.GetConnection())
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@Month", month ?? (object)DBNull.Value);

                await connection.OpenAsync().ConfigureAwait(false);
                var result = await command.ExecuteScalarAsync().ConfigureAwait(false);
                return result == DBNull.Value ? 0 : Convert.ToDecimal(result);
            }
        }

        public async Task<decimal> GetTotalOutstandingAsync()
        {
            await EnsureFeePaymentColumnsAsync().ConfigureAwait(false);

            const string sql = @"
SELECT ISNULL(SUM(
    CASE
        WHEN Amount - ISNULL(PaidAmount, CASE WHEN Status = 'Paid' THEN Amount ELSE 0 END) > 0
        THEN Amount - ISNULL(PaidAmount, CASE WHEN Status = 'Paid' THEN Amount ELSE 0 END)
        ELSE 0
    END), 0)
FROM dbo.Fees;";

            using (var connection = Database.GetConnection())
            using (var command = new SqlCommand(sql, connection))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                var result = await command.ExecuteScalarAsync().ConfigureAwait(false);
                return result == DBNull.Value ? 0 : Convert.ToDecimal(result);
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
                ClassID = reader["ClassID"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["ClassID"]),
                ClassName = reader["ClassName"] as string,
                Section = reader["Section"] as string,
                Month = reader["Month"] as string,
                FeeType = reader["FeeType"] as string,
                Amount = reader.GetDecimal(reader.GetOrdinal("Amount")),
                PaidAmount = reader["PaidAmount"] == DBNull.Value ? 0m : Convert.ToDecimal(reader["PaidAmount"]),
                Status = reader["Status"] as string,
                PaymentDate = reader.IsDBNull(reader.GetOrdinal("PaymentDate"))
                    ? (DateTime?)null
                    : reader.GetDateTime(reader.GetOrdinal("PaymentDate"))
            };
        }

        private static decimal GetPaidAmountForSave(decimal amount, decimal paidAmount, string status)
        {
            if (string.Equals(status, "Paid", StringComparison.OrdinalIgnoreCase))
            {
                return amount;
            }

            return Math.Max(Math.Min(paidAmount, amount), 0);
        }

        private static string GetStatusFromPayment(decimal amount, decimal paidAmount)
        {
            if (paidAmount >= amount)
            {
                return "Paid";
            }

            return paidAmount > 0 ? "Partial" : "Due";
        }

        private static async Task EnsureFeePaymentColumnsAsync()
        {
            if (feePaymentColumnsEnsured)
            {
                return;
            }

            const string sql = @"
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Fees') AND name = 'FeeType')
    ALTER TABLE dbo.Fees ADD FeeType NVARCHAR(100) NULL;
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
    }
}
