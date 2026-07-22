using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;
using SchoolERP.Models;

namespace SchoolERP.Data
{
    public class StudentRepository
    {
        private static bool schemaEnsured;

        private const string SelectColumns = @"
SELECT s.StudentID,
       s.RegistrationNo,
       s.Name,
       s.FatherName,
       s.DOB,
       s.ClassID,
       c.ClassName,
       s.Section,
       s.Address,
       s.Phone,
       s.StudentFormBOrCnicNumber,
       s.StudentFormBOrCnicPicturePath,
       s.StudentFormBOrCnicFrontPicturePath,
       s.StudentFormBOrCnicFrontPictureData,
       s.StudentFormBOrCnicFrontPictureFileName,
       s.StudentFormBOrCnicBackPicturePath,
       s.StudentFormBOrCnicBackPictureData,
       s.StudentFormBOrCnicBackPictureFileName,
       s.GuardianCnicNumber,
       s.GuardianCnicPicturePath,
       s.GuardianCnicFrontPicturePath,
       s.GuardianCnicFrontPictureData,
       s.GuardianCnicFrontPictureFileName,
       s.GuardianCnicBackPicturePath,
       s.GuardianCnicBackPictureData,
       s.GuardianCnicBackPictureFileName,
       s.GuardianPhone,
       s.EmergencyContactNumber,
       s.AdmissionDate,
       s.MonthlyFee,
       s.IsActive
FROM dbo.Students s
LEFT JOIN dbo.Classes c ON s.ClassID = c.ClassID";

        public async Task<List<Student>> GetAllStudentsAsync()
        {
            await EnsureStudentProfileColumnsAsync().ConfigureAwait(false);

            const string sql = SelectColumns + @"
ORDER BY s.StudentID DESC;";

            var students = new List<Student>();

            using (var connection = Database.GetConnection())
            using (var command = new SqlCommand(sql, connection))
            {
                await connection.OpenAsync().ConfigureAwait(false);

                using (var reader = await command.ExecuteReaderAsync().ConfigureAwait(false))
                {
                    while (await reader.ReadAsync().ConfigureAwait(false))
                    {
                        students.Add(MapStudent(reader));
                    }
                }
            }

            return students;
        }

        public async Task<Student> GetStudentByIdAsync(int studentId)
        {
            await EnsureStudentProfileColumnsAsync().ConfigureAwait(false);

            const string sql = SelectColumns + @"
WHERE s.StudentID = @StudentID;";

            using (var connection = Database.GetConnection())
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@StudentID", studentId);
                await connection.OpenAsync().ConfigureAwait(false);

                using (var reader = await command.ExecuteReaderAsync().ConfigureAwait(false))
                {
                    if (await reader.ReadAsync().ConfigureAwait(false))
                    {
                        return MapStudent(reader);
                    }
                }
            }

            return null;
        }

        public async Task<bool> AddStudentAsync(Student student)
        {
            if (student == null)
            {
                throw new ArgumentNullException(nameof(student));
            }

            await EnsureStudentProfileColumnsAsync().ConfigureAwait(false);

            const string sql = @"
INSERT INTO dbo.Students (RegistrationNo, Name, FatherName, DOB, ClassID, Section, Address, Phone, StudentFormBOrCnicNumber, StudentFormBOrCnicPicturePath, StudentFormBOrCnicFrontPicturePath, StudentFormBOrCnicFrontPictureData, StudentFormBOrCnicFrontPictureFileName, StudentFormBOrCnicBackPicturePath, StudentFormBOrCnicBackPictureData, StudentFormBOrCnicBackPictureFileName, GuardianCnicNumber, GuardianCnicPicturePath, GuardianCnicFrontPicturePath, GuardianCnicFrontPictureData, GuardianCnicFrontPictureFileName, GuardianCnicBackPicturePath, GuardianCnicBackPictureData, GuardianCnicBackPictureFileName, GuardianPhone, EmergencyContactNumber, AdmissionDate, MonthlyFee, IsActive)
VALUES (@RegistrationNo, @Name, @FatherName, @DOB, @ClassID, @Section, @Address, @Phone, @StudentFormBOrCnicNumber, @StudentFormBOrCnicPicturePath, @StudentFormBOrCnicFrontPicturePath, @StudentFormBOrCnicFrontPictureData, @StudentFormBOrCnicFrontPictureFileName, @StudentFormBOrCnicBackPicturePath, @StudentFormBOrCnicBackPictureData, @StudentFormBOrCnicBackPictureFileName, @GuardianCnicNumber, @GuardianCnicPicturePath, @GuardianCnicFrontPicturePath, @GuardianCnicFrontPictureData, @GuardianCnicFrontPictureFileName, @GuardianCnicBackPicturePath, @GuardianCnicBackPictureData, @GuardianCnicBackPictureFileName, @GuardianPhone, @EmergencyContactNumber, @AdmissionDate, @MonthlyFee, @IsActive);";

            using (var connection = Database.GetConnection())
            using (var command = new SqlCommand(sql, connection))
            {
                AddStudentParameters(command, student);
                await connection.OpenAsync().ConfigureAwait(false);
                return await command.ExecuteNonQueryAsync().ConfigureAwait(false) > 0;
            }
        }

        public async Task<bool> UpdateStudentAsync(Student student)
        {
            if (student == null)
            {
                throw new ArgumentNullException(nameof(student));
            }

            await EnsureStudentProfileColumnsAsync().ConfigureAwait(false);

            const string sql = @"
UPDATE dbo.Students
SET RegistrationNo = @RegistrationNo,
    Name = @Name,
    FatherName = @FatherName,
    DOB = @DOB,
    ClassID = @ClassID,
    Section = @Section,
    Address = @Address,
    Phone = @Phone,
    StudentFormBOrCnicNumber = @StudentFormBOrCnicNumber,
    StudentFormBOrCnicPicturePath = @StudentFormBOrCnicPicturePath,
    StudentFormBOrCnicFrontPicturePath = @StudentFormBOrCnicFrontPicturePath,
    StudentFormBOrCnicFrontPictureData = @StudentFormBOrCnicFrontPictureData,
    StudentFormBOrCnicFrontPictureFileName = @StudentFormBOrCnicFrontPictureFileName,
    StudentFormBOrCnicBackPicturePath = @StudentFormBOrCnicBackPicturePath,
    StudentFormBOrCnicBackPictureData = @StudentFormBOrCnicBackPictureData,
    StudentFormBOrCnicBackPictureFileName = @StudentFormBOrCnicBackPictureFileName,
    GuardianCnicNumber = @GuardianCnicNumber,
    GuardianCnicPicturePath = @GuardianCnicPicturePath,
    GuardianCnicFrontPicturePath = @GuardianCnicFrontPicturePath,
    GuardianCnicFrontPictureData = @GuardianCnicFrontPictureData,
    GuardianCnicFrontPictureFileName = @GuardianCnicFrontPictureFileName,
    GuardianCnicBackPicturePath = @GuardianCnicBackPicturePath,
    GuardianCnicBackPictureData = @GuardianCnicBackPictureData,
    GuardianCnicBackPictureFileName = @GuardianCnicBackPictureFileName,
    GuardianPhone = @GuardianPhone,
    EmergencyContactNumber = @EmergencyContactNumber,
    AdmissionDate = @AdmissionDate,
    MonthlyFee = @MonthlyFee,
    IsActive = @IsActive
WHERE StudentID = @StudentID;";

            using (var connection = Database.GetConnection())
            using (var command = new SqlCommand(sql, connection))
            {
                AddStudentParameters(command, student);
                command.Parameters.AddWithValue("@StudentID", student.StudentID);
                await connection.OpenAsync().ConfigureAwait(false);
                return await command.ExecuteNonQueryAsync().ConfigureAwait(false) > 0;
            }
        }

        public async Task<bool> DeleteStudentAsync(int studentId)
        {
            await EnsureStudentProfileColumnsAsync().ConfigureAwait(false);
            await new FamilyReceiptRepository().EnsureSchemaAsync().ConfigureAwait(false);

            const string sql = "DELETE FROM dbo.Students WHERE StudentID = @StudentID;";

            using (var connection = Database.GetConnection())
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@StudentID", studentId);
                await connection.OpenAsync().ConfigureAwait(false);
                return await command.ExecuteNonQueryAsync().ConfigureAwait(false) > 0;
            }
        }

        public async Task<bool> RegistrationNoExistsAsync(string regNo, int? excludeStudentId = null)
        {
            await EnsureStudentProfileColumnsAsync().ConfigureAwait(false);

            const string sql = @"
SELECT COUNT(1)
FROM dbo.Students
WHERE RegistrationNo = @RegistrationNo
  AND (@ExcludeStudentId IS NULL OR StudentID <> @ExcludeStudentId);";

            using (var connection = Database.GetConnection())
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@RegistrationNo", regNo ?? string.Empty);
                command.Parameters.AddWithValue("@ExcludeStudentId", (object)excludeStudentId ?? DBNull.Value);
                await connection.OpenAsync().ConfigureAwait(false);
                var count = (int)await command.ExecuteScalarAsync().ConfigureAwait(false);
                return count > 0;
            }
        }

        public async Task<List<Class>> GetAllClassesAsync()
        {
            await EnsureStudentProfileColumnsAsync().ConfigureAwait(false);

            const string sql = @"
SELECT ClassID, ClassName
FROM dbo.Classes
ORDER BY
    CASE ClassName
        WHEN 'Nursery' THEN 0
        WHEN 'Prep' THEN 1
        WHEN 'One' THEN 2
        WHEN 'Two' THEN 3
        WHEN 'Three' THEN 4
        WHEN 'Four' THEN 5
        WHEN 'Five' THEN 6
        WHEN 'Six' THEN 7
        WHEN 'Seven' THEN 8
        WHEN 'Eight' THEN 9
        WHEN 'Nine' THEN 10
        WHEN 'Ten' THEN 11
        WHEN 'Class 1' THEN 2
        WHEN 'Class 2' THEN 3
        WHEN 'Class 3' THEN 4
        WHEN 'Class 4' THEN 5
        WHEN 'Class 5' THEN 6
        WHEN 'Class 6' THEN 7
        WHEN 'Class 7' THEN 8
        WHEN 'Class 8' THEN 9
        WHEN 'Class 9' THEN 10
        WHEN 'Class 10' THEN 11
        ELSE 100
    END,
    ClassName;";

            var classes = new List<Class>();

            using (var connection = Database.GetConnection())
            using (var command = new SqlCommand(sql, connection))
            {
                await connection.OpenAsync().ConfigureAwait(false);

                using (var reader = await command.ExecuteReaderAsync().ConfigureAwait(false))
                {
                    while (await reader.ReadAsync().ConfigureAwait(false))
                    {
                        classes.Add(new Class
                        {
                            ClassID = reader.GetInt32(reader.GetOrdinal("ClassID")),
                            ClassName = reader["ClassName"] as string
                        });
                    }
                }
            }

            return classes;
        }

        private static void AddStudentParameters(SqlCommand command, Student student)
        {
            command.Parameters.AddWithValue("@RegistrationNo", (object)student.RegistrationNo ?? DBNull.Value);
            command.Parameters.AddWithValue("@Name", (object)student.Name ?? DBNull.Value);
            command.Parameters.AddWithValue("@FatherName", (object)student.FatherName ?? DBNull.Value);
            command.Parameters.AddWithValue("@DOB", (object)student.DOB ?? DBNull.Value);
            command.Parameters.AddWithValue("@ClassID", (object)student.ClassID ?? DBNull.Value);
            command.Parameters.AddWithValue("@Section", (object)student.Section ?? DBNull.Value);
            command.Parameters.AddWithValue("@Address", (object)student.Address ?? DBNull.Value);
            command.Parameters.AddWithValue("@Phone", (object)student.Phone ?? DBNull.Value);
            command.Parameters.AddWithValue("@StudentFormBOrCnicNumber", (object)student.StudentFormBOrCnicNumber ?? DBNull.Value);
            command.Parameters.AddWithValue("@StudentFormBOrCnicPicturePath", (object)student.StudentFormBOrCnicPicturePath ?? DBNull.Value);
            command.Parameters.AddWithValue("@StudentFormBOrCnicFrontPicturePath", (object)student.StudentFormBOrCnicFrontPicturePath ?? DBNull.Value);
            command.Parameters.Add(new SqlParameter("@StudentFormBOrCnicFrontPictureData", System.Data.SqlDbType.VarBinary, -1) { Value = (object)student.StudentFormBOrCnicFrontPictureData ?? DBNull.Value });
            command.Parameters.AddWithValue("@StudentFormBOrCnicFrontPictureFileName", (object)student.StudentFormBOrCnicFrontPictureFileName ?? DBNull.Value);
            command.Parameters.AddWithValue("@StudentFormBOrCnicBackPicturePath", (object)student.StudentFormBOrCnicBackPicturePath ?? DBNull.Value);
            command.Parameters.Add(new SqlParameter("@StudentFormBOrCnicBackPictureData", System.Data.SqlDbType.VarBinary, -1) { Value = (object)student.StudentFormBOrCnicBackPictureData ?? DBNull.Value });
            command.Parameters.AddWithValue("@StudentFormBOrCnicBackPictureFileName", (object)student.StudentFormBOrCnicBackPictureFileName ?? DBNull.Value);
            command.Parameters.AddWithValue("@GuardianCnicNumber", (object)student.GuardianCnicNumber ?? DBNull.Value);
            command.Parameters.AddWithValue("@GuardianCnicPicturePath", (object)student.GuardianCnicPicturePath ?? DBNull.Value);
            command.Parameters.AddWithValue("@GuardianCnicFrontPicturePath", (object)student.GuardianCnicFrontPicturePath ?? DBNull.Value);
            command.Parameters.Add(new SqlParameter("@GuardianCnicFrontPictureData", System.Data.SqlDbType.VarBinary, -1) { Value = (object)student.GuardianCnicFrontPictureData ?? DBNull.Value });
            command.Parameters.AddWithValue("@GuardianCnicFrontPictureFileName", (object)student.GuardianCnicFrontPictureFileName ?? DBNull.Value);
            command.Parameters.AddWithValue("@GuardianCnicBackPicturePath", (object)student.GuardianCnicBackPicturePath ?? DBNull.Value);
            command.Parameters.Add(new SqlParameter("@GuardianCnicBackPictureData", System.Data.SqlDbType.VarBinary, -1) { Value = (object)student.GuardianCnicBackPictureData ?? DBNull.Value });
            command.Parameters.AddWithValue("@GuardianCnicBackPictureFileName", (object)student.GuardianCnicBackPictureFileName ?? DBNull.Value);
            command.Parameters.AddWithValue("@GuardianPhone", (object)student.GuardianPhone ?? DBNull.Value);
            command.Parameters.AddWithValue("@EmergencyContactNumber", (object)student.EmergencyContactNumber ?? DBNull.Value);
            command.Parameters.AddWithValue("@AdmissionDate", (object)student.AdmissionDate ?? DBNull.Value);
            command.Parameters.AddWithValue("@MonthlyFee", student.MonthlyFee);
            command.Parameters.AddWithValue("@IsActive", student.IsActive);
        }

        private static Student MapStudent(SqlDataReader reader)
        {
            var studentLegacyPicturePath = reader["StudentFormBOrCnicPicturePath"] as string;
            var guardianLegacyPicturePath = reader["GuardianCnicPicturePath"] as string;
            var studentFrontPicturePath = reader["StudentFormBOrCnicFrontPicturePath"] as string;
            var guardianFrontPicturePath = reader["GuardianCnicFrontPicturePath"] as string;

            return new Student
            {
                StudentID = reader.GetInt32(reader.GetOrdinal("StudentID")),
                RegistrationNo = reader["RegistrationNo"] as string,
                Name = reader["Name"] as string,
                FatherName = reader["FatherName"] as string,
                DOB = reader["DOB"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["DOB"]),
                ClassID = reader["ClassID"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["ClassID"]),
                ClassName = reader["ClassName"] as string,
                Section = reader["Section"] as string,
                Address = reader["Address"] as string,
                Phone = reader["Phone"] as string,
                StudentFormBOrCnicNumber = reader["StudentFormBOrCnicNumber"] as string,
                StudentFormBOrCnicPicturePath = studentLegacyPicturePath,
                StudentFormBOrCnicFrontPicturePath = string.IsNullOrWhiteSpace(studentFrontPicturePath) ? studentLegacyPicturePath : studentFrontPicturePath,
                StudentFormBOrCnicFrontPictureData = reader["StudentFormBOrCnicFrontPictureData"] as byte[],
                StudentFormBOrCnicFrontPictureFileName = reader["StudentFormBOrCnicFrontPictureFileName"] as string,
                StudentFormBOrCnicBackPicturePath = reader["StudentFormBOrCnicBackPicturePath"] as string,
                StudentFormBOrCnicBackPictureData = reader["StudentFormBOrCnicBackPictureData"] as byte[],
                StudentFormBOrCnicBackPictureFileName = reader["StudentFormBOrCnicBackPictureFileName"] as string,
                GuardianCnicNumber = reader["GuardianCnicNumber"] as string,
                GuardianCnicPicturePath = guardianLegacyPicturePath,
                GuardianCnicFrontPicturePath = string.IsNullOrWhiteSpace(guardianFrontPicturePath) ? guardianLegacyPicturePath : guardianFrontPicturePath,
                GuardianCnicFrontPictureData = reader["GuardianCnicFrontPictureData"] as byte[],
                GuardianCnicFrontPictureFileName = reader["GuardianCnicFrontPictureFileName"] as string,
                GuardianCnicBackPicturePath = reader["GuardianCnicBackPicturePath"] as string,
                GuardianCnicBackPictureData = reader["GuardianCnicBackPictureData"] as byte[],
                GuardianCnicBackPictureFileName = reader["GuardianCnicBackPictureFileName"] as string,
                GuardianPhone = reader["GuardianPhone"] as string,
                EmergencyContactNumber = reader["EmergencyContactNumber"] as string,
                AdmissionDate = reader["AdmissionDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["AdmissionDate"]),
                MonthlyFee = reader["MonthlyFee"] == DBNull.Value ? 0m : Convert.ToDecimal(reader["MonthlyFee"]),
                IsActive = reader["IsActive"] == DBNull.Value || Convert.ToBoolean(reader["IsActive"])
            };
        }

        private static async Task EnsureStudentProfileColumnsAsync()
        {
            if (schemaEnsured)
            {
                return;
            }

            const string sql = @"
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Students') AND name = 'Section')
    ALTER TABLE dbo.Students ADD Section NVARCHAR(10) NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Students') AND name = 'StudentFormBOrCnicNumber')
    ALTER TABLE dbo.Students ADD StudentFormBOrCnicNumber NVARCHAR(50) NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Students') AND name = 'StudentFormBOrCnicPicturePath')
    ALTER TABLE dbo.Students ADD StudentFormBOrCnicPicturePath NVARCHAR(1000) NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Students') AND name = 'StudentFormBOrCnicFrontPicturePath')
    ALTER TABLE dbo.Students ADD StudentFormBOrCnicFrontPicturePath NVARCHAR(1000) NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Students') AND name = 'StudentFormBOrCnicFrontPictureData')
    ALTER TABLE dbo.Students ADD StudentFormBOrCnicFrontPictureData VARBINARY(MAX) NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Students') AND name = 'StudentFormBOrCnicFrontPictureFileName')
    ALTER TABLE dbo.Students ADD StudentFormBOrCnicFrontPictureFileName NVARCHAR(260) NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Students') AND name = 'StudentFormBOrCnicBackPicturePath')
    ALTER TABLE dbo.Students ADD StudentFormBOrCnicBackPicturePath NVARCHAR(1000) NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Students') AND name = 'StudentFormBOrCnicBackPictureData')
    ALTER TABLE dbo.Students ADD StudentFormBOrCnicBackPictureData VARBINARY(MAX) NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Students') AND name = 'StudentFormBOrCnicBackPictureFileName')
    ALTER TABLE dbo.Students ADD StudentFormBOrCnicBackPictureFileName NVARCHAR(260) NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Students') AND name = 'GuardianCnicNumber')
    ALTER TABLE dbo.Students ADD GuardianCnicNumber NVARCHAR(50) NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Students') AND name = 'GuardianCnicPicturePath')
    ALTER TABLE dbo.Students ADD GuardianCnicPicturePath NVARCHAR(1000) NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Students') AND name = 'GuardianCnicFrontPicturePath')
    ALTER TABLE dbo.Students ADD GuardianCnicFrontPicturePath NVARCHAR(1000) NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Students') AND name = 'GuardianCnicFrontPictureData')
    ALTER TABLE dbo.Students ADD GuardianCnicFrontPictureData VARBINARY(MAX) NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Students') AND name = 'GuardianCnicFrontPictureFileName')
    ALTER TABLE dbo.Students ADD GuardianCnicFrontPictureFileName NVARCHAR(260) NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Students') AND name = 'GuardianCnicBackPicturePath')
    ALTER TABLE dbo.Students ADD GuardianCnicBackPicturePath NVARCHAR(1000) NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Students') AND name = 'GuardianCnicBackPictureData')
    ALTER TABLE dbo.Students ADD GuardianCnicBackPictureData VARBINARY(MAX) NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Students') AND name = 'GuardianCnicBackPictureFileName')
    ALTER TABLE dbo.Students ADD GuardianCnicBackPictureFileName NVARCHAR(260) NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Students') AND name = 'GuardianPhone')
    ALTER TABLE dbo.Students ADD GuardianPhone NVARCHAR(50) NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Students') AND name = 'EmergencyContactNumber')
    ALTER TABLE dbo.Students ADD EmergencyContactNumber NVARCHAR(50) NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Students') AND name = 'IsActive')
    ALTER TABLE dbo.Students ADD IsActive BIT NOT NULL CONSTRAINT DF_Students_IsActive DEFAULT 1;";

            using (var connection = Database.GetConnection())
            using (var command = new SqlCommand(sql, connection))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                await command.ExecuteNonQueryAsync().ConfigureAwait(false);
                schemaEnsured = true;
            }
        }
    }
}
