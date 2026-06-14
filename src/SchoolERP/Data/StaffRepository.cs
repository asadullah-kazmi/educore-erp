using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;
using SchoolERP.Models;

namespace SchoolERP.Data
{
    public class StaffRepository
    {
        public async Task<List<Teacher>> GetAllStaffAsync()
        {
            const string sql = @"
SELECT TeacherID, Name, Designation, Salary, FingerprintID
FROM dbo.Teachers
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
            const string sql = @"
SELECT TeacherID, Name, Designation, Salary, FingerprintID
FROM dbo.Teachers
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

            const string sql = @"
INSERT INTO dbo.Teachers (Name, Designation, Salary, FingerprintID)
VALUES (@Name, @Designation, @Salary, @FingerprintID);";

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

            const string sql = @"
UPDATE dbo.Teachers
SET Name = @Name,
    Designation = @Designation,
    Salary = @Salary,
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
            command.Parameters.AddWithValue("@Designation", (object)teacher.Designation ?? DBNull.Value);
            command.Parameters.AddWithValue("@Salary", teacher.Salary);
            command.Parameters.AddWithValue("@FingerprintID", (object)teacher.FingerprintID ?? DBNull.Value);
        }

        private static Teacher MapTeacher(SqlDataReader reader)
        {
            return new Teacher
            {
                TeacherID = reader.GetInt32(reader.GetOrdinal("TeacherID")),
                Name = reader["Name"] as string,
                Designation = reader["Designation"] as string,
                Salary = reader["Salary"] == DBNull.Value ? 0m : Convert.ToDecimal(reader["Salary"]),
                FingerprintID = reader["FingerprintID"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["FingerprintID"])
            };
        }
    }
}
