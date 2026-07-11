using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using SchoolERP.Models;

namespace SchoolERP.Data
{
    public class ExamSlipRepository
    {
        private static readonly Random Random = new Random();
        private static bool schemaEnsured;

        public async Task EnsureSchemaAsync()
        {
            if (schemaEnsured)
            {
                return;
            }

            const string sql = @"
IF OBJECT_ID('dbo.ExamSlips') IS NULL
BEGIN
    CREATE TABLE dbo.ExamSlips (
        ExamSlipID INT IDENTITY(1,1) PRIMARY KEY,
        StudentID INT NOT NULL,
        TermName NVARCHAR(100) NOT NULL,
        FeeMonth NVARCHAR(20) NOT NULL,
        ExamNumber NVARCHAR(20) NOT NULL,
        GeneratedOn DATETIME NOT NULL DEFAULT(GETDATE()),
        CONSTRAINT FK_ExamSlips_Students FOREIGN KEY(StudentID) REFERENCES dbo.Students(StudentID) ON DELETE CASCADE
    );
END;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_ExamSlips_Student_Term_Month' AND object_id = OBJECT_ID('dbo.ExamSlips'))
    CREATE UNIQUE INDEX UX_ExamSlips_Student_Term_Month ON dbo.ExamSlips(StudentID, TermName, FeeMonth);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_ExamSlips_Term_Month_Number' AND object_id = OBJECT_ID('dbo.ExamSlips'))
    CREATE UNIQUE INDEX UX_ExamSlips_Term_Month_Number ON dbo.ExamSlips(TermName, FeeMonth, ExamNumber);";

            using (var connection = Database.GetConnection())
            using (var command = new SqlCommand(sql, connection))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                await command.ExecuteNonQueryAsync().ConfigureAwait(false);
                schemaEnsured = true;
            }
        }

        public async Task<int> GenerateSlipsAsync(string termName, string feeMonth, string className, string section)
        {
            await EnsureSchemaAsync().ConfigureAwait(false);

            var eligibleStudents = await GetEligibleStudentsWithoutSlipAsync(termName, feeMonth, className, section).ConfigureAwait(false);
            if (eligibleStudents.Count == 0)
            {
                return 0;
            }

            var usedNumbers = await GetUsedExamNumbersAsync(termName, feeMonth).ConfigureAwait(false);

            const string sql = @"
INSERT INTO dbo.ExamSlips (StudentID, TermName, FeeMonth, ExamNumber, GeneratedOn)
VALUES (@StudentID, @TermName, @FeeMonth, @ExamNumber, GETDATE());";

            using (var connection = Database.GetConnection())
            {
                await connection.OpenAsync().ConfigureAwait(false);
                using (var transaction = connection.BeginTransaction())
                using (var command = new SqlCommand(sql, connection, transaction))
                {
                    command.Parameters.Add("@StudentID", SqlDbType.Int);
                    command.Parameters.Add("@TermName", SqlDbType.NVarChar, 100).Value = termName.Trim();
                    command.Parameters.Add("@FeeMonth", SqlDbType.NVarChar, 20).Value = feeMonth.Trim();
                    command.Parameters.Add("@ExamNumber", SqlDbType.NVarChar, 20);

                    try
                    {
                        var inserted = 0;
                        foreach (var studentId in eligibleStudents)
                        {
                            command.Parameters["@StudentID"].Value = studentId;
                            command.Parameters["@ExamNumber"].Value = CreateExamNumber(usedNumbers);
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

        public async Task<List<ExamSlip>> GetSlipsAsync(string termName, string feeMonth, string className, string section)
        {
            await EnsureSchemaAsync().ConfigureAwait(false);

            var sql = @"
SELECT es.ExamSlipID,
       es.StudentID,
       es.TermName,
       es.FeeMonth,
       es.ExamNumber,
       es.GeneratedOn,
       s.Name AS StudentName,
       s.FatherName,
       s.RegistrationNo,
       c.ClassName,
       s.Section,
       f.Amount AS FeeAmount,
       ISNULL(f.PaidAmount, CASE WHEN f.Status = 'Paid' THEN f.Amount ELSE 0 END) AS PaidAmount
FROM dbo.ExamSlips es
INNER JOIN dbo.Students s ON s.StudentID = es.StudentID
LEFT JOIN dbo.Classes c ON c.ClassID = s.ClassID
INNER JOIN dbo.Fees f ON f.StudentID = s.StudentID
    AND LTRIM(RTRIM(f.Month)) = LTRIM(RTRIM(es.FeeMonth))
    AND LTRIM(RTRIM(ISNULL(f.FeeType, 'Monthly Tuition'))) = 'Monthly Tuition'
WHERE LTRIM(RTRIM(es.TermName)) = LTRIM(RTRIM(@TermName))
  AND LTRIM(RTRIM(es.FeeMonth)) = LTRIM(RTRIM(@FeeMonth))";

            var conditions = new List<string>();
            if (!string.IsNullOrWhiteSpace(className) && className != "All Classes")
            {
                conditions.Add("LTRIM(RTRIM(ISNULL(c.ClassName, ''))) = @ClassName");
            }
            if (!string.IsNullOrWhiteSpace(section) && section != "All Sections")
            {
                conditions.Add("LTRIM(RTRIM(ISNULL(s.Section, ''))) = @Section");
            }
            if (conditions.Count > 0)
            {
                sql += " AND " + string.Join(" AND ", conditions);
            }

            sql += " ORDER BY c.ClassName, s.Section, es.ExamNumber;";

            var slips = new List<ExamSlip>();
            using (var connection = Database.GetConnection())
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@TermName", termName ?? string.Empty);
                command.Parameters.AddWithValue("@FeeMonth", feeMonth ?? string.Empty);
                AddFilterParameters(command, className, section);
                await connection.OpenAsync().ConfigureAwait(false);

                using (var reader = await command.ExecuteReaderAsync().ConfigureAwait(false))
                {
                    while (await reader.ReadAsync().ConfigureAwait(false))
                    {
                        slips.Add(MapSlip(reader));
                    }
                }
            }

            return slips;
        }

        public async Task<int> GetEligibleStudentCountAsync(string feeMonth, string className, string section)
        {
            await EnsureSchemaAsync().ConfigureAwait(false);

            var sql = @"
SELECT COUNT(DISTINCT s.StudentID)
FROM dbo.Students s
LEFT JOIN dbo.Classes c ON c.ClassID = s.ClassID
INNER JOIN dbo.Fees f ON f.StudentID = s.StudentID
WHERE LTRIM(RTRIM(f.Month)) = LTRIM(RTRIM(@FeeMonth))
  AND LTRIM(RTRIM(ISNULL(f.FeeType, 'Monthly Tuition'))) = 'Monthly Tuition'
  AND ISNULL(f.PaidAmount, CASE WHEN f.Status = 'Paid' THEN f.Amount ELSE 0 END) >= f.Amount
  AND f.Amount > 0";

            var conditions = new List<string>();
            if (!string.IsNullOrWhiteSpace(className) && className != "All Classes")
            {
                conditions.Add("LTRIM(RTRIM(ISNULL(c.ClassName, ''))) = @ClassName");
            }
            if (!string.IsNullOrWhiteSpace(section) && section != "All Sections")
            {
                conditions.Add("LTRIM(RTRIM(ISNULL(s.Section, ''))) = @Section");
            }
            if (conditions.Count > 0)
            {
                sql += " AND " + string.Join(" AND ", conditions);
            }

            using (var connection = Database.GetConnection())
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@FeeMonth", feeMonth ?? string.Empty);
                AddFilterParameters(command, className, section);
                await connection.OpenAsync().ConfigureAwait(false);
                return Convert.ToInt32(await command.ExecuteScalarAsync().ConfigureAwait(false));
            }
        }

        public async Task<List<string>> GetSectionsAsync()
        {
            await EnsureSchemaAsync().ConfigureAwait(false);

            const string sql = @"
SELECT DISTINCT LTRIM(RTRIM(Section)) AS Section
FROM dbo.Students
WHERE Section IS NOT NULL AND LTRIM(RTRIM(Section)) <> ''
ORDER BY Section;";

            var sections = new List<string>();
            using (var connection = Database.GetConnection())
            using (var command = new SqlCommand(sql, connection))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                using (var reader = await command.ExecuteReaderAsync().ConfigureAwait(false))
                {
                    while (await reader.ReadAsync().ConfigureAwait(false))
                    {
                        sections.Add(reader["Section"] as string);
                    }
                }
            }

            return sections;
        }

        private async Task<List<int>> GetEligibleStudentsWithoutSlipAsync(string termName, string feeMonth, string className, string section)
        {
            var sql = @"
SELECT DISTINCT s.StudentID
FROM dbo.Students s
LEFT JOIN dbo.Classes c ON c.ClassID = s.ClassID
INNER JOIN dbo.Fees f ON f.StudentID = s.StudentID
WHERE LTRIM(RTRIM(f.Month)) = LTRIM(RTRIM(@FeeMonth))
  AND LTRIM(RTRIM(ISNULL(f.FeeType, 'Monthly Tuition'))) = 'Monthly Tuition'
  AND ISNULL(f.PaidAmount, CASE WHEN f.Status = 'Paid' THEN f.Amount ELSE 0 END) >= f.Amount
  AND f.Amount > 0
  AND NOT EXISTS (
      SELECT 1
      FROM dbo.ExamSlips es
      WHERE es.StudentID = s.StudentID
        AND LTRIM(RTRIM(es.TermName)) = LTRIM(RTRIM(@TermName))
        AND LTRIM(RTRIM(es.FeeMonth)) = LTRIM(RTRIM(@FeeMonth))
  )";

            var conditions = new List<string>();
            if (!string.IsNullOrWhiteSpace(className) && className != "All Classes")
            {
                conditions.Add("LTRIM(RTRIM(ISNULL(c.ClassName, ''))) = @ClassName");
            }
            if (!string.IsNullOrWhiteSpace(section) && section != "All Sections")
            {
                conditions.Add("LTRIM(RTRIM(ISNULL(s.Section, ''))) = @Section");
            }
            if (conditions.Count > 0)
            {
                sql += " AND " + string.Join(" AND ", conditions);
            }

            sql += " ORDER BY s.StudentID;";

            var students = new List<int>();
            using (var connection = Database.GetConnection())
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@TermName", termName ?? string.Empty);
                command.Parameters.AddWithValue("@FeeMonth", feeMonth ?? string.Empty);
                AddFilterParameters(command, className, section);
                await connection.OpenAsync().ConfigureAwait(false);

                using (var reader = await command.ExecuteReaderAsync().ConfigureAwait(false))
                {
                    while (await reader.ReadAsync().ConfigureAwait(false))
                    {
                        students.Add(Convert.ToInt32(reader["StudentID"]));
                    }
                }
            }

            return students;
        }

        private async Task<HashSet<string>> GetUsedExamNumbersAsync(string termName, string feeMonth)
        {
            const string sql = @"
SELECT ExamNumber
FROM dbo.ExamSlips
WHERE LTRIM(RTRIM(TermName)) = LTRIM(RTRIM(@TermName))
  AND LTRIM(RTRIM(FeeMonth)) = LTRIM(RTRIM(@FeeMonth));";

            var numbers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            using (var connection = Database.GetConnection())
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@TermName", termName ?? string.Empty);
                command.Parameters.AddWithValue("@FeeMonth", feeMonth ?? string.Empty);
                await connection.OpenAsync().ConfigureAwait(false);

                using (var reader = await command.ExecuteReaderAsync().ConfigureAwait(false))
                {
                    while (await reader.ReadAsync().ConfigureAwait(false))
                    {
                        numbers.Add(reader["ExamNumber"] as string);
                    }
                }
            }

            return numbers;
        }

        private static string CreateExamNumber(HashSet<string> usedNumbers)
        {
            string value;
            do
            {
                lock (Random)
                {
                    value = Random.Next(10000, 99999).ToString();
                }
            }
            while (usedNumbers.Contains(value));

            usedNumbers.Add(value);
            return value;
        }

        private static void AddFilterParameters(SqlCommand command, string className, string section)
        {
            if (!string.IsNullOrWhiteSpace(className) && className != "All Classes")
            {
                command.Parameters.AddWithValue("@ClassName", className.Trim());
            }
            if (!string.IsNullOrWhiteSpace(section) && section != "All Sections")
            {
                command.Parameters.AddWithValue("@Section", section.Trim());
            }
        }

        private static ExamSlip MapSlip(SqlDataReader reader)
        {
            return new ExamSlip
            {
                ExamSlipID = Convert.ToInt32(reader["ExamSlipID"]),
                StudentID = Convert.ToInt32(reader["StudentID"]),
                TermName = reader["TermName"] as string,
                FeeMonth = reader["FeeMonth"] as string,
                ExamNumber = reader["ExamNumber"] as string,
                GeneratedOn = Convert.ToDateTime(reader["GeneratedOn"]),
                StudentName = reader["StudentName"] as string,
                FatherName = reader["FatherName"] as string,
                RegistrationNo = reader["RegistrationNo"] as string,
                ClassName = reader["ClassName"] as string,
                Section = reader["Section"] as string,
                FeeAmount = reader["FeeAmount"] == DBNull.Value ? 0m : Convert.ToDecimal(reader["FeeAmount"]),
                PaidAmount = reader["PaidAmount"] == DBNull.Value ? 0m : Convert.ToDecimal(reader["PaidAmount"])
            };
        }
    }
}
