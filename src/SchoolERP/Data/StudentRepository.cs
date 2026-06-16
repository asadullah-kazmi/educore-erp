using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;
using SchoolERP.Models;

namespace SchoolERP.Data
{
    public class StudentRepository
    {
        private const string SelectColumns = @"
SELECT s.StudentID,
       s.RegistrationNo,
       s.Name,
       s.FatherName,
       s.DOB,
       s.ClassID,
       c.ClassName,
       s.Address,
       s.Phone,
       s.AdmissionDate,
       s.MonthlyFee
FROM dbo.Students s
LEFT JOIN dbo.Classes c ON s.ClassID = c.ClassID";

        public async Task<List<Student>> GetAllStudentsAsync()
        {
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

            const string sql = @"
INSERT INTO dbo.Students (RegistrationNo, Name, FatherName, DOB, ClassID, Class, Address, Phone, AdmissionDate, MonthlyFee)
VALUES (@RegistrationNo, @Name, @FatherName, @DOB, @ClassID, @Class, @Address, @Phone, @AdmissionDate, @MonthlyFee);";

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

            const string sql = @"
UPDATE dbo.Students
SET RegistrationNo = @RegistrationNo,
    Name = @Name,
    FatherName = @FatherName,
    DOB = @DOB,
    ClassID = @ClassID,
    Class = @Class,
    Address = @Address,
    Phone = @Phone,
    AdmissionDate = @AdmissionDate,
    MonthlyFee = @MonthlyFee
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
            const string sql = @"
SELECT ClassID, ClassName
FROM dbo.Classes
ORDER BY ClassName;";

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
            command.Parameters.AddWithValue("@Class", (object)student.ClassName ?? DBNull.Value);
            command.Parameters.AddWithValue("@Address", (object)student.Address ?? DBNull.Value);
            command.Parameters.AddWithValue("@Phone", (object)student.Phone ?? DBNull.Value);
            command.Parameters.AddWithValue("@AdmissionDate", (object)student.AdmissionDate ?? DBNull.Value);
            command.Parameters.AddWithValue("@MonthlyFee", student.MonthlyFee);
        }

        private static Student MapStudent(SqlDataReader reader)
        {
            return new Student
            {
                StudentID = reader.GetInt32(reader.GetOrdinal("StudentID")),
                RegistrationNo = reader["RegistrationNo"] as string,
                Name = reader["Name"] as string,
                FatherName = reader["FatherName"] as string,
                DOB = reader["DOB"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["DOB"]),
                ClassID = reader["ClassID"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["ClassID"]),
                ClassName = reader["ClassName"] as string,
                Address = reader["Address"] as string,
                Phone = reader["Phone"] as string,
                AdmissionDate = reader["AdmissionDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["AdmissionDate"]),
                MonthlyFee = reader["MonthlyFee"] == DBNull.Value ? 0m : Convert.ToDecimal(reader["MonthlyFee"])
            };
        }
    }
}
