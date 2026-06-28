using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Windows.Input;
using SchoolERP.Data;

namespace SchoolERP.ViewModels
{
    public class DashboardViewModel : ObservableObject
    {
        private static bool feePaymentColumnsEnsured;

        private const string TodayPeriod = "Today";
        private const string LastSevenDaysPeriod = "Last 7 Days";
        private const string LastThirtyDaysPeriod = "Last 30 Days";
        private const string ThisMonthPeriod = "This Month";
        private const string LastMonthPeriod = "Last Month";
        private const string CustomPeriod = "Custom Period";

        private int totalStudents;
        private int totalTeachers;
        private decimal feesPaidThisMonth;
        private decimal feesDueThisMonth;
        private int presentTodayCount;
        private int absentTodayCount;
        private decimal totalExpenses;
        private decimal salariesPaid;
        private decimal netBalance;
        private int newAdmissions;
        private int attendanceMarkedCount;
        private decimal collectionRate;
        private decimal attendanceRate;
        private decimal averageDailyCollection;
        private string selectedPeriod;
        private DateTime? customFromDate;
        private DateTime? customToDate;
        private DateTime fromDate;
        private DateTime toDate;
        private string dateRangeLabel;
        private string statusMessage;
        private bool isBusy;

        public DashboardViewModel()
        {
            PeriodOptions = new List<string>
            {
                TodayPeriod,
                LastSevenDaysPeriod,
                LastThirtyDaysPeriod,
                ThisMonthPeriod,
                LastMonthPeriod,
                CustomPeriod
            };

            RefreshCommand = new RelayCommand(_ => LoadStats(), _ => !IsBusy);
            ApplyCustomRangeCommand = new RelayCommand(_ => ApplyCustomRange(), _ => !IsBusy && IsCustomPeriod);

            selectedPeriod = ThisMonthPeriod;
            SetDateRangeForSelectedPeriod();
            LoadStats();
        }

        public IReadOnlyList<string> PeriodOptions { get; }

        public ICommand RefreshCommand { get; }

        public ICommand ApplyCustomRangeCommand { get; }

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
            set
            {
                if (SetProperty(ref feesPaidThisMonth, value))
                {
                    OnPropertyChanged(nameof(FeesCollectedLabel));
                }
            }
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

        public decimal TotalExpenses
        {
            get => totalExpenses;
            set => SetProperty(ref totalExpenses, value);
        }

        public decimal SalariesPaid
        {
            get => salariesPaid;
            set => SetProperty(ref salariesPaid, value);
        }

        public decimal NetBalance
        {
            get => netBalance;
            set => SetProperty(ref netBalance, value);
        }

        public int NewAdmissions
        {
            get => newAdmissions;
            set => SetProperty(ref newAdmissions, value);
        }

        public int AttendanceMarkedCount
        {
            get => attendanceMarkedCount;
            set => SetProperty(ref attendanceMarkedCount, value);
        }

        public decimal CollectionRate
        {
            get => collectionRate;
            set => SetProperty(ref collectionRate, value);
        }

        public decimal AttendanceRate
        {
            get => attendanceRate;
            set => SetProperty(ref attendanceRate, value);
        }

        public decimal AverageDailyCollection
        {
            get => averageDailyCollection;
            set => SetProperty(ref averageDailyCollection, value);
        }

        public string SelectedPeriod
        {
            get => selectedPeriod;
            set
            {
                if (SetProperty(ref selectedPeriod, value))
                {
                    OnPropertyChanged(nameof(IsCustomPeriod));
                    (ApplyCustomRangeCommand as RelayCommand)?.RaiseCanExecuteChanged();

                    if (!IsCustomPeriod)
                    {
                        SetDateRangeForSelectedPeriod();
                        LoadStats();
                    }
                }
            }
        }

        public DateTime? CustomFromDate
        {
            get => customFromDate;
            set
            {
                if (SetProperty(ref customFromDate, value))
                {
                    (ApplyCustomRangeCommand as RelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        public DateTime? CustomToDate
        {
            get => customToDate;
            set
            {
                if (SetProperty(ref customToDate, value))
                {
                    (ApplyCustomRangeCommand as RelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        public string DateRangeLabel
        {
            get => dateRangeLabel;
            set => SetProperty(ref dateRangeLabel, value);
        }

        public string StatusMessage
        {
            get => statusMessage;
            set => SetProperty(ref statusMessage, value);
        }

        public bool IsBusy
        {
            get => isBusy;
            set
            {
                if (SetProperty(ref isBusy, value))
                {
                    (RefreshCommand as RelayCommand)?.RaiseCanExecuteChanged();
                    (ApplyCustomRangeCommand as RelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        public bool IsCustomPeriod => SelectedPeriod == CustomPeriod;

        public string FeesCollectedLabel => SelectedPeriod == ThisMonthPeriod
            ? "Fee Collected (Month)"
            : "Fee Collected";

        private void ApplyCustomRange()
        {
            var from = CustomFromDate ?? DateTime.Today;
            var to = CustomToDate ?? from;

            if (from.Date > to.Date)
            {
                var swap = from;
                from = to;
                to = swap;
            }

            fromDate = from.Date;
            toDate = to.Date;
            CustomFromDate = fromDate;
            CustomToDate = toDate;
            UpdateDateRangeLabel();
            LoadStats();
        }

        private void SetDateRangeForSelectedPeriod()
        {
            var today = DateTime.Today;

            switch (SelectedPeriod)
            {
                case TodayPeriod:
                    fromDate = today;
                    toDate = today;
                    break;
                case LastSevenDaysPeriod:
                    fromDate = today.AddDays(-6);
                    toDate = today;
                    break;
                case LastThirtyDaysPeriod:
                    fromDate = today.AddDays(-29);
                    toDate = today;
                    break;
                case LastMonthPeriod:
                    var firstOfThisMonth = new DateTime(today.Year, today.Month, 1);
                    fromDate = firstOfThisMonth.AddMonths(-1);
                    toDate = firstOfThisMonth.AddDays(-1);
                    break;
                case ThisMonthPeriod:
                default:
                    fromDate = new DateTime(today.Year, today.Month, 1);
                    toDate = today;
                    break;
            }

            CustomFromDate = fromDate;
            CustomToDate = toDate;
            UpdateDateRangeLabel();
        }

        private void LoadStats()
        {
            IsBusy = true;
            StatusMessage = string.Empty;

            try
            {
                LoadOverviewStats();
                LoadFinanceStats();
                LoadAttendanceStats();
                LoadAdmissionsStats();
                UpdateDerivedStats();
                UpdateDateRangeLabel();
            }
            catch (Exception ex)
            {
                StatusMessage = "Unable to refresh dashboard: " + ex.Message;
                System.Diagnostics.Debug.WriteLine("Error loading stats: " + ex.Message);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void LoadOverviewStats()
        {
            const string sql = @"
SELECT
    (SELECT COUNT(*) FROM dbo.Students) AS TotalStudents,
    (SELECT COUNT(*) FROM dbo.Teachers) AS TotalTeachers;";

            using (var connection = Database.GetConnection())
            using (var command = new SqlCommand(sql, connection))
            {
                connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        TotalStudents = reader["TotalStudents"] == DBNull.Value ? 0 : Convert.ToInt32(reader["TotalStudents"]);
                        TotalTeachers = reader["TotalTeachers"] == DBNull.Value ? 0 : Convert.ToInt32(reader["TotalTeachers"]);
                    }
                }
            }
        }

        private void LoadFinanceStats()
        {
            EnsureFeePaymentColumns();

            var monthNames = GetMonthNamesInRange();
            var monthFilter = BuildMonthFilter(monthNames);

            var sql = @"
SELECT
    (SELECT ISNULL(SUM(ISNULL(PaidAmount, CASE WHEN Status = 'Paid' THEN Amount ELSE 0 END)), 0)
     FROM dbo.Fees
     WHERE ISNULL(PaidAmount, CASE WHEN Status = 'Paid' THEN Amount ELSE 0 END) > 0
       AND (
           (PaymentDate IS NOT NULL AND CAST(PaymentDate AS DATE) BETWEEN @From AND @To)
           OR (PaymentDate IS NULL AND " + monthFilter.Condition + @")
       )) AS FeesCollected,
    (SELECT ISNULL(SUM(
        CASE
            WHEN Amount - ISNULL(PaidAmount, CASE WHEN Status = 'Paid' THEN Amount ELSE 0 END) > 0
            THEN Amount - ISNULL(PaidAmount, CASE WHEN Status = 'Paid' THEN Amount ELSE 0 END)
            ELSE 0
        END), 0)
     FROM dbo.Fees
     WHERE " + monthFilter.Condition + @") AS FeesOutstanding,
    (SELECT ISNULL(SUM(Amount), 0)
     FROM dbo.Expenses
     WHERE [Date] BETWEEN @From AND @To) AS Expenses,
    (SELECT ISNULL(SUM(Amount), 0)
     FROM dbo.SalaryPayments
     WHERE PaymentDate BETWEEN @From AND @To) AS Salaries;";

            using (var connection = Database.GetConnection())
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@From", fromDate.Date);
                command.Parameters.AddWithValue("@To", toDate.Date);
                AddMonthParameters(command, monthNames);

                connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        FeesPaidThisMonth = ToDecimal(reader["FeesCollected"]);
                        FeesDueThisMonth = ToDecimal(reader["FeesOutstanding"]);
                        TotalExpenses = ToDecimal(reader["Expenses"]);
                        SalariesPaid = ToDecimal(reader["Salaries"]);
                    }
                }
            }
        }

        private static void EnsureFeePaymentColumns()
        {
            if (feePaymentColumnsEnsured)
            {
                return;
            }

            const string sql = @"
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Fees') AND name = 'PaidAmount')
BEGIN
    ALTER TABLE dbo.Fees ADD PaidAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_Fees_PaidAmount DEFAULT 0;
    EXEC('UPDATE dbo.Fees SET PaidAmount = Amount WHERE Status = ''Paid''');
END
EXEC('UPDATE dbo.Fees
SET Status =
    CASE
        WHEN ISNULL(PaidAmount, 0) >= Amount THEN ''Paid''
        WHEN ISNULL(PaidAmount, 0) > 0 THEN ''Partial''
        ELSE ''Due''
    END');";

            using (var connection = Database.GetConnection())
            using (var command = new SqlCommand(sql, connection))
            {
                connection.Open();
                command.ExecuteNonQuery();
                feePaymentColumnsEnsured = true;
            }
        }

        private void LoadAttendanceStats()
        {
            const string sql = @"
SELECT
    SUM(CASE WHEN Status = 'Present' THEN 1 ELSE 0 END) AS PresentCount,
    SUM(CASE WHEN Status = 'Absent' THEN 1 ELSE 0 END) AS AbsentCount,
    COUNT(*) AS MarkedCount
FROM dbo.Attendance
WHERE [Date] BETWEEN @From AND @To
  AND TeacherID IS NOT NULL;";

            using (var connection = Database.GetConnection())
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@From", fromDate.Date);
                command.Parameters.AddWithValue("@To", toDate.Date);

                connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        PresentTodayCount = ToInt(reader["PresentCount"]);
                        AbsentTodayCount = ToInt(reader["AbsentCount"]);
                        AttendanceMarkedCount = ToInt(reader["MarkedCount"]);
                    }
                }
            }
        }

        private void LoadAdmissionsStats()
        {
            const string sql = @"
SELECT COUNT(*)
FROM dbo.Students
WHERE AdmissionDate BETWEEN @From AND @To;";

            using (var connection = Database.GetConnection())
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@From", fromDate.Date);
                command.Parameters.AddWithValue("@To", toDate.Date);
                connection.Open();
                NewAdmissions = ToInt(command.ExecuteScalar());
            }
        }

        private void UpdateDerivedStats()
        {
            NetBalance = FeesPaidThisMonth - TotalExpenses - SalariesPaid;

            var expectedFees = FeesPaidThisMonth + FeesDueThisMonth;
            CollectionRate = expectedFees > 0 ? FeesPaidThisMonth / expectedFees : 0m;

            var attendanceTotal = PresentTodayCount + AbsentTodayCount;
            AttendanceRate = attendanceTotal > 0 ? (decimal)PresentTodayCount / attendanceTotal : 0m;

            var totalDays = Math.Max(1, (toDate.Date - fromDate.Date).Days + 1);
            AverageDailyCollection = FeesPaidThisMonth / totalDays;
        }

        private void UpdateDateRangeLabel()
        {
            DateRangeLabel = fromDate.Date == toDate.Date
                ? fromDate.ToString("dd MMM yyyy", CultureInfo.CurrentCulture)
                : fromDate.ToString("dd MMM yyyy", CultureInfo.CurrentCulture) + " - " + toDate.ToString("dd MMM yyyy", CultureInfo.CurrentCulture);

            OnPropertyChanged(nameof(FeesCollectedLabel));
        }

        private List<string> GetMonthNamesInRange()
        {
            var months = new List<string>();
            var cursor = new DateTime(fromDate.Year, fromDate.Month, 1);
            var end = new DateTime(toDate.Year, toDate.Month, 1);

            while (cursor <= end)
            {
                months.Add(cursor.ToString("MMM yyyy", CultureInfo.CurrentCulture));
                cursor = cursor.AddMonths(1);
            }

            return months;
        }

        private static MonthFilter BuildMonthFilter(IReadOnlyList<string> months)
        {
            var parameters = Enumerable.Range(0, months.Count).Select(i => "@Month" + i).ToList();
            return new MonthFilter("LTRIM(RTRIM(Month)) IN (" + string.Join(",", parameters) + ")");
        }

        private static void AddMonthParameters(SqlCommand command, IReadOnlyList<string> months)
        {
            for (var i = 0; i < months.Count; i++)
            {
                command.Parameters.AddWithValue("@Month" + i, months[i]);
            }
        }

        private static decimal ToDecimal(object value)
        {
            return value == null || value == DBNull.Value ? 0m : Convert.ToDecimal(value);
        }

        private static int ToInt(object value)
        {
            return value == null || value == DBNull.Value ? 0 : Convert.ToInt32(value);
        }

        private class MonthFilter
        {
            public MonthFilter(string condition)
            {
                Condition = condition;
            }

            public string Condition { get; }
        }
    }
}
