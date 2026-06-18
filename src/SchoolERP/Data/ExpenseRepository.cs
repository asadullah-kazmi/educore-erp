using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;
using SchoolERP.Models;

namespace SchoolERP.Data
{
    public class ExpenseRepository
    {
        private const string SelectBase = @"
SELECT ExpenseID, Category, Amount, [Date], Notes
FROM dbo.Expenses";

        public async Task<List<Expense>> GetAllExpensesAsync(string month = null, string category = null)
        {
            var sql = SelectBase;
            var conditions = new List<string>();

            if (!string.IsNullOrEmpty(month))
            {
                conditions.Add("FORMAT([Date], 'MMM yyyy') = @Month");
            }

            if (!string.IsNullOrEmpty(category) && category != "All Categories")
            {
                conditions.Add("Category = @Category");
            }

            if (conditions.Count > 0)
            {
                sql += " WHERE " + string.Join(" AND ", conditions);
            }

            sql += " ORDER BY [Date] DESC;";

            var expenses = new List<Expense>();

            using (var connection = Database.GetConnection())
            using (var command = new SqlCommand(sql, connection))
            {
                if (!string.IsNullOrEmpty(month))
                {
                    command.Parameters.AddWithValue("@Month", month);
                }

                if (!string.IsNullOrEmpty(category) && category != "All Categories")
                {
                    command.Parameters.AddWithValue("@Category", category);
                }

                await connection.OpenAsync().ConfigureAwait(false);

                using (var reader = await command.ExecuteReaderAsync().ConfigureAwait(false))
                {
                    while (await reader.ReadAsync().ConfigureAwait(false))
                    {
                        expenses.Add(MapExpense(reader));
                    }
                }
            }

            return expenses;
        }

        public async Task<Expense> GetExpenseByIdAsync(int id)
        {
            var sql = SelectBase + " WHERE ExpenseID = @ExpenseID;";

            using (var connection = Database.GetConnection())
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@ExpenseID", id);
                await connection.OpenAsync().ConfigureAwait(false);

                using (var reader = await command.ExecuteReaderAsync().ConfigureAwait(false))
                {
                    if (await reader.ReadAsync().ConfigureAwait(false))
                    {
                        return MapExpense(reader);
                    }
                }
            }

            return null;
        }

        public async Task<bool> AddExpenseAsync(Expense expense)
        {
            if (expense == null)
            {
                throw new ArgumentNullException(nameof(expense));
            }

            const string sql = @"
INSERT INTO dbo.Expenses (Category, Amount, [Date], Notes)
VALUES (@Category, @Amount, @Date, @Notes);";

            using (var connection = Database.GetConnection())
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@Category", (object)expense.Category ?? DBNull.Value);
                command.Parameters.AddWithValue("@Amount", expense.Amount);
                command.Parameters.AddWithValue("@Date", expense.Date);
                command.Parameters.AddWithValue("@Notes", (object)expense.Notes ?? DBNull.Value);

                await connection.OpenAsync().ConfigureAwait(false);
                return await command.ExecuteNonQueryAsync().ConfigureAwait(false) > 0;
            }
        }

        public async Task<bool> UpdateExpenseAsync(Expense expense)
        {
            if (expense == null)
            {
                throw new ArgumentNullException(nameof(expense));
            }

            const string sql = @"
UPDATE dbo.Expenses
SET Category = @Category,
    Amount = @Amount,
    [Date] = @Date,
    Notes = @Notes
WHERE ExpenseID = @ExpenseID;";

            using (var connection = Database.GetConnection())
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@ExpenseID", expense.ExpenseID);
                command.Parameters.AddWithValue("@Category", (object)expense.Category ?? DBNull.Value);
                command.Parameters.AddWithValue("@Amount", expense.Amount);
                command.Parameters.AddWithValue("@Date", expense.Date);
                command.Parameters.AddWithValue("@Notes", (object)expense.Notes ?? DBNull.Value);

                await connection.OpenAsync().ConfigureAwait(false);
                return await command.ExecuteNonQueryAsync().ConfigureAwait(false) > 0;
            }
        }

        public async Task<bool> DeleteExpenseAsync(int id)
        {
            const string sql = "DELETE FROM dbo.Expenses WHERE ExpenseID = @ExpenseID;";

            using (var connection = Database.GetConnection())
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@ExpenseID", id);
                await connection.OpenAsync().ConfigureAwait(false);
                return await command.ExecuteNonQueryAsync().ConfigureAwait(false) > 0;
            }
        }

        public async Task<decimal> GetTotalExpensesAsync(string month)
        {
            const string sql = @"
SELECT ISNULL(SUM(Amount), 0)
FROM dbo.Expenses
WHERE FORMAT([Date], 'MMM yyyy') = @Month;";

            using (var connection = Database.GetConnection())
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@Month", month ?? (object)DBNull.Value);
                await connection.OpenAsync().ConfigureAwait(false);
                var result = await command.ExecuteScalarAsync().ConfigureAwait(false);
                return result == null || result == DBNull.Value ? 0m : Convert.ToDecimal(result);
            }
        }

        public async Task<List<string>> GetCategoriesAsync()
        {
            const string sql = @"
SELECT DISTINCT Category
FROM dbo.Expenses
WHERE Category IS NOT NULL
ORDER BY Category;";

            var categories = new List<string>();

            using (var connection = Database.GetConnection())
            using (var command = new SqlCommand(sql, connection))
            {
                await connection.OpenAsync().ConfigureAwait(false);

                using (var reader = await command.ExecuteReaderAsync().ConfigureAwait(false))
                {
                    while (await reader.ReadAsync().ConfigureAwait(false))
                    {
                        var category = reader["Category"] as string;
                        if (category != null)
                        {
                            categories.Add(category);
                        }
                    }
                }
            }

            return categories;
        }

        private static Expense MapExpense(SqlDataReader reader)
        {
            return new Expense
            {
                ExpenseID = reader.GetInt32(reader.GetOrdinal("ExpenseID")),
                Category = reader["Category"] as string,
                Amount = reader["Amount"] == DBNull.Value ? 0m : Convert.ToDecimal(reader["Amount"]),
                Date = reader.GetDateTime(reader.GetOrdinal("Date")),
                Notes = reader["Notes"] as string
            };
        }
    }
}
