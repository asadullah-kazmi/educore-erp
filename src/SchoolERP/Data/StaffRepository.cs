using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;
using SchoolERP.Models;

namespace SchoolERP.Data
{
    public class StaffRepository
    {
        private static bool staffProfileColumnsEnsured;

        private const string SelectColumns = @"
SELECT TeacherID,
       Name,
       Age,
       Experience,
       DOB,
       ContactNumber,
       DateOfJoining,
       Designation,
       Salary,
       Address,
       CnicNumber,
       CnicFrontImagePath,
       CnicBackImagePath,
       EducationalDocumentsPath,
       CertificatesPath,
       FingerprintID
FROM dbo.Teachers";

        public async Task<List<Teacher>> GetAllStaffAsync()
        {
            await EnsureStaffProfileColumnsAsync().ConfigureAwait(false);

            const string sql = SelectColumns + @"
ORDER BY Name;";

            var staff = new List<Teacher>();

            using (var connection = Database.GetConnection())
            using (var command = new SqlCommand(sql, connection))
            {
                await connection.OpenAsync().ConfigureAwait(false);

                using (var reader = await command.ExecuteReaderAsync().ConfigureAwait(false))
                {
                    while (await reader.ReadAsync().ConfigureAwait(false))
                    {
                        staff.Add(MapTeacher(reader));
                    }
                }
            }

            return staff;
        }

        public async Task<Teacher> GetStaffByIdAsync(int teacherId)
        {
            await EnsureStaffProfileColumnsAsync().ConfigureAwait(false);

            const string sql = SelectColumns + @"
WHERE TeacherID = @TeacherID;";

            using (var connection = Database.GetConnection())
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@TeacherID", teacherId);
                await connection.OpenAsync().ConfigureAwait(false);

                using (var reader = await command.ExecuteReaderAsync().ConfigureAwait(false))
                {
                    if (await reader.ReadAsync().ConfigureAwait(false))
                    {
                        return MapTeacher(reader);
                    }
                }
            }

            return null;
        }

        public async Task<bool> AddStaffAsync(Teacher teacher)
        {
            if (teacher == null)
            {
                throw new ArgumentNullException(nameof(teacher));
            }

            await EnsureStaffProfileColumnsAsync().ConfigureAwait(false);

            const string sql = @"
INSERT INTO dbo.Teachers (Name, Age, Experience, DOB, ContactNumber, DateOfJoining, Designation, Salary, Address, CnicNumber, CnicFrontImagePath, CnicBackImagePath, EducationalDocumentsPath, CertificatesPath, FingerprintID)
VALUES (@Name, @Age, @Experience, @DOB, @ContactNumber, @DateOfJoining, @Designation, @Salary, @Address, @CnicNumber, @CnicFrontImagePath, @CnicBackImagePath, @EducationalDocumentsPath, @CertificatesPath, @FingerprintID);";

            using (var connection = Database.GetConnection())
            using (var command = new SqlCommand(sql, connection))
            {
                AddTeacherParameters(command, teacher);
                await connection.OpenAsync().ConfigureAwait(false);
                return await command.ExecuteNonQueryAsync().ConfigureAwait(false) > 0;
            }
        }

        public async Task<bool> UpdateStaffAsync(Teacher teacher)
        {
            if (teacher == null)
            {
                throw new ArgumentNullException(nameof(teacher));
            }

            await EnsureStaffProfileColumnsAsync().ConfigureAwait(false);

            const string sql = @"
UPDATE dbo.Teachers
SET Name = @Name,
    Age = @Age,
    Experience = @Experience,
    DOB = @DOB,
    ContactNumber = @ContactNumber,
    DateOfJoining = @DateOfJoining,
    Designation = @Designation,
    Salary = @Salary,
    Address = @Address,
    CnicNumber = @CnicNumber,
    CnicFrontImagePath = @CnicFrontImagePath,
    CnicBackImagePath = @CnicBackImagePath,
    EducationalDocumentsPath = @EducationalDocumentsPath,
    CertificatesPath = @CertificatesPath,
    FingerprintID = @FingerprintID
WHERE TeacherID = @TeacherID;";

            using (var connection = Database.GetConnection())
            using (var command = new SqlCommand(sql, connection))
            {
                AddTeacherParameters(command, teacher);
                command.Parameters.AddWithValue("@TeacherID", teacher.TeacherID);
                await connection.OpenAsync().ConfigureAwait(false);
                return await command.ExecuteNonQueryAsync().ConfigureAwait(false) > 0;
            }
        }

        public async Task<bool> DeleteStaffAsync(int teacherId)
        {
            await EnsureStaffProfileColumnsAsync().ConfigureAwait(false);

            const string sql = "DELETE FROM dbo.Teachers WHERE TeacherID = @TeacherID;";

            using (var connection = Database.GetConnection())
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@TeacherID", teacherId);
                await connection.OpenAsync().ConfigureAwait(false);
                return await command.ExecuteNonQueryAsync().ConfigureAwait(false) > 0;
            }
        }

        private static void AddTeacherParameters(SqlCommand command, Teacher teacher)
        {
            command.Parameters.AddWithValue("@Name", (object)teacher.Name ?? DBNull.Value);
            command.Parameters.AddWithValue("@Age", (object)teacher.Age ?? DBNull.Value);
            command.Parameters.AddWithValue("@Experience", (object)teacher.Experience ?? DBNull.Value);
            command.Parameters.AddWithValue("@DOB", (object)teacher.DOB ?? DBNull.Value);
            command.Parameters.AddWithValue("@ContactNumber", (object)teacher.ContactNumber ?? DBNull.Value);
            command.Parameters.AddWithValue("@DateOfJoining", (object)teacher.DateOfJoining ?? DBNull.Value);
            command.Parameters.AddWithValue("@Designation", (object)teacher.Designation ?? DBNull.Value);
            command.Parameters.AddWithValue("@Salary", teacher.Salary);
            command.Parameters.AddWithValue("@Address", (object)teacher.Address ?? DBNull.Value);
            command.Parameters.AddWithValue("@CnicNumber", (object)teacher.CnicNumber ?? DBNull.Value);
            command.Parameters.AddWithValue("@CnicFrontImagePath", (object)teacher.CnicFrontImagePath ?? DBNull.Value);
            command.Parameters.AddWithValue("@CnicBackImagePath", (object)teacher.CnicBackImagePath ?? DBNull.Value);
            command.Parameters.AddWithValue("@EducationalDocumentsPath", (object)teacher.EducationalDocumentsPath ?? DBNull.Value);
            command.Parameters.AddWithValue("@CertificatesPath", (object)teacher.CertificatesPath ?? DBNull.Value);
            command.Parameters.AddWithValue("@FingerprintID", (object)teacher.FingerprintID ?? DBNull.Value);
        }

        private static Teacher MapTeacher(SqlDataReader reader)
        {
            return new Teacher
            {
                TeacherID = reader.GetInt32(reader.GetOrdinal("TeacherID")),
                Name = reader["Name"] as string,
                Age = reader["Age"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["Age"]),
                Experience = reader["Experience"] as string,
                DOB = reader["DOB"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["DOB"]),
                ContactNumber = reader["ContactNumber"] as string,
                DateOfJoining = reader["DateOfJoining"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["DateOfJoining"]),
                Designation = reader["Designation"] as string,
                Salary = reader["Salary"] == DBNull.Value ? 0m : Convert.ToDecimal(reader["Salary"]),
                Address = reader["Address"] as string,
                CnicNumber = reader["CnicNumber"] as string,
                CnicFrontImagePath = reader["CnicFrontImagePath"] as string,
                CnicBackImagePath = reader["CnicBackImagePath"] as string,
                EducationalDocumentsPath = reader["EducationalDocumentsPath"] as string,
                CertificatesPath = reader["CertificatesPath"] as string,
                FingerprintID = reader["FingerprintID"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["FingerprintID"])
            };
        }

        private static async Task EnsureStaffProfileColumnsAsync()
        {
            if (staffProfileColumnsEnsured)
            {
                return;
            }

            const string sql = @"
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Teachers') AND name = 'Age')
    ALTER TABLE dbo.Teachers ADD Age INT NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Teachers') AND name = 'Experience')
    ALTER TABLE dbo.Teachers ADD Experience NVARCHAR(100) NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Teachers') AND name = 'DOB')
    ALTER TABLE dbo.Teachers ADD DOB DATE NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Teachers') AND name = 'ContactNumber')
    ALTER TABLE dbo.Teachers ADD ContactNumber NVARCHAR(50) NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Teachers') AND name = 'DateOfJoining')
    ALTER TABLE dbo.Teachers ADD DateOfJoining DATE NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Teachers') AND name = 'Address')
    ALTER TABLE dbo.Teachers ADD Address NVARCHAR(1000) NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Teachers') AND name = 'CnicNumber')
    ALTER TABLE dbo.Teachers ADD CnicNumber NVARCHAR(50) NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Teachers') AND name = 'CnicFrontImagePath')
    ALTER TABLE dbo.Teachers ADD CnicFrontImagePath NVARCHAR(1000) NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Teachers') AND name = 'CnicBackImagePath')
    ALTER TABLE dbo.Teachers ADD CnicBackImagePath NVARCHAR(1000) NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Teachers') AND name = 'EducationalDocumentsPath')
    ALTER TABLE dbo.Teachers ADD EducationalDocumentsPath NVARCHAR(2000) NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Teachers') AND name = 'CertificatesPath')
    ALTER TABLE dbo.Teachers ADD CertificatesPath NVARCHAR(2000) NULL;";

            using (var connection = Database.GetConnection())
            using (var command = new SqlCommand(sql, connection))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                await command.ExecuteNonQueryAsync().ConfigureAwait(false);
                staffProfileColumnsEnsured = true;
            }
        }
    }
}
