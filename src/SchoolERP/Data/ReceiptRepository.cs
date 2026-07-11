using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;
using SchoolERP.Models;

namespace SchoolERP.Data
{
    public class ReceiptRepository
    {
        private static bool schemaEnsured;

        public static async Task EnsureSchemaAsync()
        {
            if (schemaEnsured)
            {
                return;
            }

            const string sql = @"
IF OBJECT_ID('dbo.FeeReceipts') IS NULL
BEGIN
    CREATE TABLE dbo.FeeReceipts (
        ReceiptID INT IDENTITY(1,1) PRIMARY KEY,
        ReceiptNumber NVARCHAR(40) NOT NULL,
        StudentID INT NOT NULL,
        PaymentDate DATETIME NOT NULL,
        AmountPaid DECIMAL(18,2) NOT NULL,
        BalanceAfter DECIMAL(18,2) NOT NULL DEFAULT 0,
        Details NVARCHAR(1000) NULL,
        CreatedOn DATETIME NOT NULL DEFAULT(GETDATE()),
        CONSTRAINT FK_FeeReceipts_Students FOREIGN KEY(StudentID) REFERENCES dbo.Students(StudentID) ON DELETE CASCADE
    );
END;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_FeeReceipts_ReceiptNumber' AND object_id = OBJECT_ID('dbo.FeeReceipts'))
    CREATE UNIQUE INDEX UX_FeeReceipts_ReceiptNumber ON dbo.FeeReceipts(ReceiptNumber);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_FeeReceipts_Student_PaymentDate' AND object_id = OBJECT_ID('dbo.FeeReceipts'))
    CREATE INDEX IX_FeeReceipts_Student_PaymentDate ON dbo.FeeReceipts(StudentID, PaymentDate DESC);";

            using (var connection = Database.GetConnection())
            using (var command = new SqlCommand(sql, connection))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                await command.ExecuteNonQueryAsync().ConfigureAwait(false);
                schemaEnsured = true;
            }
        }

        public async Task<List<FeeReceipt>> GetAllAsync()
        {
            await EnsureSchemaAsync().ConfigureAwait(false);

            const string sql = @"
SELECT r.ReceiptID, r.ReceiptNumber, r.StudentID, r.PaymentDate,
       r.AmountPaid, r.BalanceAfter, r.Details, r.CreatedOn,
       s.Name AS StudentName, s.RegistrationNo, c.ClassName, s.Section
FROM dbo.FeeReceipts r
INNER JOIN dbo.Students s ON s.StudentID = r.StudentID
LEFT JOIN dbo.Classes c ON c.ClassID = s.ClassID
ORDER BY r.PaymentDate DESC, r.ReceiptID DESC;";

            var receipts = new List<FeeReceipt>();
            using (var connection = Database.GetConnection())
            using (var command = new SqlCommand(sql, connection))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                using (var reader = await command.ExecuteReaderAsync().ConfigureAwait(false))
                {
                    while (await reader.ReadAsync().ConfigureAwait(false))
                    {
                        receipts.Add(Map(reader));
                    }
                }
            }

            return receipts;
        }

        public async Task<FeeReceipt> GetLatestForStudentAsync(int studentId)
        {
            await EnsureSchemaAsync().ConfigureAwait(false);

            const string sql = @"
SELECT TOP 1 r.ReceiptID, r.ReceiptNumber, r.StudentID, r.PaymentDate,
       r.AmountPaid, r.BalanceAfter, r.Details, r.CreatedOn,
       s.Name AS StudentName, s.RegistrationNo, c.ClassName, s.Section
FROM dbo.FeeReceipts r
INNER JOIN dbo.Students s ON s.StudentID = r.StudentID
LEFT JOIN dbo.Classes c ON c.ClassID = s.ClassID
WHERE r.StudentID = @StudentID
ORDER BY r.PaymentDate DESC, r.ReceiptID DESC;";

            using (var connection = Database.GetConnection())
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@StudentID", studentId);
                await connection.OpenAsync().ConfigureAwait(false);
                using (var reader = await command.ExecuteReaderAsync().ConfigureAwait(false))
                {
                    return await reader.ReadAsync().ConfigureAwait(false) ? Map(reader) : null;
                }
            }
        }

        private static FeeReceipt Map(SqlDataReader reader)
        {
            return new FeeReceipt
            {
                ReceiptID = Convert.ToInt32(reader["ReceiptID"]),
                ReceiptNumber = reader["ReceiptNumber"] as string,
                StudentID = Convert.ToInt32(reader["StudentID"]),
                StudentName = reader["StudentName"] as string,
                RegistrationNo = reader["RegistrationNo"] as string,
                ClassName = reader["ClassName"] as string,
                Section = reader["Section"] as string,
                PaymentDate = Convert.ToDateTime(reader["PaymentDate"]),
                AmountPaid = Convert.ToDecimal(reader["AmountPaid"]),
                BalanceAfter = Convert.ToDecimal(reader["BalanceAfter"]),
                Details = reader["Details"] == DBNull.Value ? string.Empty : reader["Details"] as string,
                CreatedOn = Convert.ToDateTime(reader["CreatedOn"])
            };
        }
    }
}
