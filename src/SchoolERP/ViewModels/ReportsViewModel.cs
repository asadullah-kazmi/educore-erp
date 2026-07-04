using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Win32;
using SchoolERP.Data;
using SchoolERP.Models;
using SchoolERP.Services;

namespace SchoolERP.ViewModels
{
    public class ReportsViewModel : ObservableObject
    {
        private readonly MonthlyReportRepository _monthlyRepo = new MonthlyReportRepository();
        private readonly ExpenseRepository _expenseRepo = new ExpenseRepository();
        private readonly SalaryRepository _salaryRepo = new SalaryRepository();

        private bool _isInitialized;
        private bool _hasLoadedInitialReports;

        public ReportsViewModel()
        {
            IsAdmin = true;

            PersonTypeOptions = new List<string> { "Teacher", "Student" };

            // Initialize month options for Fee and Finance
            MonthOptions = new ObservableCollection<string>();
            FinanceMonthOptions = new ObservableCollection<string>();
            var startDate = new DateTime(2025, 1, 1);
            for (int i = 0; i < 24; i++)
            {
                var monthStr = startDate.AddMonths(i).ToString("MMM yyyy");
                MonthOptions.Add(monthStr);
                FinanceMonthOptions.Add(monthStr);
            }

            // Initialize commands FIRST before setting defaults
            GenerateFeeReportCommand = new RelayCommand(async _ => await GenerateFeeReportAsync());
            PrintFeeReportCommand = new RelayCommand(_ => PrintFeeReport(), _ => FeeRows.Count > 0);
            ExportFeeReportCsvCommand = new RelayCommand(_ => ExportFeeReportCsv(), _ => IsAdmin && FeeRows.Count > 0);

            GenerateAttendanceReportCommand = new RelayCommand(async _ => await GenerateAttendanceReportAsync());
            PrintAttendanceReportCommand = new RelayCommand(_ => PrintAttendanceReport(), _ => AttendanceRows.Count > 0);
            ExportAttendanceCsvCommand = new RelayCommand(_ => ExportAttendanceCsv(), _ => IsAdmin && AttendanceRows.Count > 0);

            GenerateFinanceReportCommand = new RelayCommand(async _ => await GenerateFinanceReportAsync());
            PrintFinanceSummaryCommand = new RelayCommand(_ => PrintFinanceSummary(), _ => FinanceSummary != null);
            PrintExpenseReportCommand = new RelayCommand(_ => PrintExpenseReport(), _ => ExpenseRows.Count > 0);
            PrintSalaryReportCommand = new RelayCommand(_ => PrintSalaryReport(), _ => SalaryRows.Count > 0);
            ExportFinanceCsvCommand = new RelayCommand(_ => ExportFinanceCsv(), _ => IsAdmin && (FinanceSummary != null || ExpenseRows.Count > 0 || SalaryRows.Count > 0));

            SelectedFeeMonth = DateTime.Now.ToString("MMM yyyy");
            AttendanceFrom = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            AttendanceTo = DateTime.Now;
            AttendancePersonType = "Teacher";
            SelectedFinanceMonth = DateTime.Now.ToString("MMM yyyy");

            _isInitialized = true;

            PrintFeeReportCommand.RaiseCanExecuteChanged();
            ExportFeeReportCsvCommand.RaiseCanExecuteChanged();
            PrintAttendanceReportCommand.RaiseCanExecuteChanged();
            ExportAttendanceCsvCommand.RaiseCanExecuteChanged();
            PrintFinanceSummaryCommand.RaiseCanExecuteChanged();
            PrintExpenseReportCommand.RaiseCanExecuteChanged();
            PrintSalaryReportCommand.RaiseCanExecuteChanged();
            ExportFinanceCsvCommand.RaiseCanExecuteChanged();
            CommandManager.InvalidateRequerySuggested();
        }

        public async Task InitializeAsync()
        {
            if (_hasLoadedInitialReports)
            {
                return;
            }

            _hasLoadedInitialReports = true;
            await GenerateFeeReportAsync().ConfigureAwait(true);
            await GenerateAttendanceReportAsync().ConfigureAwait(true);
            await GenerateFinanceReportAsync().ConfigureAwait(true);
        }

        #region Common Properties

        public bool IsAdmin { get; }

        #endregion

        #region Tab 1: Fee Collection Report

        private string _selectedFeeMonth;
        private ObservableCollection<FeeCollectionReportRow> _feeRows = new ObservableCollection<FeeCollectionReportRow>();
        private int _feeTotalStudents;
        private decimal _feeTotalCollected;
        private decimal _feeTotalDue;
        private bool _isLoadingFee;
        private string _feeStatusMessage;

        public ObservableCollection<string> MonthOptions { get; }
        public string SelectedFeeMonth
        {
            get => _selectedFeeMonth;
            set
            {
                if (SetProperty(ref _selectedFeeMonth, value))
                {
                    FeeRows.Clear();
                    FeeTotalStudents = 0;
                    FeeTotalCollected = 0;
                    FeeTotalDue = 0;
                    FeeStatusMessage = string.Empty;
                    if (_isInitialized)
                    {
                        PrintFeeReportCommand.RaiseCanExecuteChanged();
                        ExportFeeReportCsvCommand.RaiseCanExecuteChanged();
                    }
                }
            }
        }
        public ObservableCollection<FeeCollectionReportRow> FeeRows
        {
            get => _feeRows;
            set => SetProperty(ref _feeRows, value);
        }
        public int FeeTotalStudents
        {
            get => _feeTotalStudents;
            set => SetProperty(ref _feeTotalStudents, value);
        }
        public decimal FeeTotalCollected
        {
            get => _feeTotalCollected;
            set => SetProperty(ref _feeTotalCollected, value);
        }
        public decimal FeeTotalDue
        {
            get => _feeTotalDue;
            set => SetProperty(ref _feeTotalDue, value);
        }
        public bool IsLoadingFee
        {
            get => _isLoadingFee;
            set => SetProperty(ref _isLoadingFee, value);
        }
        public string FeeStatusMessage
        {
            get => _feeStatusMessage;
            set => SetProperty(ref _feeStatusMessage, value);
        }
        public RelayCommand GenerateFeeReportCommand { get; }
        public RelayCommand PrintFeeReportCommand { get; }
        public RelayCommand ExportFeeReportCsvCommand { get; }

        private async Task GenerateFeeReportAsync()
        {
            try
            {
                IsLoadingFee = true;
                FeeStatusMessage = string.Empty;

                var rows = await _monthlyRepo.GetFeeCollectionReportAsync(SelectedFeeMonth);
                FeeRows.Clear();
                foreach (var row in rows)
                {
                    FeeRows.Add(row);
                }

                FeeTotalStudents = FeeRows.Count;
                FeeTotalCollected = FeeRows.Sum(r => r.AmountPaid);
                FeeTotalDue = FeeRows.Sum(r => Math.Max(0, r.MonthlyFee - r.AmountPaid));

                FeeStatusMessage = $"Loaded {FeeRows.Count} rows for {SelectedFeeMonth}";

                PrintFeeReportCommand.RaiseCanExecuteChanged();
                ExportFeeReportCsvCommand.RaiseCanExecuteChanged();
                CommandManager.InvalidateRequerySuggested();
            }
            catch (Exception ex)
            {
                FeeStatusMessage = "Error: " + ex.Message;
            }
            finally
            {
                IsLoadingFee = false;
            }
        }

        private void PrintFeeReport()
        {
            ReportPrintService.PrintFeeCollectionReport(FeeRows.ToList(), SelectedFeeMonth);
        }

        private void ExportFeeReportCsv()
        {
            var saveFileDialog = new SaveFileDialog
            {
                Filter = "CSV|*.csv",
                FileName = $"FeeReport_{SelectedFeeMonth.Replace(" ", "_")}.csv",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
            };

            if (saveFileDialog.ShowDialog() != true) return;

            var csv = new StringBuilder();
            csv.AppendLine("Student,Reg No,Class,Monthly Fee,Paid,Status,Payment Date");
            foreach (var row in FeeRows)
            {
                csv.AppendLine($"{EscapeCsv(row.StudentName)},{EscapeCsv(row.RegistrationNo)},{EscapeCsv(row.ClassName)},{row.MonthlyFee.ToString(CultureInfo.InvariantCulture)},{row.AmountPaid.ToString(CultureInfo.InvariantCulture)},{EscapeCsv(row.Status)},{(row.PaymentDate.HasValue ? row.PaymentDate.Value.ToString("dd MMM yyyy") : "—")}");
            }

            File.WriteAllText(saveFileDialog.FileName, csv.ToString(), Encoding.UTF8);
        }

        #endregion

        #region Tab 2: Attendance Summary Report

        private DateTime _attendanceFrom;
        private DateTime _attendanceTo;
        private string _attendancePersonType;
        private ObservableCollection<AttendanceSummaryRow> _attendanceRows = new ObservableCollection<AttendanceSummaryRow>();
        private bool _isLoadingAttendance;
        private string _attendanceStatusMessage;
        private int _attendanceTotalPresent;
        private int _attendanceTotalAbsent;

        public List<string> PersonTypeOptions { get; }
        public DateTime AttendanceFrom
        {
            get => _attendanceFrom;
            set => SetProperty(ref _attendanceFrom, value);
        }
        public DateTime AttendanceTo
        {
            get => _attendanceTo;
            set => SetProperty(ref _attendanceTo, value);
        }
        public string AttendancePersonType
        {
            get => _attendancePersonType;
            set => SetProperty(ref _attendancePersonType, value);
        }
        public ObservableCollection<AttendanceSummaryRow> AttendanceRows
        {
            get => _attendanceRows;
            set => SetProperty(ref _attendanceRows, value);
        }
        public bool IsLoadingAttendance
        {
            get => _isLoadingAttendance;
            set => SetProperty(ref _isLoadingAttendance, value);
        }
        public string AttendanceStatusMessage
        {
            get => _attendanceStatusMessage;
            set => SetProperty(ref _attendanceStatusMessage, value);
        }
        public int AttendanceTotalPresent
        {
            get => _attendanceTotalPresent;
            set => SetProperty(ref _attendanceTotalPresent, value);
        }
        public int AttendanceTotalAbsent
        {
            get => _attendanceTotalAbsent;
            set => SetProperty(ref _attendanceTotalAbsent, value);
        }
        public RelayCommand GenerateAttendanceReportCommand { get; }
        public RelayCommand PrintAttendanceReportCommand { get; }
        public RelayCommand ExportAttendanceCsvCommand { get; }

        private async Task GenerateAttendanceReportAsync()
        {
            try
            {
                IsLoadingAttendance = true;
                AttendanceStatusMessage = string.Empty;

                if (AttendanceFrom > AttendanceTo)
                {
                    AttendanceStatusMessage = "From date must be before or equal to To date";
                    return;
                }

                var rows = await _monthlyRepo.GetAttendanceSummaryAsync(AttendanceFrom, AttendanceTo, AttendancePersonType);
                AttendanceRows.Clear();
                foreach (var row in rows)
                {
                    AttendanceRows.Add(row);
                }

                AttendanceTotalPresent = AttendanceRows.Sum(r => r.PresentDays);
                AttendanceTotalAbsent = AttendanceRows.Sum(r => r.AbsentDays);

                PrintAttendanceReportCommand.RaiseCanExecuteChanged();
                ExportAttendanceCsvCommand.RaiseCanExecuteChanged();
                CommandManager.InvalidateRequerySuggested();
            }
            catch (Exception ex)
            {
                AttendanceStatusMessage = ex.Message;
            }
            finally
            {
                IsLoadingAttendance = false;
            }
        }

        private void PrintAttendanceReport()
        {
            ReportPrintService.PrintAttendanceReport(AttendanceRows.ToList(), $"Attendance Summary — {AttendanceFrom:dd MMM yyyy} to {AttendanceTo:dd MMM yyyy} ({AttendancePersonType})");
        }

        private void ExportAttendanceCsv()
        {
            var saveFileDialog = new SaveFileDialog
            {
                Filter = "CSV|*.csv",
                FileName = $"AttendanceReport_{AttendanceFrom:yyyyMMdd}_{AttendanceTo:yyyyMMdd}.csv",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
            };

            if (saveFileDialog.ShowDialog() != true) return;

            var csv = new StringBuilder();
            csv.AppendLine("Name,Present Days,Absent Days,Total Days,Attendance %");
            foreach (var row in AttendanceRows)
            {
                csv.AppendLine($"{EscapeCsv(row.Name)},{row.PresentDays},{row.AbsentDays},{row.TotalDays},{row.AttendancePercent.ToString("0.0", CultureInfo.InvariantCulture)}%");
            }

            File.WriteAllText(saveFileDialog.FileName, csv.ToString(), Encoding.UTF8);
        }

        #endregion

        #region Tab 3: Finance Summary Report

        private ObservableCollection<string> _financeMonthOptions;
        private string _selectedFinanceMonth;
        private MonthlyFinanceSummary _financeSummary;
        private ObservableCollection<Expense> _expenseRows = new ObservableCollection<Expense>();
        private ObservableCollection<SalaryPayment> _salaryRows = new ObservableCollection<SalaryPayment>();
        private bool _isLoadingFinance;
        private string _financeStatusMessage;

        public ObservableCollection<string> FinanceMonthOptions
        {
            get => _financeMonthOptions;
            set => SetProperty(ref _financeMonthOptions, value);
        }
        public string SelectedFinanceMonth
        {
            get => _selectedFinanceMonth;
            set
            {
                if (SetProperty(ref _selectedFinanceMonth, value))
                {
                    ExpenseRows.Clear();
                    SalaryRows.Clear();
                    FinanceSummary = null;
                    FinanceStatusMessage = string.Empty;
                    if (_isInitialized)
                    {
                        PrintExpenseReportCommand.RaiseCanExecuteChanged();
                        PrintSalaryReportCommand.RaiseCanExecuteChanged();
                        PrintFinanceSummaryCommand.RaiseCanExecuteChanged();
                        ExportFinanceCsvCommand.RaiseCanExecuteChanged();
                    }
                }
            }
        }
        public MonthlyFinanceSummary FinanceSummary
        {
            get => _financeSummary;
            set
            {
                if (SetProperty(ref _financeSummary, value))
                {
                    OnPropertyChanged(nameof(FinanceSurplusColor));
                }
            }
        }
        public ObservableCollection<Expense> ExpenseRows
        {
            get => _expenseRows;
            set => SetProperty(ref _expenseRows, value);
        }
        public ObservableCollection<SalaryPayment> SalaryRows
        {
            get => _salaryRows;
            set => SetProperty(ref _salaryRows, value);
        }
        public bool IsLoadingFinance
        {
            get => _isLoadingFinance;
            set => SetProperty(ref _isLoadingFinance, value);
        }
        public string FinanceStatusMessage
        {
            get => _financeStatusMessage;
            set => SetProperty(ref _financeStatusMessage, value);
        }
        public string FinanceSurplusColor
        {
            get
            {
                if (FinanceSummary == null) return "#64748B";
                return FinanceSummary.NetSurplus >= 0 ? "#10B981" : "#EF4444";
            }
        }
        public RelayCommand GenerateFinanceReportCommand { get; }
        public RelayCommand PrintFinanceSummaryCommand { get; }
        public RelayCommand PrintExpenseReportCommand { get; }
        public RelayCommand PrintSalaryReportCommand { get; }
        public RelayCommand ExportFinanceCsvCommand { get; }

        private async Task GenerateFinanceReportAsync()
        {
            try
            {
                IsLoadingFinance = true;
                FinanceStatusMessage = string.Empty;

                FinanceSummary = await _monthlyRepo.GetMonthlySummaryAsync(SelectedFinanceMonth);

                var expenses = await _expenseRepo.GetAllExpensesAsync(SelectedFinanceMonth);
                ExpenseRows.Clear();
                foreach (var expense in expenses)
                {
                    ExpenseRows.Add(expense);
                }

                var salaries = await _salaryRepo.GetAllPaymentsAsync(SelectedFinanceMonth);
                SalaryRows.Clear();
                foreach (var salary in salaries)
                {
                    SalaryRows.Add(salary);
                }

                FinanceStatusMessage = $"Loaded {ExpenseRows.Count} expenses and {SalaryRows.Count} salary payments for {SelectedFinanceMonth}";

                PrintFinanceSummaryCommand.RaiseCanExecuteChanged();
                PrintExpenseReportCommand.RaiseCanExecuteChanged();
                PrintSalaryReportCommand.RaiseCanExecuteChanged();
                ExportFinanceCsvCommand.RaiseCanExecuteChanged();
                CommandManager.InvalidateRequerySuggested();
            }
            catch (Exception ex)
            {
                FinanceStatusMessage = "Error: " + ex.Message;
            }
            finally
            {
                IsLoadingFinance = false;
            }
        }

        private void PrintExpenseReport()
        {
            ReportPrintService.PrintExpenseReport(ExpenseRows.ToList(), SelectedFinanceMonth);
        }

        private void PrintFinanceSummary()
        {
            if (FinanceSummary != null)
            {
                ReportPrintService.PrintFinanceSummaryReport(FinanceSummary);
            }
        }

        private void PrintSalaryReport()
        {
            ReportPrintService.PrintSalaryReport(SalaryRows.ToList(), SelectedFinanceMonth);
        }

        private void ExportFinanceCsv()
        {
            var saveFileDialog = new SaveFileDialog
            {
                Filter = "CSV|*.csv",
                FileName = $"FinanceReport_{SelectedFinanceMonth.Replace(" ", "_")}.csv",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
            };

            if (saveFileDialog.ShowDialog() != true) return;
            if (FinanceSummary == null) return;

            var csv = new StringBuilder();
            csv.AppendLine("Finance Summary");
            csv.AppendLine($"Month,{FinanceSummary.Month}");
            csv.AppendLine($"Fees Collected,{FinanceSummary.TotalFeesCollected.ToString(CultureInfo.InvariantCulture)}");
            csv.AppendLine($"Total Expenses,{FinanceSummary.TotalExpenses.ToString(CultureInfo.InvariantCulture)}");
            csv.AppendLine($"Total Salaries Paid,{FinanceSummary.TotalSalariesPaid.ToString(CultureInfo.InvariantCulture)}");
            csv.AppendLine($"Net Surplus,{FinanceSummary.NetSurplus.ToString(CultureInfo.InvariantCulture)}");
            csv.AppendLine();

            csv.AppendLine("Expenses Detail");
            csv.AppendLine("Date,Category,Amount,Notes");
            foreach (var expense in ExpenseRows)
            {
                csv.AppendLine($"{expense.Date:dd MMM yyyy},{EscapeCsv(expense.Category)},{expense.Amount.ToString(CultureInfo.InvariantCulture)},{EscapeCsv(expense.Notes)}");
            }
            csv.AppendLine();

            csv.AppendLine("Salary Payments Detail");
            csv.AppendLine("Teacher,Designation,Amount,Payment Date,Notes");
            foreach (var salary in SalaryRows)
            {
                csv.AppendLine($"{EscapeCsv(salary.TeacherName)},{EscapeCsv(salary.Designation)},{salary.Amount.ToString(CultureInfo.InvariantCulture)},{salary.PaymentDate:dd MMM yyyy},{EscapeCsv(salary.Notes)}");
            }

            File.WriteAllText(saveFileDialog.FileName, csv.ToString(), Encoding.UTF8);
        }

        #endregion

        #region Helpers

        private string EscapeCsv(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            if (value.Contains(",") || value.Contains("\"") || value.Contains("\n"))
            {
                return $"\"{value.Replace("\"", "\"\"")}\"";
            }
            return value;
        }

        #endregion
    }
}
