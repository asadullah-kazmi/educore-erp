using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;
using SchoolERP.Models;

namespace SchoolERP.Data
{
    public class SalaryRepository
    {
        private const string SelectBase = @"
SELECT sp.SalaryPaymentID,
       sp.TeacherID,
       sp.Amount,
       sp.PaymentDate,
       sp.Notes,
       t.Name AS TeacherName,
       t.Designation,
       t.Salary AS BaseSalary
FROM dbo.SalaryPayments sp
INNER JOIN dbo.Teachers t ON t.TeacherID = sp.TeacherID";

        public async Task<List<SalaryPayment>> GetAllPaymentsAsync(string month = null, int? teacherId = null)
        {
            var sql = SelectBase;
            var conditions = new List<string>();

            if (!string.IsNullOrEmpty(month))
            {
                conditions.Add("FORMAT(sp.PaymentDate, 'MMM yyyy') = @Month");
            }

            if (teacherId.HasValue)
            {
                conditions.Add("sp.TeacherID = @TeacherID");
            }

            if (conditions.Count > 0)
            {
                sql += " WHERE " + string.Join(" AND ", conditions);
            }

            sql += " ORDER BY sp.PaymentDate DESC;";

            var payments = new List<SalaryPayment>();

            using (var connection = Database.GetConnection())
            using (var command = new SqlCommand(sql, connection))
            {
                if (!string.IsNullOrEmpty(month))
                {
                    command.Parameters.AddWithValue("@Month", month);
                }

                if (teacherId.HasValue)
                {
                    command.Parameters.AddWithValue("@TeacherID", teacherId.Value);
                }

                await connection.OpenAsync().ConfigureAwait(false);

                using (var reader = await command.ExecuteReaderAsync().ConfigureAwait(false))
                {
                    while (await reader.ReadAsync().ConfigureAwait(false))
                    {
                        payments.Add(MapSalaryPayment(reader));
                    }
                }
            }

            return payments;
        }

        public async Task<List<SalaryPayment>> GetPaymentHistoryAsync(int teacherId)
        {
            var sql = SelectBase + @"
WHERE sp.TeacherID = @TeacherID
ORDER BY sp.PaymentDate DESC;";

            var payments = new List<SalaryPayment>();

            using (var connection = Database.GetConnection())
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@TeacherID", teacherId);
                await connection.OpenAsync().ConfigureAwait(false);

                using (var reader = await command.ExecuteReaderAsync().ConfigureAwait(false))
                {
                    while (await reader.ReadAsync().ConfigureAwait(false))
                    {
                        payments.Add(MapSalaryPayment(reader));
                    }
                }
            }

            return payments;
        }

        public async Task<bool> PaySalaryAsync(int teacherId, decimal amount, DateTime paymentDate, string notes)
        {
            const string verifySql = @"
SELECT COUNT(1)
FROM dbo.Teachers
WHERE TeacherID = @TeacherID;";

            const string insertSql = @"
INSERT INTO dbo.SalaryPayments (TeacherID, Amount, PaymentDate, Notes)
VALUES (@TeacherID, @Amount, @PaymentDate, @Notes);";

            using (var connection = Database.GetConnection())
            {
                await connection.OpenAsync().ConfigureAwait(false);
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        int teacherCount;
                        using (var verifyCommand = new SqlCommand(verifySql, connection, transaction))
                        {
                            verifyCommand.Parameters.AddWithValue("@TeacherID", teacherId);
                            teacherCount = Convert.ToInt32(await verifyCommand.ExecuteScalarAsync().ConfigureAwait(false));
                        }

                        if (teacherCount == 0)
                        {
                            transaction.Rollback();
                            return false;
                        }

                        int rowsAffected;
                        using (var insertCommand = new SqlCommand(insertSql, connection, transaction))
                        {
                            insertCommand.Parameters.AddWithValue("@TeacherID", teacherId);
                            insertCommand.Parameters.AddWithValue("@Amount", amount);
                            insertCommand.Parameters.AddWithValue("@PaymentDate", paymentDate);
                            insertCommand.Parameters.AddWithValue("@Notes", (object)notes ?? DBNull.Value);
                            rowsAffected = await insertCommand.ExecuteNonQueryAsync().ConfigureAwait(false);
                        }

                        transaction.Commit();
                        return rowsAffected > 0;
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        public async Task<bool> HasPaidThisMonthAsync(int teacherId, string month)
        {
            const string sql = @"
SELECT COUNT(1)
FROM dbo.SalaryPayments
WHERE TeacherID = @TeacherID
  AND FORMAT(PaymentDate, 'MMM yyyy') = @Month;";

            using (var connection = Database.GetConnection())
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@TeacherID", teacherId);
                command.Parameters.AddWithValue("@Month", month ?? (object)DBNull.Value);
                await connection.OpenAsync().ConfigureAwait(false);
                var count = Convert.ToInt32(await command.ExecuteScalarAsync().ConfigureAwait(false));
                return count > 0;
            }
        }

        public async Task<decimal> GetTotalSalariesPaidAsync(string month)
        {
            const string sql = @"
SELECT ISNULL(SUM(Amount), 0)
FROM dbo.SalaryPayments
WHERE FORMAT(PaymentDate, 'MMM yyyy') = @Month;";

            using (var connection = Database.GetConnection())
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@Month", month ?? (object)DBNull.Value);
                await connection.OpenAsync().ConfigureAwait(false);
                var result = await command.ExecuteScalarAsync().ConfigureAwait(false);
                return result == null || result == DBNull.Value ? 0m : Convert.ToDecimal(result);
            }
        }

        public async Task<bool> DeletePaymentAsync(int salaryPaymentId)
        {
            const string sql = "DELETE FROM dbo.SalaryPayments WHERE SalaryPaymentID = @SalaryPaymentID;";

            using (var connection = Database.GetConnection())
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@SalaryPaymentID", salaryPaymentId);
                await connection.OpenAsync().ConfigureAwait(false);
                return await command.ExecuteNonQueryAsync().ConfigureAwait(false) > 0;
            }
        }

        private static SalaryPayment MapSalaryPayment(SqlDataReader reader)
        {
            return new SalaryPayment
            {
                SalaryPaymentID = reader.GetInt32(reader.GetOrdinal("SalaryPaymentID")),
                TeacherID = reader.GetInt32(reader.GetOrdinal("TeacherID")),
                TeacherName = reader["TeacherName"] as string,
                Designation = reader["Designation"] as string,
                BaseSalary = reader["BaseSalary"] == DBNull.Value ? 0m : Convert.ToDecimal(reader["BaseSalary"]),
                Amount = reader["Amount"] == DBNull.Value ? 0m : Convert.ToDecimal(reader["Amount"]),
                PaymentDate = reader.GetDateTime(reader.GetOrdinal("PaymentDate")),
                Notes = reader["Notes"] as string
            };
        }
    }
}
