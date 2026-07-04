using System;
using System.Collections.ObjectModel;
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
        private int notMarkedStaffCount;
        private int expectedAttendanceMarks;
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
            AttendanceChartPoints = new ObservableCollection<AttendanceChartPoint>();

            selectedPeriod = ThisMonthPeriod;
            SetDateRangeForSelectedPeriod();
            LoadStats();
        }

        public IReadOnlyList<string> PeriodOptions { get; }

        public ICommand RefreshCommand { get; }

        public ICommand ApplyCustomRangeCommand { get; }

        public ObservableCollection<AttendanceChartPoint> AttendanceChartPoints { get; }

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

        public int NotMarkedStaffCount
        {
            get => notMarkedStaffCount;
            set => SetProperty(ref notMarkedStaffCount, value);
        }

        public int ExpectedAttendanceMarks
        {
            get => expectedAttendanceMarks;
            set => SetProperty(ref expectedAttendanceMarks, value);
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
            set
            {
                if (SetProperty(ref attendanceRate, value))
                {
                    OnPropertyChanged(nameof(AttendancePercentText));
                }
            }
        }

        public decimal AbsentAttendanceRate =>
            ExpectedAttendanceMarks > 0 ? (decimal)AbsentTodayCount / ExpectedAttendanceMarks : 0m;

        public decimal NotMarkedAttendanceRate =>
            ExpectedAttendanceMarks > 0 ? (decimal)NotMarkedStaffCount / ExpectedAttendanceMarks : 0m;

        public string AttendancePercentText => AttendanceRate.ToString("P0", CultureInfo.CurrentCulture);

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
                LoadAttendanceChart();
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
WHERE [Date] >= @From
  AND [Date] < @ToExclusive
  AND TeacherID IS NOT NULL;

SELECT
    SUM(CASE WHEN Status = 'Present' THEN 1 ELSE 0 END) AS PresentCount,
    SUM(CASE WHEN Status = 'Absent' THEN 1 ELSE 0 END) AS AbsentCount
FROM dbo.Attendance
WHERE [Date] >= @SummaryDate
  AND [Date] < @SummaryDateExclusive
  AND TeacherID IS NOT NULL;";

            using (var connection = Database.GetConnection())
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@From", fromDate.Date);
                command.Parameters.AddWithValue("@ToExclusive", toDate.Date.AddDays(1));
                command.Parameters.AddWithValue("@SummaryDate", toDate.Date);
                command.Parameters.AddWithValue("@SummaryDateExclusive", toDate.Date.AddDays(1));

                connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        AttendanceMarkedCount = ToInt(reader["MarkedCount"]);
                    }

                    if (reader.NextResult() && reader.Read())
                    {
                        PresentTodayCount = ToInt(reader["PresentCount"]);
                        AbsentTodayCount = ToInt(reader["AbsentCount"]);
                    }
                }
            }
        }

        private void LoadAttendanceChart()
        {
            AttendanceChartPoints.Clear();

            var chartFrom = toDate.Date.AddDays(-6);
            if (chartFrom < fromDate.Date)
            {
                chartFrom = fromDate.Date;
            }

            const string sql = @"
SELECT CAST([Date] AS DATE) AS AttendanceDate,
       SUM(CASE WHEN Status = 'Present' THEN 1 ELSE 0 END) AS PresentCount,
       SUM(CASE WHEN Status = 'Absent' THEN 1 ELSE 0 END) AS AbsentCount
FROM dbo.Attendance
WHERE [Date] >= @From
  AND [Date] < @ToExclusive
  AND TeacherID IS NOT NULL
GROUP BY CAST([Date] AS DATE);";

            var daily = new Dictionary<DateTime, DailyAttendanceCounts>();

            using (var connection = Database.GetConnection())
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@From", chartFrom);
                command.Parameters.AddWithValue("@ToExclusive", toDate.Date.AddDays(1));

                connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var date = Convert.ToDateTime(reader["AttendanceDate"]).Date;
                        daily[date] = new DailyAttendanceCounts
                        {
                            Present = ToInt(reader["PresentCount"]),
                            Absent = ToInt(reader["AbsentCount"])
                        };
                    }
                }
            }

            var maxDailyMarks = Math.Max(1, TotalTeachers);
            for (var date = chartFrom; date <= toDate.Date; date = date.AddDays(1))
            {
                daily.TryGetValue(date, out var counts);
                var present = counts?.Present ?? 0;
                var absent = counts?.Absent ?? 0;
                var notMarked = Math.Max(0, TotalTeachers - present - absent);
                AttendanceChartPoints.Add(new AttendanceChartPoint(date, present, absent, notMarked, maxDailyMarks));
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
            var totalDays = Math.Max(1, (toDate.Date - fromDate.Date).Days + 1);
            ExpectedAttendanceMarks = TotalTeachers;
            NotMarkedStaffCount = Math.Max(0, ExpectedAttendanceMarks - attendanceTotal);
            AttendanceRate = ExpectedAttendanceMarks > 0 ? (decimal)PresentTodayCount / ExpectedAttendanceMarks : 0m;
            OnPropertyChanged(nameof(AbsentAttendanceRate));
            OnPropertyChanged(nameof(NotMarkedAttendanceRate));

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

        private class DailyAttendanceCounts
        {
            public int Present { get; set; }

            public int Absent { get; set; }
        }
    }

    public class AttendanceChartPoint
    {
        private const double ChartHeight = 170;

        public AttendanceChartPoint(DateTime date, int present, int absent, int notMarked, int maxDailyMarks)
        {
            Date = date;
            Present = present;
            Absent = absent;
            NotMarked = notMarked;
            MaxDailyMarks = Math.Max(1, maxDailyMarks);
        }

        public DateTime Date { get; }

        public int Present { get; }

        public int Absent { get; }

        public int NotMarked { get; }

        public int Total => Present + Absent + NotMarked;

        public int MaxDailyMarks { get; }

        public string DayLabel => Date.ToString("ddd", CultureInfo.CurrentCulture);

        public string ShortDateLabel => Date.ToString("dd MMM", CultureInfo.CurrentCulture);

        public string ToolTipText => $"{ShortDateLabel}: Present {Present}, Absent {Absent}, Not Marked {NotMarked}";

        public double PresentHeight => GetHeight(Present);

        public double AbsentHeight => GetHeight(Absent);

        public double NotMarkedHeight => GetHeight(NotMarked);

        private double GetHeight(int value)
        {
            if (value <= 0)
            {
                return 0;
            }

            return ChartHeight * value / MaxDailyMarks;
        }
    }
}
