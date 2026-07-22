using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using SchoolERP.Models;
using SchoolERP.ViewModels;

namespace SchoolERP.Data
{
    public class FamilyReceiptRepository
    {
        private static bool schemaEnsured;

        public async Task EnsureSchemaAsync()
        {
            if (schemaEnsured) return;

            const string sql = @"
IF OBJECT_ID('dbo.FamilyFeeReceipts') IS NULL
BEGIN
    CREATE TABLE dbo.FamilyFeeReceipts (
        FamilyReceiptID INT IDENTITY(1,1) PRIMARY KEY,
        ReceiptNumber NVARCHAR(40) NOT NULL,
        GuardianCnic NVARCHAR(50) NOT NULL,
        PaymentDate DATETIME NOT NULL,
        TotalPaid DECIMAL(18,2) NOT NULL,
        TotalBalanceAfter DECIMAL(18,2) NOT NULL,
        CreatedOn DATETIME NOT NULL DEFAULT(GETDATE())
    );
    CREATE UNIQUE INDEX UX_FamilyFeeReceipts_Number ON dbo.FamilyFeeReceipts(ReceiptNumber);
    CREATE INDEX IX_FamilyFeeReceipts_Cnic ON dbo.FamilyFeeReceipts(GuardianCnic, PaymentDate DESC);
END;
IF OBJECT_ID('dbo.FamilyFeeReceiptItems') IS NULL
BEGIN
    CREATE TABLE dbo.FamilyFeeReceiptItems (
        FamilyReceiptItemID INT IDENTITY(1,1) PRIMARY KEY,
        FamilyReceiptID INT NOT NULL,
        StudentID INT NOT NULL,
        AmountPaid DECIMAL(18,2) NOT NULL,
        BalanceAfter DECIMAL(18,2) NOT NULL,
        CONSTRAINT FK_FamilyReceiptItems_Receipt FOREIGN KEY(FamilyReceiptID) REFERENCES dbo.FamilyFeeReceipts(FamilyReceiptID) ON DELETE CASCADE,
        CONSTRAINT FK_FamilyReceiptItems_Student FOREIGN KEY(StudentID) REFERENCES dbo.Students(StudentID) ON DELETE CASCADE
    );
END;

IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_FamilyReceiptItems_Student' AND delete_referential_action = 0)
BEGIN
    ALTER TABLE dbo.FamilyFeeReceiptItems DROP CONSTRAINT FK_FamilyReceiptItems_Student;
    ALTER TABLE dbo.FamilyFeeReceiptItems ADD CONSTRAINT FK_FamilyReceiptItems_Student FOREIGN KEY(StudentID) REFERENCES dbo.Students(StudentID) ON DELETE CASCADE;
END;";

            using (var connection = Database.GetConnection())
            using (var command = new SqlCommand(sql, connection))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                await command.ExecuteNonQueryAsync().ConfigureAwait(false);
                schemaEnsured = true;
            }
        }

        /// <summary>
        /// Find all siblings matching the given guardian CNIC.
        /// Results are sorted by ClassID ASC (youngest class first), then by name.
        /// </summary>
        public async Task<List<FamilyPaymentRowViewModel>> FindSiblingsAsync(string guardianCnic)
        {
            await EnsureSchemaAsync().ConfigureAwait(false);
            const string sql = @"
SELECT s.StudentID, s.Name, s.FatherName, s.RegistrationNo, s.ClassID, c.ClassName, s.Section,
       COALESCE(s.MonthlyFee, 0) AS MonthlyFee,
       ISNULL(SUM(CASE WHEN f.Amount > ISNULL(f.PaidAmount, 0) THEN f.Amount - ISNULL(f.PaidAmount, 0) ELSE 0 END), 0) AS Outstanding
FROM dbo.Students s
LEFT JOIN dbo.Classes c ON c.ClassID = s.ClassID
LEFT JOIN dbo.Fees f ON f.StudentID = s.StudentID
WHERE REPLACE(REPLACE(LTRIM(RTRIM(ISNULL(s.GuardianCnicNumber, ''))), '-', ''), ' ', '') = @GuardianCnic
GROUP BY s.StudentID, s.Name, s.FatherName, s.RegistrationNo, s.ClassID, c.ClassName, s.Section, s.MonthlyFee
ORDER BY s.ClassID ASC, s.Name ASC;";

            var rows = new List<FamilyPaymentRowViewModel>();
            using (var connection = Database.GetConnection())
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@GuardianCnic", NormalizeCnic(guardianCnic));
                await connection.OpenAsync().ConfigureAwait(false);
                using (var reader = await command.ExecuteReaderAsync().ConfigureAwait(false))
                {
                    while (await reader.ReadAsync().ConfigureAwait(false))
                    {
                        rows.Add(new FamilyPaymentRowViewModel
                        {
                            StudentID = Convert.ToInt32(reader["StudentID"]),
                            StudentName = reader["Name"] as string,
                            FatherName = reader["FatherName"] as string,
                            RegistrationNo = reader["RegistrationNo"] as string,
                            ClassID = reader["ClassID"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["ClassID"]),
                            ClassName = reader["ClassName"] as string,
                            Section = reader["Section"] as string,
                            MonthlyFee = Convert.ToDecimal(reader["MonthlyFee"]),
                            OutstandingBalance = Convert.ToDecimal(reader["Outstanding"])
                        });
                    }
                }
            }
            return rows;
        }

        /// <summary>
        /// Create a family receipt and also insert individual FeeReceipts for each sibling
        /// so their receipt history remains complete.
        /// </summary>
        public async Task<FamilyFeeReceipt> CreateAsync(string guardianCnic, DateTime paymentDate, IList<FamilyPaymentRowViewModel> rows)
        {
            await EnsureSchemaAsync().ConfigureAwait(false);
            await ReceiptRepository.EnsureSchemaAsync().ConfigureAwait(false);

            var receipt = new FamilyFeeReceipt
            {
                ReceiptNumber = "FAM-" + paymentDate.ToString("yyyyMMdd") + "-" + Guid.NewGuid().ToString("N").Substring(0, 6).ToUpperInvariant(),
                GuardianCnic = guardianCnic.Trim(),
                PaymentDate = paymentDate
            };

            using (var connection = Database.GetConnection())
            {
                await connection.OpenAsync().ConfigureAwait(false);
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // Apply payments for each sibling and build receipt items
                        foreach (var row in rows)
                        {
                            if (row.AllocatedAmount <= 0) continue;
                            var balanceAfter = await ApplyStudentPaymentAsync(connection, transaction, row.StudentID, row.AllocatedAmount, paymentDate).ConfigureAwait(false);
                            receipt.TotalPaid += row.AllocatedAmount;
                            receipt.TotalBalanceAfter += balanceAfter;
                            receipt.Items.Add(row.ToReceiptItem(balanceAfter));
                        }

                        if (receipt.Items.Count == 0) throw new InvalidOperationException("Enter a payment amount greater than zero.");

                        // Insert family receipt header
                        const string headerSql = @"
INSERT INTO dbo.FamilyFeeReceipts (ReceiptNumber, GuardianCnic, PaymentDate, TotalPaid, TotalBalanceAfter)
VALUES (@ReceiptNumber, @GuardianCnic, @PaymentDate, @TotalPaid, @TotalBalanceAfter);
SELECT CAST(SCOPE_IDENTITY() AS INT);";
                        using (var command = new SqlCommand(headerSql, connection, transaction))
                        {
                            command.Parameters.AddWithValue("@ReceiptNumber", receipt.ReceiptNumber);
                            command.Parameters.AddWithValue("@GuardianCnic", receipt.GuardianCnic);
                            command.Parameters.AddWithValue("@PaymentDate", receipt.PaymentDate);
                            command.Parameters.AddWithValue("@TotalPaid", receipt.TotalPaid);
                            command.Parameters.AddWithValue("@TotalBalanceAfter", receipt.TotalBalanceAfter);
                            receipt.FamilyReceiptID = Convert.ToInt32(await command.ExecuteScalarAsync().ConfigureAwait(false));
                        }

                        // Insert family receipt items
                        const string itemSql = @"INSERT INTO dbo.FamilyFeeReceiptItems (FamilyReceiptID, StudentID, AmountPaid, BalanceAfter) VALUES (@ReceiptID, @StudentID, @AmountPaid, @BalanceAfter);";
                        foreach (var item in receipt.Items)
                        {
                            using (var command = new SqlCommand(itemSql, connection, transaction))
                            {
                                command.Parameters.AddWithValue("@ReceiptID", receipt.FamilyReceiptID);
                                command.Parameters.AddWithValue("@StudentID", item.StudentID);
                                command.Parameters.AddWithValue("@AmountPaid", item.AmountPaid);
                                command.Parameters.AddWithValue("@BalanceAfter", item.BalanceAfter);
                                await command.ExecuteNonQueryAsync().ConfigureAwait(false);
                            }
                        }

                        // Create individual FeeReceipts for each sibling so their receipt history stays complete
                        const string individualReceiptSql = @"
INSERT INTO dbo.FeeReceipts (ReceiptNumber, StudentID, PaymentDate, AmountPaid, BalanceAfter, Details, CreatedOn)
VALUES (@ReceiptNumber, @StudentID, @PaymentDate, @AmountPaid, @BalanceAfter, @Details, GETDATE());";
                        foreach (var item in receipt.Items)
                        {
                            var individualReceiptNumber = "RCP-" + paymentDate.ToString("yyyyMMdd") + "-" + Guid.NewGuid().ToString("N").Substring(0, 6).ToUpperInvariant();
                            using (var command = new SqlCommand(individualReceiptSql, connection, transaction))
                            {
                                command.Parameters.AddWithValue("@ReceiptNumber", individualReceiptNumber);
                                command.Parameters.AddWithValue("@StudentID", item.StudentID);
                                command.Parameters.AddWithValue("@PaymentDate", paymentDate);
                                command.Parameters.AddWithValue("@AmountPaid", item.AmountPaid);
                                command.Parameters.AddWithValue("@BalanceAfter", item.BalanceAfter);
                                command.Parameters.AddWithValue("@Details", "Family Receipt " + receipt.ReceiptNumber);
                                await command.ExecuteNonQueryAsync().ConfigureAwait(false);
                            }
                        }

                        transaction.Commit();
                        return receipt;
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        public async Task<FamilyFeeReceipt> GetByIdAsync(int familyReceiptId)
        {
            await EnsureSchemaAsync().ConfigureAwait(false);
            const string headerSql = @"SELECT FamilyReceiptID, ReceiptNumber, GuardianCnic, PaymentDate, TotalPaid, TotalBalanceAfter FROM dbo.FamilyFeeReceipts WHERE FamilyReceiptID=@ReceiptID;";
            var receipt = new FamilyFeeReceipt();
            using (var connection = Database.GetConnection())
            {
                await connection.OpenAsync().ConfigureAwait(false);
                using (var command = new SqlCommand(headerSql, connection))
                {
                    command.Parameters.AddWithValue("@ReceiptID", familyReceiptId);
                    using (var reader = await command.ExecuteReaderAsync().ConfigureAwait(false))
                    {
                        if (!await reader.ReadAsync().ConfigureAwait(false)) return null;
                        receipt.FamilyReceiptID = Convert.ToInt32(reader["FamilyReceiptID"]);
                        receipt.ReceiptNumber = reader["ReceiptNumber"] as string;
                        receipt.GuardianCnic = reader["GuardianCnic"] as string;
                        receipt.PaymentDate = Convert.ToDateTime(reader["PaymentDate"]);
                        receipt.TotalPaid = Convert.ToDecimal(reader["TotalPaid"]);
                        receipt.TotalBalanceAfter = Convert.ToDecimal(reader["TotalBalanceAfter"]);
                    }
                }

                const string itemSql = @"
SELECT i.StudentID, s.Name AS StudentName, s.RegistrationNo, s.FatherName, c.ClassName, s.Section, i.AmountPaid, i.BalanceAfter
FROM dbo.FamilyFeeReceiptItems i
INNER JOIN dbo.Students s ON s.StudentID = i.StudentID
LEFT JOIN dbo.Classes c ON c.ClassID = s.ClassID
WHERE i.FamilyReceiptID=@ReceiptID ORDER BY s.ClassID ASC, s.Name;";
                using (var command = new SqlCommand(itemSql, connection))
                {
                    command.Parameters.AddWithValue("@ReceiptID", familyReceiptId);
                    using (var reader = await command.ExecuteReaderAsync().ConfigureAwait(false))
                    {
                        while (await reader.ReadAsync().ConfigureAwait(false))
                        {
                            receipt.Items.Add(new FamilyFeeReceiptItem
                            {
                                StudentID = Convert.ToInt32(reader["StudentID"]), StudentName = reader["StudentName"] as string,
                                FatherName = reader["FatherName"] == DBNull.Value ? string.Empty : reader["FatherName"] as string,
                                RegistrationNo = reader["RegistrationNo"] as string, ClassName = reader["ClassName"] as string,
                                Section = reader["Section"] as string, AmountPaid = Convert.ToDecimal(reader["AmountPaid"]),
                                BalanceAfter = Convert.ToDecimal(reader["BalanceAfter"])
                            });
                        }
                    }
                }
            }
            return receipt;
        }

        private static async Task<decimal> ApplyStudentPaymentAsync(SqlConnection connection, SqlTransaction transaction, int studentId, decimal payment, DateTime paymentDate)
        {
            var fees = new List<Tuple<int, decimal, decimal>>();
            const string selectSql = @"SELECT FeeID, Amount, ISNULL(PaidAmount, 0) PaidAmount FROM dbo.Fees WHERE StudentID=@StudentID AND Amount > ISNULL(PaidAmount,0) ORDER BY FeeID;";
            using (var command = new SqlCommand(selectSql, connection, transaction))
            {
                command.Parameters.AddWithValue("@StudentID", studentId);
                using (var reader = await command.ExecuteReaderAsync().ConfigureAwait(false))
                    while (await reader.ReadAsync().ConfigureAwait(false))
                        fees.Add(Tuple.Create(Convert.ToInt32(reader["FeeID"]), Convert.ToDecimal(reader["Amount"]), Convert.ToDecimal(reader["PaidAmount"])));
            }

            var available = 0m;
            foreach (var fee in fees) available += fee.Item2 - fee.Item3;
            if (payment > available) throw new InvalidOperationException("Payment cannot exceed the student's outstanding balance.");

            var remaining = payment;
            foreach (var fee in fees)
            {
                if (remaining <= 0) break;
                var applied = Math.Min(fee.Item2 - fee.Item3, remaining);
                var paid = fee.Item3 + applied;
                const string updateSql = @"UPDATE dbo.Fees SET PaidAmount=@Paid, Status=CASE WHEN @Paid >= Amount THEN 'Paid' ELSE 'Partial' END, PaymentDate=@PaymentDate WHERE FeeID=@FeeID;";
                using (var command = new SqlCommand(updateSql, connection, transaction))
                {
                    command.Parameters.AddWithValue("@Paid", paid);
                    command.Parameters.AddWithValue("@PaymentDate", paymentDate);
                    command.Parameters.AddWithValue("@FeeID", fee.Item1);
                    await command.ExecuteNonQueryAsync().ConfigureAwait(false);
                }
                remaining -= applied;
            }
            return available - payment;
        }

        private static string NormalizeCnic(string value) => (value ?? string.Empty).Replace("-", string.Empty).Replace(" ", string.Empty).Trim();
    }
}
