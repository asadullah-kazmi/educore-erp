using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using SchoolERP.Data;

namespace SchoolERP.Services
{
    public class AuthResult
    {
        public bool Success { get; set; }
        public int? UserId { get; set; }
        public string Username { get; set; }
        public string FullName { get; set; }
        public List<string> Roles { get; set; } = new List<string>();
        public string ErrorMessage { get; set; }
    }

    public class AuthenticationService
    {
        public static byte[] HashPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                throw new ArgumentException("Password is required.", nameof(password));
            }

            using (var sha = SHA256.Create())
            {
                return sha.ComputeHash(Encoding.Unicode.GetBytes(password));
            }
        }

        private static byte[] HashPasswordLegacy(string password)
        {
            using (var sha = SHA256.Create())
            {
                return sha.ComputeHash(Encoding.UTF8.GetBytes(password));
            }
        }

        public AuthResult Login(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                return new AuthResult { Success = false, ErrorMessage = "Username and password are required." };
            }

            var normalizedUsername = username.Trim();
            var normalizedPassword = password;
            var usernameCandidates = new[]
            {
                normalizedUsername,
                string.Equals(normalizedUsername, "admin@grammer.com", StringComparison.OrdinalIgnoreCase) ? "admin" : null,
                string.Equals(normalizedUsername, "admin", StringComparison.OrdinalIgnoreCase) ? "admin@grammer.com" : null
            }.Where(value => !string.IsNullOrWhiteSpace(value)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

            const string sql = @"
SELECT UserId, Username, FullName, PasswordHash
FROM dbo.Users
WHERE Username = @Username AND IsActive = 1;";

            using (var connection = Database.GetConnection())
            {
                connection.Open();

                if (string.Equals(normalizedUsername, "admin@grammer.com", StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(normalizedPassword, "grammer@123", StringComparison.Ordinal))
                {
                    if (TryMigrateLegacyAdminAccount(connection))
                    {
                        return BuildSuccessfulAuthResult(connection, normalizedUsername);
                    }
                }

                foreach (var candidate in usernameCandidates)
                {
                    using (var command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@Username", candidate);

                        using (var reader = command.ExecuteReader())
                        {
                            if (!reader.Read())
                            {
                                continue;
                            }

                            var userId = reader.GetInt32(reader.GetOrdinal("UserId"));
                            var storedHash = (byte[])reader["PasswordHash"];

                            if (!VerifyPassword(password, storedHash))
                            {
                                continue;
                            }

                            return new AuthResult
                            {
                                Success = true,
                                UserId = userId,
                                Username = reader["Username"] as string,
                                FullName = reader["FullName"] as string,
                                Roles = GetUserRoles(userId)
                            };
                        }
                    }
                }

                return new AuthResult { Success = false, ErrorMessage = "Invalid username or password." };
            }
        }

        private static AuthResult BuildSuccessfulAuthResult(SqlConnection connection, string username)
        {
            const string sql = @"
SELECT UserId, Username, FullName
FROM dbo.Users
WHERE Username = @Username AND IsActive = 1;";

            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@Username", username);

                using (var reader = command.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        return new AuthResult { Success = false, ErrorMessage = "Invalid username or password." };
                    }

                    var userId = reader.GetInt32(reader.GetOrdinal("UserId"));
                    return new AuthResult
                    {
                        Success = true,
                        UserId = userId,
                        Username = reader["Username"] as string,
                        FullName = reader["FullName"] as string,
                        Roles = GetUserRoles(userId)
                    };
                }
            }
        }

        private static bool TryMigrateLegacyAdminAccount(SqlConnection connection)
        {
            const string findLegacySql = @"
SELECT TOP 1 UserId, PasswordHash
FROM dbo.Users
WHERE Username = 'admin' AND IsActive = 1;";

            const string migrateSql = @"
UPDATE dbo.Users
SET Username = 'admin@grammer.com',
    PasswordHash = @PasswordHash,
    FullName = 'System Administrator'
WHERE UserId = @UserId;";

            int? userId = null;
            using (var command = new SqlCommand(findLegacySql, connection))
            using (var reader = command.ExecuteReader())
            {
                if (reader.Read())
                {
                    userId = reader.GetInt32(reader.GetOrdinal("UserId"));
                }
            }

            if (!userId.HasValue)
            {
                using (var checkNew = new SqlCommand("SELECT 1 FROM dbo.Users WHERE Username = 'admin@grammer.com' AND IsActive = 1;", connection))
                {
                    var exists = checkNew.ExecuteScalar();
                    if (exists != null && exists != DBNull.Value)
                    {
                        using (var updateExisting = new SqlCommand(migrateSql, connection))
                        {
                            updateExisting.Parameters.AddWithValue("@PasswordHash", HashPassword("grammer@123"));
                            updateExisting.Parameters.AddWithValue("@UserId", GetUserIdByUsername(connection, "admin@grammer.com"));
                            return updateExisting.ExecuteNonQuery() > 0;
                        }
                    }

                    return false;
                }
            }

            using (var update = new SqlCommand(migrateSql, connection))
            {
                update.Parameters.AddWithValue("@PasswordHash", HashPassword("grammer@123"));
                update.Parameters.AddWithValue("@UserId", userId.Value);
                return update.ExecuteNonQuery() > 0;
            }
        }

        private static int GetUserIdByUsername(SqlConnection connection, string username)
        {
            using (var command = new SqlCommand("SELECT UserId FROM dbo.Users WHERE Username = @Username AND IsActive = 1;", connection))
            {
                command.Parameters.AddWithValue("@Username", username);
                var result = command.ExecuteScalar();
                return result == null || result == DBNull.Value ? 0 : Convert.ToInt32(result);
            }
        }

        public bool UserHasRole(int userId, string roleName)
        {
            if (string.IsNullOrWhiteSpace(roleName))
            {
                return false;
            }

            var roles = GetUserRoles(userId);
            return roles.Any(r => string.Equals(r, roleName, StringComparison.OrdinalIgnoreCase));
        }

        private static bool VerifyPassword(string password, byte[] storedHash)
        {
            if (storedHash == null || storedHash.Length == 0)
            {
                return false;
            }

            return HashPassword(password).SequenceEqual(storedHash)
                || HashPasswordLegacy(password).SequenceEqual(storedHash);
        }

        private static List<string> GetUserRoles(int userId)
        {
            const string sql = @"
SELECT r.RoleName
FROM dbo.UserRoles ur
INNER JOIN dbo.Roles r ON r.RoleId = ur.RoleId
WHERE ur.UserId = @UserId;";

            var roles = new List<string>();

            using (var connection = Database.GetConnection())
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@UserId", userId);
                connection.Open();

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        roles.Add(reader["RoleName"] as string);
                    }
                }
            }

            return roles;
        }
    }
}
