using System;
using System.Data.SqlClient;
using SchoolERP.Data;

namespace SchoolERP.ViewModels
{
    public class DashboardViewModel : ObservableObject
    {
        private int totalStudents;
        private int totalTeachers;
        private decimal feesPaidThisMonth;
        private decimal feesDueThisMonth;
        private int presentTodayCount;
        private int absentTodayCount;

        public DashboardViewModel()
        {
            LoadStats();
        }

        public int TotalStudents
        {
            get => totalStudents;
            set => SetProperty(ref totalStudents, value);
        }

        public int TotalTeachers
        {
            get => totalTeachers;
            set => SetProperty(ref totalTeachers, value);
        }

        public decimal FeesPaidThisMonth
        {
            get => feesPaidThisMonth;
            set => SetProperty(ref feesPaidThisMonth, value);
        }

        public decimal FeesDueThisMonth
        {
            get => feesDueThisMonth;
            set => SetProperty(ref feesDueThisMonth, value);
        }

        public int PresentTodayCount
        {
            get => presentTodayCount;
            set => SetProperty(ref presentTodayCount, value);
        }

        public int AbsentTodayCount
        {
            get => absentTodayCount;
            set => SetProperty(ref absentTodayCount, value);
        }

        private void LoadStats()
        {
            try
            {
                LoadTotalStudents();
                LoadTotalTeachers();
                LoadFeesPaidThisMonth();
                LoadFeesDueThisMonth();
                LoadPresentTodayCount();
                LoadAbsentTodayCount();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error loading stats: " + ex.Message);
            }
        }

        private void LoadTotalStudents()
        {
            const string sql = "SELECT COUNT(*) FROM dbo.Students";

            using (var connection = Database.GetConnection())
            using (var command = new SqlCommand(sql, connection))
            {
                connection.Open();
                var result = command.ExecuteScalar();
                TotalStudents = result != null ? Convert.ToInt32(result) : 0;
            }
        }

        private void LoadTotalTeachers()
        {
            const string sql = "SELECT COUNT(*) FROM dbo.Teachers";
            using (var connection = Database.GetConnection())
            using (var command = new SqlCommand(sql, connection))
            {
                connection.Open();
                var result = command.ExecuteScalar();
                TotalTeachers = result != null ? Convert.ToInt32(result) : 0;
            }
        }

        private void LoadFeesPaidThisMonth()
        {
            string currentMonth = DateTime.Now.ToString("MMM yyyy");
            const string sql = @"
SELECT ISNULL(SUM(Amount), 0) FROM dbo.Fees
WHERE Status = 'Paid' AND LTRIM(RTRIM(Month)) = @Month";

            using (var connection = Database.GetConnection())
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@Month", currentMonth);
                connection.Open();
                var result = command.ExecuteScalar();
                FeesPaidThisMonth = result != null ? Convert.ToDecimal(result) : 0m;
            }
        }

        private void LoadFeesDueThisMonth()
        {
            string currentMonth = DateTime.Now.ToString("MMM yyyy");
            const string sql = @"
SELECT ISNULL(SUM(Amount), 0) FROM dbo.Fees
WHERE Status = 'Due' AND LTRIM(RTRIM(Month)) = @Month";

            using (var connection = Database.GetConnection())
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@Month", currentMonth);
                connection.Open();
                var result = command.ExecuteScalar();
                FeesDueThisMonth = result != null ? Convert.ToDecimal(result) : 0m;
            }
        }

        private void LoadPresentTodayCount()
        {
            const string sql = @"
SELECT COUNT(*) FROM dbo.Attendance 
WHERE [Date] = CAST(GETDATE() AS DATE) AND Status='Present'";

            using (var connection = Database.GetConnection())
            using (var command = new SqlCommand(sql, connection))
            {
                connection.Open();
                var result = command.ExecuteScalar();
                PresentTodayCount = result != null ? Convert.ToInt32(result) : 0;
            }
        }

        private void LoadAbsentTodayCount()
        {
            const string sql = @"
SELECT COUNT(*) FROM dbo.Attendance
WHERE [Date] = CAST(GETDATE() AS DATE) AND Status = 'Absent'";

            using (var connection = Database.GetConnection())
            using (var command = new SqlCommand(sql, connection))
            {
                connection.Open();
                var result = command.ExecuteScalar();
                AbsentTodayCount = result != null ? Convert.ToInt32(result) : 0;
            }
        }
    }
}
