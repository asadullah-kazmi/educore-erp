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

        private const string FinancePeriodToday = "Today";
        private const string FinancePeriodLastSevenDays = "Last 7 Days";
        private const string FinancePeriodLastThirtyDays = "Last 30 Days";
        private const string FinancePeriodThisMonth = "This Month";
        private const string FinancePeriodLastMonth = "Last Month";
        private const string FinancePeriodCustomRange = "Custom Range";

        private bool _isInitialized;
        private bool _hasLoadedInitialReports;

        public ReportsViewModel()
        {
            IsAdmin = true;

            PersonTypeOptions = new List<string> { "Teacher", "Student" };
            FeeClassOptions.Add("All Classes");
            FeeSectionOptions.Add("All Sections");

            // Initialize month options for Fee and Finance
            MonthOptions = new ObservableCollection<string>();
            FinanceMonthOptions = new ObservableCollection<string>();
            FinancePeriodOptions = new ObservableCollection<string>
            {
                FinancePeriodToday,
                FinancePeriodLastSevenDays,
                FinancePeriodLastThirtyDays,
                FinancePeriodThisMonth,
                FinancePeriodLastMonth,
                FinancePeriodCustomRange
            };

            var startDate = new DateTime(2025, 1, 1);
            for (int i = 0; i < 24; i++)
            {
                var monthStr = startDate.AddMonths(i).ToString("MMM yyyy");
                MonthOptions.Add(monthStr);
                FinanceMonthOptions.Add(monthStr);
                FinancePeriodOptions.Add(monthStr);
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
            SelectedFinancePeriod = SelectedFinanceMonth;
            FinanceCustomFrom = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            FinanceCustomTo = DateTime.Now;

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
        private readonly List<FeeCollectionReportRow> _allFeeRows = new List<FeeCollectionReportRow>();
        private ObservableCollection<FeeCollectionReportRow> _feeRows = new ObservableCollection<FeeCollectionReportRow>();
        private ObservableCollection<string> _feeClassOptions = new ObservableCollection<string>();
        private ObservableCollection<string> _feeSectionOptions = new ObservableCollection<string>();
        private ObservableCollection<string> _feeStatusOptions = new ObservableCollection<string> { "All Statuses", "Paid", "Partial", "Due" };
        private string _selectedFeeClass = "All Classes";
        private string _selectedFeeSection = "All Sections";
        private string _selectedFeeStatus = "All Statuses";
        private string _feeSearchText;
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
                    _allFeeRows.Clear();
                    FeeClassOptions.Clear();
                    FeeClassOptions.Add("All Classes");
                    FeeSectionOptions.Clear();
                    FeeSectionOptions.Add("All Sections");
                    SelectedFeeClass = "All Classes";
                    SelectedFeeSection = "All Sections";
                    SelectedFeeStatus = "All Statuses";
                    FeeSearchText = string.Empty;
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
        public ObservableCollection<string> FeeClassOptions
        {
            get => _feeClassOptions;
            set => SetProperty(ref _feeClassOptions, value);
        }
        public ObservableCollection<string> FeeStatusOptions
        {
            get => _feeStatusOptions;
            set => SetProperty(ref _feeStatusOptions, value);
        }
        public ObservableCollection<string> FeeSectionOptions
        {
            get => _feeSectionOptions;
            set => SetProperty(ref _feeSectionOptions, value);
        }
        public string SelectedFeeClass
        {
            get => _selectedFeeClass;
            set
            {
                if (SetProperty(ref _selectedFeeClass, value))
                {
                    ApplyFeeFilters();
                }
            }
        }
        public string SelectedFeeSection
        {
            get => _selectedFeeSection;
            set
            {
                if (SetProperty(ref _selectedFeeSection, value))
                {
                    ApplyFeeFilters();
                }
            }
        }
        public string SelectedFeeStatus
        {
            get => _selectedFeeStatus;
            set
            {
                if (SetProperty(ref _selectedFeeStatus, value))
                {
                    ApplyFeeFilters();
                }
            }
        }
        public string FeeSearchText
        {
            get => _feeSearchText;
            set
            {
                if (SetProperty(ref _feeSearchText, value))
                {
                    ApplyFeeFilters();
                }
            }
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
                _allFeeRows.Clear();
                _allFeeRows.AddRange(rows);
                RefreshFeeClassOptions();
                RefreshFeeSectionOptions();
                ApplyFeeFilters();

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

        private void RefreshFeeClassOptions()
        {
            var selected = SelectedFeeClass;
            FeeClassOptions.Clear();
            FeeClassOptions.Add("All Classes");
            foreach (var className in _allFeeRows
                .Select(r => string.IsNullOrWhiteSpace(r.ClassName) ? "Unassigned" : r.ClassName.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(c => c))
            {
                FeeClassOptions.Add(className);
            }

            SelectedFeeClass = FeeClassOptions.Contains(selected) ? selected : "All Classes";
        }

        private void RefreshFeeSectionOptions()
        {
            var selected = SelectedFeeSection;
            FeeSectionOptions.Clear();
            FeeSectionOptions.Add("All Sections");
            foreach (var section in _allFeeRows
                .Select(r => string.IsNullOrWhiteSpace(r.Section) ? "Unassigned" : r.Section.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(s => s))
            {
                FeeSectionOptions.Add(section);
            }

            SelectedFeeSection = FeeSectionOptions.Contains(selected) ? selected : "All Sections";
        }

        private void ApplyFeeFilters()
        {
            var query = _allFeeRows.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(SelectedFeeClass) && SelectedFeeClass != "All Classes")
            {
                query = query.Where(r => string.Equals(
                    string.IsNullOrWhiteSpace(r.ClassName) ? "Unassigned" : r.ClassName.Trim(),
                    SelectedFeeClass,
                    StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(SelectedFeeSection) && SelectedFeeSection != "All Sections")
            {
                query = query.Where(r => string.Equals(
                    string.IsNullOrWhiteSpace(r.Section) ? "Unassigned" : r.Section.Trim(),
                    SelectedFeeSection,
                    StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(SelectedFeeStatus) && SelectedFeeStatus != "All Statuses")
            {
                query = query.Where(r => string.Equals(r.Status, SelectedFeeStatus, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(FeeSearchText))
            {
                var search = FeeSearchText.Trim();
                query = query.Where(r =>
                    ContainsText(r.StudentName, search) ||
                    ContainsText(r.RegistrationNo, search) ||
                    ContainsText(r.ClassName, search) ||
                    ContainsText(r.Section, search));
            }

            FeeRows.Clear();
            foreach (var row in query)
            {
                FeeRows.Add(row);
            }

            FeeTotalStudents = FeeRows.Count;
            FeeTotalCollected = FeeRows.Sum(r => r.AmountPaid);
            FeeTotalDue = FeeRows.Sum(r => Math.Max(0, r.MonthlyFee - r.AmountPaid));
            FeeStatusMessage = $"Showing {FeeRows.Count} of {_allFeeRows.Count} rows for {SelectedFeeMonth}";

            PrintFeeReportCommand.RaiseCanExecuteChanged();
            ExportFeeReportCsvCommand.RaiseCanExecuteChanged();
            CommandManager.InvalidateRequerySuggested();
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
            csv.AppendLine("Student,Reg No,Class,Section,Monthly Fee,Paid,Status,Payment Date");
            foreach (var row in FeeRows)
            {
                csv.AppendLine($"{EscapeCsv(row.StudentName)},{EscapeCsv(row.RegistrationNo)},{EscapeCsv(row.ClassName)},{EscapeCsv(row.Section)},{row.MonthlyFee.ToString(CultureInfo.InvariantCulture)},{row.AmountPaid.ToString(CultureInfo.InvariantCulture)},{EscapeCsv(row.Status)},{(row.PaymentDate.HasValue ? row.PaymentDate.Value.ToString("dd MMM yyyy") : "-")}");
            }

            File.WriteAllText(saveFileDialog.FileName, csv.ToString(), Encoding.UTF8);
        }

        #endregion

        #region Tab 2: Attendance Summary Report

        private DateTime _attendanceFrom;
        private DateTime _attendanceTo;
        private string _attendancePersonType;
        private readonly List<AttendanceSummaryRow> _allAttendanceRows = new List<AttendanceSummaryRow>();
        private ObservableCollection<AttendanceSummaryRow> _attendanceRows = new ObservableCollection<AttendanceSummaryRow>();
        private ObservableCollection<string> _attendanceRateOptions = new ObservableCollection<string> { "All Rates", "75% and above", "Below 75%", "No Records" };
        private string _selectedAttendanceRate = "All Rates";
        private string _attendanceSearchText;
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
            set
            {
                if (SetProperty(ref _attendancePersonType, value))
                {
                    AttendanceSearchText = string.Empty;
                    SelectedAttendanceRate = "All Rates";
                }
            }
        }
        public ObservableCollection<AttendanceSummaryRow> AttendanceRows
        {
            get => _attendanceRows;
            set => SetProperty(ref _attendanceRows, value);
        }
        public ObservableCollection<string> AttendanceRateOptions
        {
            get => _attendanceRateOptions;
            set => SetProperty(ref _attendanceRateOptions, value);
        }
        public string SelectedAttendanceRate
        {
            get => _selectedAttendanceRate;
            set
            {
                if (SetProperty(ref _selectedAttendanceRate, value))
                {
                    ApplyAttendanceFilters();
                }
            }
        }
        public string AttendanceSearchText
        {
            get => _attendanceSearchText;
            set
            {
                if (SetProperty(ref _attendanceSearchText, value))
                {
                    ApplyAttendanceFilters();
                }
            }
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
                _allAttendanceRows.Clear();
                _allAttendanceRows.AddRange(rows);
                ApplyAttendanceFilters();

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

        private void ApplyAttendanceFilters()
        {
            var query = _allAttendanceRows.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(SelectedAttendanceRate) && SelectedAttendanceRate != "All Rates")
            {
                if (SelectedAttendanceRate == "75% and above")
                {
                    query = query.Where(r => r.TotalDays > 0 && r.AttendancePercent >= 75);
                }
                else if (SelectedAttendanceRate == "Below 75%")
                {
                    query = query.Where(r => r.TotalDays > 0 && r.AttendancePercent < 75);
                }
                else if (SelectedAttendanceRate == "No Records")
                {
                    query = query.Where(r => r.TotalDays == 0);
                }
            }

            if (!string.IsNullOrWhiteSpace(AttendanceSearchText))
            {
                var search = AttendanceSearchText.Trim();
                query = query.Where(r => ContainsText(r.Name, search));
            }

            AttendanceRows.Clear();
            foreach (var row in query)
            {
                AttendanceRows.Add(row);
            }

            AttendanceTotalPresent = AttendanceRows.Sum(r => r.PresentDays);
            AttendanceTotalAbsent = AttendanceRows.Sum(r => r.AbsentDays);
            AttendanceStatusMessage = $"Showing {AttendanceRows.Count} of {_allAttendanceRows.Count} rows";

            PrintAttendanceReportCommand.RaiseCanExecuteChanged();
            ExportAttendanceCsvCommand.RaiseCanExecuteChanged();
            CommandManager.InvalidateRequerySuggested();
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
        private ObservableCollection<string> _financePeriodOptions;
        private string _selectedFinancePeriod;
        private string _selectedFinanceMonth;
        private DateTime? _financeCustomFrom;
        private DateTime? _financeCustomTo;
        private MonthlyFinanceSummary _financeSummary;
        private readonly List<Expense> _allExpenseRows = new List<Expense>();
        private readonly List<SalaryPayment> _allSalaryRows = new List<SalaryPayment>();
        private ObservableCollection<Expense> _expenseRows = new ObservableCollection<Expense>();
        private ObservableCollection<SalaryPayment> _salaryRows = new ObservableCollection<SalaryPayment>();
        private ObservableCollection<string> _expenseCategoryOptions = new ObservableCollection<string> { "All Categories" };
        private ObservableCollection<string> _salaryTeacherOptions = new ObservableCollection<string> { "All Teachers" };
        private ObservableCollection<string> _salaryStaffTypeOptions = new ObservableCollection<string> { "All Staff Types" };
        private string _selectedExpenseCategory = "All Categories";
        private string _selectedSalaryTeacher = "All Teachers";
        private string _selectedSalaryStaffType = "All Staff Types";
        private string _expenseSearchText;
        private string _salarySearchText;
        private bool _isLoadingFinance;
        private string _financeStatusMessage;

        public ObservableCollection<string> FinanceMonthOptions
        {
            get => _financeMonthOptions;
            set => SetProperty(ref _financeMonthOptions, value);
        }
        public ObservableCollection<string> FinancePeriodOptions
        {
            get => _financePeriodOptions;
            set => SetProperty(ref _financePeriodOptions, value);
        }
        public string SelectedFinancePeriod
        {
            get => _selectedFinancePeriod;
            set
            {
                if (SetProperty(ref _selectedFinancePeriod, value))
                {
                    OnPropertyChanged(nameof(IsCustomFinancePeriod));
                    if (IsFinanceMonthOption(value))
                    {
                        SelectedFinanceMonth = value;
                    }
                    ClearFinanceReport();
                }
            }
        }
        public bool IsCustomFinancePeriod => SelectedFinancePeriod == FinancePeriodCustomRange;
        public string SelectedFinanceMonth
        {
            get => _selectedFinanceMonth;
            set
            {
                if (SetProperty(ref _selectedFinanceMonth, value))
                {
                    ClearFinanceReport();
                }
            }
        }
        public DateTime? FinanceCustomFrom
        {
            get => _financeCustomFrom;
            set
            {
                if (SetProperty(ref _financeCustomFrom, value))
                {
                    ClearFinanceReport();
                }
            }
        }
        public DateTime? FinanceCustomTo
        {
            get => _financeCustomTo;
            set
            {
                if (SetProperty(ref _financeCustomTo, value))
                {
                    ClearFinanceReport();
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
        public ObservableCollection<string> ExpenseCategoryOptions
        {
            get => _expenseCategoryOptions;
            set => SetProperty(ref _expenseCategoryOptions, value);
        }
        public ObservableCollection<string> SalaryTeacherOptions
        {
            get => _salaryTeacherOptions;
            set => SetProperty(ref _salaryTeacherOptions, value);
        }
        public ObservableCollection<string> SalaryStaffTypeOptions
        {
            get => _salaryStaffTypeOptions;
            set => SetProperty(ref _salaryStaffTypeOptions, value);
        }
        public string SelectedExpenseCategory
        {
            get => _selectedExpenseCategory;
            set
            {
                if (SetProperty(ref _selectedExpenseCategory, value))
                {
                    ApplyFinanceFilters();
                }
            }
        }
        public string SelectedSalaryTeacher
        {
            get => _selectedSalaryTeacher;
            set
            {
                if (SetProperty(ref _selectedSalaryTeacher, value))
                {
                    ApplyFinanceFilters();
                }
            }
        }
        public string SelectedSalaryStaffType
        {
            get => _selectedSalaryStaffType;
            set
            {
                if (SetProperty(ref _selectedSalaryStaffType, value))
                {
                    ApplyFinanceFilters();
                }
            }
        }
        public string ExpenseSearchText
        {
            get => _expenseSearchText;
            set
            {
                if (SetProperty(ref _expenseSearchText, value))
                {
                    ApplyFinanceFilters();
                }
            }
        }
        public string SalarySearchText
        {
            get => _salarySearchText;
            set
            {
                if (SetProperty(ref _salarySearchText, value))
                {
                    ApplyFinanceFilters();
                }
            }
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

        private void ClearFinanceReport()
        {
            ExpenseRows.Clear();
            SalaryRows.Clear();
            _allExpenseRows.Clear();
            _allSalaryRows.Clear();
            ExpenseCategoryOptions.Clear();
            ExpenseCategoryOptions.Add("All Categories");
            SalaryTeacherOptions.Clear();
            SalaryTeacherOptions.Add("All Teachers");
            SalaryStaffTypeOptions.Clear();
            SalaryStaffTypeOptions.Add("All Staff Types");
            SelectedExpenseCategory = "All Categories";
            SelectedSalaryTeacher = "All Teachers";
            SelectedSalaryStaffType = "All Staff Types";
            ExpenseSearchText = string.Empty;
            SalarySearchText = string.Empty;
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

        private async Task GenerateFinanceReportAsync()
        {
            try
            {
                IsLoadingFinance = true;
                FinanceStatusMessage = string.Empty;

                var period = ResolveFinancePeriod();

                FinanceSummary = period.UseMonth
                    ? await _monthlyRepo.GetMonthlySummaryAsync(SelectedFinanceMonth)
                    : await _monthlyRepo.GetFinanceSummaryAsync(period.From, period.To, period.Label);

                var expenses = period.UseMonth
                    ? await _expenseRepo.GetAllExpensesAsync(SelectedFinanceMonth)
                    : await _expenseRepo.GetExpensesByDateRangeAsync(period.From, period.To);
                _allExpenseRows.Clear();
                _allExpenseRows.AddRange(expenses);
                RefreshExpenseCategoryOptions();

                var salaries = period.UseMonth
                    ? await _salaryRepo.GetAllPaymentsAsync(SelectedFinanceMonth)
                    : await _salaryRepo.GetPaymentsByDateRangeAsync(period.From, period.To);
                _allSalaryRows.Clear();
                _allSalaryRows.AddRange(salaries);
                RefreshSalaryTeacherOptions();
                RefreshSalaryStaffTypeOptions();
                ApplyFinanceFilters();

                FinanceStatusMessage = $"Loaded {ExpenseRows.Count} expenses and {SalaryRows.Count} salary payments for {period.Label}";

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

        private FinancePeriodRange ResolveFinancePeriod()
        {
            var today = DateTime.Today;

            if (SelectedFinancePeriod == FinancePeriodToday)
            {
                return new FinancePeriodRange(today, today, FinancePeriodToday, false);
            }

            if (SelectedFinancePeriod == FinancePeriodLastSevenDays)
            {
                return new FinancePeriodRange(today.AddDays(-6), today, FinancePeriodLastSevenDays, false);
            }

            if (SelectedFinancePeriod == FinancePeriodLastThirtyDays)
            {
                return new FinancePeriodRange(today.AddDays(-29), today, FinancePeriodLastThirtyDays, false);
            }

            if (SelectedFinancePeriod == FinancePeriodThisMonth)
            {
                return new FinancePeriodRange(new DateTime(today.Year, today.Month, 1), today, FinancePeriodThisMonth, false);
            }

            if (SelectedFinancePeriod == FinancePeriodLastMonth)
            {
                var firstOfThisMonth = new DateTime(today.Year, today.Month, 1);
                var from = firstOfThisMonth.AddMonths(-1);
                var to = firstOfThisMonth.AddDays(-1);
                return new FinancePeriodRange(from, to, FinancePeriodLastMonth, false);
            }

            if (SelectedFinancePeriod == FinancePeriodCustomRange)
            {
                var from = FinanceCustomFrom?.Date ?? new DateTime(today.Year, today.Month, 1);
                var to = FinanceCustomTo?.Date ?? today;
                if (from > to)
                {
                    throw new InvalidOperationException("Custom finance period From date cannot be after To date.");
                }

                return new FinancePeriodRange(from, to, FormatDateRangeLabel(from, to), false);
            }

            var monthLabel = IsFinanceMonthOption(SelectedFinancePeriod)
                ? SelectedFinancePeriod
                : SelectedFinanceMonth;

            monthLabel = string.IsNullOrWhiteSpace(monthLabel)
                ? today.ToString("MMM yyyy", CultureInfo.CurrentCulture)
                : monthLabel;

            if (!DateTime.TryParseExact(monthLabel, "MMM yyyy", CultureInfo.CurrentCulture, DateTimeStyles.None, out var monthStart))
            {
                monthStart = new DateTime(today.Year, today.Month, 1);
                monthLabel = monthStart.ToString("MMM yyyy", CultureInfo.CurrentCulture);
            }

            var monthEnd = monthStart.AddMonths(1).AddDays(-1);
            return new FinancePeriodRange(monthStart, monthEnd, monthLabel, true);
        }

        private static bool IsFinanceMonthOption(string value)
        {
            return !string.IsNullOrWhiteSpace(value) &&
                   DateTime.TryParseExact(value, "MMM yyyy", CultureInfo.CurrentCulture, DateTimeStyles.None, out _);
        }

        private static string FormatDateRangeLabel(DateTime from, DateTime to)
        {
            return from.Date == to.Date
                ? from.ToString("dd MMM yyyy", CultureInfo.CurrentCulture)
                : $"{from:dd MMM yyyy} - {to:dd MMM yyyy}";
        }

        private void RefreshExpenseCategoryOptions()
        {
            var selected = SelectedExpenseCategory;
            ExpenseCategoryOptions.Clear();
            ExpenseCategoryOptions.Add("All Categories");
            foreach (var category in _allExpenseRows
                .Select(e => string.IsNullOrWhiteSpace(e.Category) ? "Uncategorized" : e.Category.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(c => c))
            {
                ExpenseCategoryOptions.Add(category);
            }

            SelectedExpenseCategory = ExpenseCategoryOptions.Contains(selected) ? selected : "All Categories";
        }

        private void RefreshSalaryTeacherOptions()
        {
            var selected = SelectedSalaryTeacher;
            SalaryTeacherOptions.Clear();
            SalaryTeacherOptions.Add("All Teachers");
            foreach (var teacher in _allSalaryRows
                .Select(s => string.IsNullOrWhiteSpace(s.TeacherName) ? "Unknown Teacher" : s.TeacherName.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(t => t))
            {
                SalaryTeacherOptions.Add(teacher);
            }

            SelectedSalaryTeacher = SalaryTeacherOptions.Contains(selected) ? selected : "All Teachers";
        }

        private void RefreshSalaryStaffTypeOptions()
        {
            var selected = SelectedSalaryStaffType;
            SalaryStaffTypeOptions.Clear();
            SalaryStaffTypeOptions.Add("All Staff Types");
            foreach (var staffType in _allSalaryRows
                .Select(s => string.IsNullOrWhiteSpace(s.StaffType) ? "Teacher" : s.StaffType.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(t => t))
            {
                SalaryStaffTypeOptions.Add(staffType);
            }

            SelectedSalaryStaffType = SalaryStaffTypeOptions.Contains(selected) ? selected : "All Staff Types";
        }

        private void ApplyFinanceFilters()
        {
            var expenseQuery = _allExpenseRows.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(SelectedExpenseCategory) && SelectedExpenseCategory != "All Categories")
            {
                expenseQuery = expenseQuery.Where(e => string.Equals(
                    string.IsNullOrWhiteSpace(e.Category) ? "Uncategorized" : e.Category.Trim(),
                    SelectedExpenseCategory,
                    StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(ExpenseSearchText))
            {
                var search = ExpenseSearchText.Trim();
                expenseQuery = expenseQuery.Where(e =>
                    ContainsText(e.Category, search) ||
                    ContainsText(e.Notes, search));
            }

            ExpenseRows.Clear();
            foreach (var expense in expenseQuery)
            {
                ExpenseRows.Add(expense);
            }

            var salaryQuery = _allSalaryRows.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(SelectedSalaryTeacher) && SelectedSalaryTeacher != "All Teachers")
            {
                salaryQuery = salaryQuery.Where(s => string.Equals(
                    string.IsNullOrWhiteSpace(s.TeacherName) ? "Unknown Teacher" : s.TeacherName.Trim(),
                    SelectedSalaryTeacher,
                    StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(SelectedSalaryStaffType) && SelectedSalaryStaffType != "All Staff Types")
            {
                salaryQuery = salaryQuery.Where(s => string.Equals(
                    string.IsNullOrWhiteSpace(s.StaffType) ? "Teacher" : s.StaffType.Trim(),
                    SelectedSalaryStaffType,
                    StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(SalarySearchText))
            {
                var search = SalarySearchText.Trim();
                salaryQuery = salaryQuery.Where(s =>
                    ContainsText(s.TeacherName, search) ||
                    ContainsText(s.StaffType, search) ||
                    ContainsText(s.Designation, search) ||
                    ContainsText(s.Notes, search));
            }

            SalaryRows.Clear();
            foreach (var salary in salaryQuery)
            {
                SalaryRows.Add(salary);
            }

            FinanceStatusMessage = $"Showing {ExpenseRows.Count} of {_allExpenseRows.Count} expenses and {SalaryRows.Count} of {_allSalaryRows.Count} salary payments for {GetFinanceReportLabel()}";

            PrintExpenseReportCommand.RaiseCanExecuteChanged();
            PrintSalaryReportCommand.RaiseCanExecuteChanged();
            ExportFinanceCsvCommand.RaiseCanExecuteChanged();
            CommandManager.InvalidateRequerySuggested();
        }

        private void PrintExpenseReport()
        {
            ReportPrintService.PrintExpenseReport(ExpenseRows.ToList(), GetFinanceReportLabel());
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
            ReportPrintService.PrintSalaryReport(SalaryRows.ToList(), GetFinanceReportLabel());
        }

        private void ExportFinanceCsv()
        {
            var saveFileDialog = new SaveFileDialog
            {
                Filter = "CSV|*.csv",
                FileName = $"FinanceReport_{MakeSafeFileName(GetFinanceReportLabel())}.csv",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
            };

            if (saveFileDialog.ShowDialog() != true) return;
            if (FinanceSummary == null) return;

            var csv = new StringBuilder();
            csv.AppendLine("Finance Summary");
            csv.AppendLine($"Period,{FinanceSummary.Month}");
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
            csv.AppendLine("Staff,Staff Type,Designation,Amount,Payment Date,Notes");
            foreach (var salary in SalaryRows)
            {
                csv.AppendLine($"{EscapeCsv(salary.TeacherName)},{EscapeCsv(salary.StaffType)},{EscapeCsv(salary.Designation)},{salary.Amount.ToString(CultureInfo.InvariantCulture)},{salary.PaymentDate:dd MMM yyyy},{EscapeCsv(salary.Notes)}");
            }

            File.WriteAllText(saveFileDialog.FileName, csv.ToString(), Encoding.UTF8);
        }

        private string GetFinanceReportLabel()
        {
            if (!string.IsNullOrWhiteSpace(FinanceSummary?.Month))
            {
                return FinanceSummary.Month;
            }

            if (IsFinanceMonthOption(SelectedFinancePeriod))
            {
                return SelectedFinancePeriod;
            }

            return string.IsNullOrWhiteSpace(SelectedFinancePeriod)
                ? DateTime.Today.ToString("MMM yyyy", CultureInfo.CurrentCulture)
                : SelectedFinancePeriod;
        }

        private static string MakeSafeFileName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "FinanceReport";
            }

            var builder = new StringBuilder(value.Length);
            foreach (var ch in value)
            {
                builder.Append(Path.GetInvalidFileNameChars().Contains(ch) ? '_' : ch);
            }

            return builder.ToString().Replace(" ", "_");
        }

        #endregion

        #region Helpers

        private class FinancePeriodRange
        {
            public FinancePeriodRange(DateTime from, DateTime to, string label, bool useMonth)
            {
                From = from;
                To = to;
                Label = label;
                UseMonth = useMonth;
            }

            public DateTime From { get; }

            public DateTime To { get; }

            public string Label { get; }

            public bool UseMonth { get; }
        }

        private string EscapeCsv(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            if (value.Contains(",") || value.Contains("\"") || value.Contains("\n"))
            {
                return $"\"{value.Replace("\"", "\"\"")}\"";
            }
            return value;
        }

        private bool ContainsText(string value, string search)
        {
            return !string.IsNullOrWhiteSpace(value) &&
                   value.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        #endregion
    }
}
