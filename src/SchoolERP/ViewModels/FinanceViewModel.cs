using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using SchoolERP.Controls;
using SchoolERP.Data;
using SchoolERP.Models;
using SchoolERP.Services;
using SchoolERP.Views;

namespace SchoolERP.ViewModels
{
    public class FinanceViewModel : ViewModelBase
    {
        private readonly ExpenseRepository expenseRepository = new ExpenseRepository();
        private readonly SalaryRepository salaryRepository = new SalaryRepository();
        private readonly MonthlyReportRepository monthlyRepository = new MonthlyReportRepository();
        private readonly StaffRepository staffRepository = new StaffRepository();

        private int selectedTabIndex;
        private string expenseSearchText = string.Empty;
        private string expenseCategoryFilter = "All Categories";
        private string expenseMonthFilter;
        private decimal totalExpensesFiltered;
        private string salaryMonthFilter;
        private decimal totalSalaryPaidThisMonth;
        private string selectedSummaryMonth;
        private MonthlyFinanceSummary currentSummary;
        private bool isLoadingSummary;

        public FinanceViewModel()
        {
            AllExpenses = new ObservableCollection<ExpenseViewModel>();
            FilteredExpenses = new ObservableCollection<ExpenseViewModel>();
            CategoryOptions = new ObservableCollection<string>();
            StaffRows = new ObservableCollection<StaffSalaryRowViewModel>();
            SalaryMonthOptions = new ObservableCollection<string>();
            SummaryMonthOptions = new ObservableCollection<string>();

            InitializeMonthOptions(SalaryMonthOptions);
            InitializeMonthOptions(SummaryMonthOptions);

            var currentMonth = DateTime.Now.ToString("MMM yyyy");
            ExpenseMonthFilter = currentMonth;
            SalaryMonthFilter = currentMonth;
            SelectedSummaryMonth = currentMonth;

            LoadExpensesCommand = new RelayCommand(async _ => await LoadExpensesAsync());
            AddExpenseCommand = new RelayCommand(_ => OnAddExpense());
            EditExpenseCommand = new RelayCommand<ExpenseViewModel>(vm => OnEditExpense(vm));
            DeleteExpenseCommand = new RelayCommand<ExpenseViewModel>(
                async vm => await OnDeleteExpenseAsync(vm),
                _ => IsAdmin);

            LoadSalariesCommand = new RelayCommand(async _ => await LoadSalariesAsync());
            PaySalaryCommand = new RelayCommand<StaffSalaryRowViewModel>(staff => OnPaySalary(staff));
            ViewHistoryCommand = new RelayCommand<StaffSalaryRowViewModel>(staff => OnViewHistory(staff));

            LoadSummaryCommand = new RelayCommand(async _ => await LoadSummaryAsync());
            PrintFeeReportCommand = new RelayCommand(async _ => await OnPrintFeeReportAsync());
            PrintExpenseReportCommand = new RelayCommand(async _ => await OnPrintExpenseReportAsync());
            ExportSummaryCommand = new RelayCommand(async _ => await OnExportSummaryAsync());

            _ = LoadExpensesAsync();
            _ = LoadSalariesAsync();
            _ = LoadSummaryAsync();
        }

        public bool IsAdmin => string.Equals(AppSession.CurrentRole, "Admin", StringComparison.OrdinalIgnoreCase);

        public int SelectedTabIndex
        {
            get => selectedTabIndex;
            set => SetProperty(ref selectedTabIndex, value);
        }

        public ObservableCollection<ExpenseViewModel> AllExpenses { get; }

        public ObservableCollection<ExpenseViewModel> FilteredExpenses { get; }

        public ObservableCollection<string> CategoryOptions { get; }

        public string ExpenseSearchText
        {
            get => expenseSearchText;
            set
            {
                if (SetProperty(ref expenseSearchText, value))
                {
                    ApplyExpenseFilter();
                }
            }
        }

        public string ExpenseCategoryFilter
        {
            get => expenseCategoryFilter;
            set
            {
                if (SetProperty(ref expenseCategoryFilter, value))
                {
                    ApplyExpenseFilter();
                }
            }
        }

        public string ExpenseMonthFilter
        {
            get => expenseMonthFilter;
            set
            {
                if (SetProperty(ref expenseMonthFilter, value))
                {
                    _ = LoadExpensesAsync();
                }
            }
        }

        public decimal TotalExpensesFiltered
        {
            get => totalExpensesFiltered;
            private set => SetProperty(ref totalExpensesFiltered, value);
        }

        public ObservableCollection<StaffSalaryRowViewModel> StaffRows { get; }

        public ObservableCollection<string> SalaryMonthOptions { get; }

        public string SalaryMonthFilter
        {
            get => salaryMonthFilter;
            set
            {
                if (SetProperty(ref salaryMonthFilter, value))
                {
                    _ = LoadSalariesAsync();
                }
            }
        }

        public int PendingCount => StaffRows.Count(r => !r.PaidThisMonth);

        public decimal TotalSalaryPaidThisMonth
        {
            get => totalSalaryPaidThisMonth;
            private set => SetProperty(ref totalSalaryPaidThisMonth, value);
        }

        public ObservableCollection<string> SummaryMonthOptions { get; }

        public string SelectedSummaryMonth
        {
            get => selectedSummaryMonth;
            set
            {
                if (SetProperty(ref selectedSummaryMonth, value))
                {
                    _ = LoadSummaryAsync();
                }
            }
        }

        public MonthlyFinanceSummary CurrentSummary
        {
            get => currentSummary;
            private set => SetProperty(ref currentSummary, value);
        }

        public string SurplusColor => (CurrentSummary?.NetSurplus ?? 0) >= 0 ? "#10B981" : "#EF4444";

        public bool IsLoadingSummary
        {
            get => isLoadingSummary;
            private set => SetProperty(ref isLoadingSummary, value);
        }

        public ICommand LoadExpensesCommand { get; }

        public ICommand AddExpenseCommand { get; }

        public RelayCommand<ExpenseViewModel> EditExpenseCommand { get; }

        public RelayCommand<ExpenseViewModel> DeleteExpenseCommand { get; }

        public ICommand LoadSalariesCommand { get; }

        public RelayCommand<StaffSalaryRowViewModel> PaySalaryCommand { get; }

        public RelayCommand<StaffSalaryRowViewModel> ViewHistoryCommand { get; }

        public ICommand LoadSummaryCommand { get; }

        public ICommand PrintFeeReportCommand { get; }

        public ICommand PrintExpenseReportCommand { get; }

        public ICommand ExportSummaryCommand { get; }

        private static void InitializeMonthOptions(ObservableCollection<string> options)
        {
            var start = new DateTime(2025, 1, 1);
            for (int i = 0; i < 24; i++)
            {
                options.Add(start.AddMonths(i).ToString("MMM yyyy"));
            }
        }

        public async Task LoadExpensesAsync()
        {
            try
            {
                var list = await expenseRepository.GetAllExpensesAsync(ExpenseMonthFilter).ConfigureAwait(true);
                AllExpenses.Clear();
                foreach (var expense in list)
                {
                    AllExpenses.Add(ExpenseViewModel.FromModel(expense));
                }

                var categories = await expenseRepository.GetCategoriesAsync().ConfigureAwait(true);
                CategoryOptions.Clear();
                CategoryOptions.Add("All Categories");
                foreach (var category in categories)
                {
                    CategoryOptions.Add(category);
                }

                ApplyExpenseFilter();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Failed to load expenses: " + ex.Message,
                    "Finance",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        public void ApplyExpenseFilter()
        {
            var search = (ExpenseSearchText ?? string.Empty).Trim();
            var categoryFilter = ExpenseCategoryFilter ?? "All Categories";

            FilteredExpenses.Clear();

            var query = AllExpenses.AsEnumerable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(e =>
                    e.Category != null &&
                    e.Category.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0);
            }

            if (!string.Equals(categoryFilter, "All Categories", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(e =>
                    string.Equals(e.Category, categoryFilter, StringComparison.OrdinalIgnoreCase));
            }

            foreach (var expense in query)
            {
                FilteredExpenses.Add(expense);
            }

            TotalExpensesFiltered = FilteredExpenses.Sum(x => x.Amount);
        }

        private void OnAddExpense()
        {
            var window = new AddEditExpenseWindow(null)
            {
                Owner = Application.Current.MainWindow
            };

            if (window.ShowDialog() == true)
            {
                _ = LoadExpensesAsync();
            }
        }

        private void OnEditExpense(ExpenseViewModel vm)
        {
            if (vm == null)
            {
                return;
            }

            var window = new AddEditExpenseWindow(vm.ToModel())
            {
                Owner = Application.Current.MainWindow
            };

            if (window.ShowDialog() == true)
            {
                _ = LoadExpensesAsync();
            }
        }

        private async Task OnDeleteExpenseAsync(ExpenseViewModel vm)
        {
            if (vm == null || !IsAdmin)
            {
                return;
            }

            if (!ConfirmationDialog.Show(
                "Are you sure you want to delete this expense record?",
                "Confirm Delete"))
            {
                return;
            }

            try
            {
                bool success = await expenseRepository.DeleteExpenseAsync(vm.ExpenseID).ConfigureAwait(true);
                if (success)
                {
                    await LoadExpensesAsync().ConfigureAwait(true);
                }
                else
                {
                    MessageBox.Show(
                        "Failed to delete expense record.",
                        "Finance",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Failed to delete expense record: " + ex.Message,
                    "Finance",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        public async Task LoadSalariesAsync()
        {
            try
            {
                var teachers = await staffRepository.GetAllStaffAsync().ConfigureAwait(true);
                var payments = await salaryRepository.GetAllPaymentsAsync(month: SalaryMonthFilter).ConfigureAwait(true);

                StaffRows.Clear();
                foreach (var teacher in teachers)
                {
                    var teacherPayments = payments.Where(p => p.TeacherID == teacher.TeacherID).ToList();
                    var row = new StaffSalaryRowViewModel
                    {
                        TeacherID = teacher.TeacherID,
                        Name = teacher.Name,
                        Designation = teacher.Designation,
                        BaseSalary = teacher.Salary,
                        PaidThisMonth = teacherPayments.Count > 0,
                        LastPaymentDate = teacherPayments
                            .OrderByDescending(p => p.PaymentDate)
                            .FirstOrDefault()?.PaymentDate
                    };
                    StaffRows.Add(row);
                }

                TotalSalaryPaidThisMonth = await salaryRepository
                    .GetTotalSalariesPaidAsync(SalaryMonthFilter)
                    .ConfigureAwait(true);

                OnPropertyChanged(nameof(PendingCount));
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Failed to load salaries: " + ex.Message,
                    "Finance",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void OnPaySalary(StaffSalaryRowViewModel staff)
        {
            if (staff == null)
            {
                return;
            }

            if (staff.PaidThisMonth)
            {
                var result = MessageBox.Show(
                    "Salary already paid for " + SalaryMonthFilter + ". Pay again?",
                    "Confirm Payment",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                {
                    return;
                }
            }

            var window = new PaySalaryWindow(staff, SalaryMonthFilter)
            {
                Owner = Application.Current.MainWindow
            };

            if (window.ShowDialog() == true)
            {
                _ = LoadSalariesAsync();
            }
        }

        private void OnViewHistory(StaffSalaryRowViewModel staff)
        {
            if (staff == null)
            {
                return;
            }

            var window = new SalaryHistoryWindow(staff.TeacherID, staff.Name, IsAdmin)
            {
                Owner = Application.Current.MainWindow
            };

            window.ShowDialog();
        }

        public async Task LoadSummaryAsync()
        {
            IsLoadingSummary = true;

            try
            {
                CurrentSummary = await monthlyRepository
                    .GetMonthlySummaryAsync(SelectedSummaryMonth)
                    .ConfigureAwait(true);

                OnPropertyChanged(nameof(SurplusColor));
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Failed to load summary: " + ex.Message,
                    "Finance",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                IsLoadingSummary = false;
            }
        }

        private async Task OnPrintFeeReportAsync()
        {
            try
            {
                var rows = await monthlyRepository
                    .GetFeeCollectionReportAsync(SelectedSummaryMonth)
                    .ConfigureAwait(true);

                ReportPrintService.PrintFeeCollectionReport(rows, SelectedSummaryMonth);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Failed to print fee report: " + ex.Message,
                    "Finance",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private async Task OnPrintExpenseReportAsync()
        {
            try
            {
                var expenses = await expenseRepository
                    .GetAllExpensesAsync(month: SelectedSummaryMonth)
                    .ConfigureAwait(true);

                ReportPrintService.PrintExpenseReport(expenses, SelectedSummaryMonth);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Failed to print expense report: " + ex.Message,
                    "Finance",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private async Task OnExportSummaryAsync()
        {
            try
            {
                var summary = await monthlyRepository
                    .GetMonthlySummaryAsync(SelectedSummaryMonth)
                    .ConfigureAwait(true);

                var dialog = new SaveFileDialog
                {
                    Filter = "CSV|*.csv",
                    FileName = "Finance_" + SelectedSummaryMonth.Replace(" ", "_") + ".csv"
                };

                if (dialog.ShowDialog() != true)
                {
                    return;
                }

                var builder = new StringBuilder();
                builder.AppendLine("Month,Fees Collected,Expenses,Salaries Paid,Net Surplus");
                builder.AppendLine(string.Join(",",
                    EscapeCsv(summary.Month),
                    summary.TotalFeesCollected.ToString(CultureInfo.InvariantCulture),
                    summary.TotalExpenses.ToString(CultureInfo.InvariantCulture),
                    summary.TotalSalariesPaid.ToString(CultureInfo.InvariantCulture),
                    summary.NetSurplus.ToString(CultureInfo.InvariantCulture)));

                File.WriteAllText(dialog.FileName, builder.ToString(), Encoding.UTF8);

                MessageBox.Show(
                    "Summary exported successfully.",
                    "Finance",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Failed to export summary: " + ex.Message,
                    "Finance",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private static string EscapeCsv(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            if (value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) >= 0)
            {
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            }

            return value;
        }
    }
}
