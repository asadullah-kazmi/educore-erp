using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using SchoolERP.Models;
using SchoolERP.Data;
using SchoolERP.Repositories;
using SchoolERP.Services;
using SchoolERP.Views;

namespace SchoolERP.ViewModels
{
    public class FeesViewModel : ViewModelBase
    {
        private readonly FeeRepository repository = new FeeRepository();
        private readonly StudentRepository studentRepository = new StudentRepository();
        private string searchText = string.Empty;
        private string statusFilter = "All";
        private string feeTypeFilter = "All Fee Types";
        private string displayMonthFilter = "All Months";
        private string sectionFilter = "All Sections";
        private ClassFilterOption selectedClassFilter;
        private decimal totalCollected;
        private decimal totalOutstanding;
        private int totalRecords;

        public FeesViewModel()
        {
            AllFees = new ObservableCollection<FeeRecord>();
            FilteredFees = new ObservableCollection<FeeRecord>();
            MonthOptions = new ObservableCollection<string>();
            FeeTypeOptions = new ObservableCollection<string>();
            ClassFilterOptions = new ObservableCollection<ClassFilterOption>();
            SectionFilterOptions = new ObservableCollection<string>
            {
                "All Sections",
                "A",
                "B",
                "C",
                "D"
            };

            // Generate months Jan 2025 through Dec 2026
            MonthOptions.Add("All Months");
            DateTime start = new DateTime(2025, 1, 1);
            for (int i = 0; i < 24; i++)
            {
                MonthOptions.Add(start.AddMonths(i).ToString("MMM yyyy"));
            }
            FeeTypeOptions.Add("All Fee Types");

            LoadFeesCommand = new RelayCommand(async _ => await LoadFeesAsync());
            GenerateMonthlyFeesCommand = new RelayCommand(async _ => await OnGenerateMonthlyFeesAsync(), _ => IsAdmin);
            MarkAsPaidCommand = new RelayCommand<FeeRecord>(async fee => await OnMarkAsPaidAsync(fee));
            AddFeeCommand = new RelayCommand(_ => OnAddFee());
            EditFeeCommand = new RelayCommand<FeeRecord>(fee => OnEditFee(fee));
            DeleteFeeCommand = new RelayCommand<FeeRecord>(async fee => await OnDeleteFeeAsync(fee));
            ViewFeeDetailCommand = new RelayCommand<FeeRecord>(fee => OpenFeeDetail(fee));
            PrintReceiptCommand = new RelayCommand<FeeRecord>(fee => OpenFeeReceipt(fee));

            StatusFilter = "All";
            FeeTypeFilter = "All Fee Types";
            DisplayMonthFilter = DateTime.Now.ToString("MMM yyyy");

            _ = InitializeAsync();
        }

        public ObservableCollection<FeeRecord> AllFees { get; }
        public ObservableCollection<FeeRecord> FilteredFees { get; }
        public ObservableCollection<string> MonthOptions { get; }
        public ObservableCollection<string> FeeTypeOptions { get; }
        public ObservableCollection<ClassFilterOption> ClassFilterOptions { get; }
        public ObservableCollection<string> SectionFilterOptions { get; }

        public string SearchText
        {
            get => searchText;
            set
            {
                if (SetProperty(ref searchText, value))
                {
                    ApplyFilter();
                }
            }
        }

        public string StatusFilter
        {
            get => statusFilter;
            set
            {
                if (SetProperty(ref statusFilter, value))
                {
                    ApplyFilter();
                }
            }
        }

        public string FeeTypeFilter
        {
            get => feeTypeFilter;
            set
            {
                if (SetProperty(ref feeTypeFilter, value))
                {
                    ApplyFilter();
                }
            }
        }

        public string DisplayMonthFilter
        {
            get => displayMonthFilter;
            set
            {
                if (SetProperty(ref displayMonthFilter, value))
                {
                    _ = LoadFeesAsync();
                }
            }
        }

        public string SectionFilter
        {
            get => sectionFilter;
            set
            {
                if (SetProperty(ref sectionFilter, value))
                {
                    _ = LoadFeesAsync();
                }
            }
        }

        public ClassFilterOption SelectedClassFilter
        {
            get => selectedClassFilter;
            set
            {
                if (SetProperty(ref selectedClassFilter, value))
                {
                    _ = LoadFeesAsync();
                }
            }
        }

        public decimal TotalCollected
        {
            get => totalCollected;
            set => SetProperty(ref totalCollected, value);
        }

        public decimal TotalOutstanding
        {
            get => totalOutstanding;
            set => SetProperty(ref totalOutstanding, value);
        }

        public int TotalRecords
        {
            get => totalRecords;
            set => SetProperty(ref totalRecords, value);
        }

        public bool IsAdmin => string.Equals(AppSession.CurrentRole, "Admin", StringComparison.OrdinalIgnoreCase);

        public ICommand LoadFeesCommand { get; }
        public ICommand GenerateMonthlyFeesCommand { get; }
        public RelayCommand<FeeRecord> MarkAsPaidCommand { get; }
        public ICommand AddFeeCommand { get; }
        public RelayCommand<FeeRecord> EditFeeCommand { get; }
        public RelayCommand<FeeRecord> DeleteFeeCommand { get; }

        public RelayCommand<FeeRecord> ViewFeeDetailCommand { get; }

        public RelayCommand<FeeRecord> PrintReceiptCommand { get; }

        private async Task InitializeAsync()
        {
            try
            {
                ClassFilterOptions.Clear();
                ClassFilterOptions.Add(new ClassFilterOption(null, "All Classes"));

                var classes = await studentRepository.GetAllClassesAsync().ConfigureAwait(true);
                foreach (var item in classes)
                {
                    ClassFilterOptions.Add(new ClassFilterOption(item.ClassID, item.ClassName));
                }

                SelectedClassFilter = ClassFilterOptions.FirstOrDefault();
                await LoadFeesAsync().ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to load fee filters: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async Task LoadFeesAsync()
        {
            try
            {
                var fees = await repository.GetAllFeesAsync().ConfigureAwait(true);

                AllFees.Clear();
                foreach (var fee in fees)
                {
                    AllFees.Add(fee);
                }

                await LoadFeeTypesAsync().ConfigureAwait(true);
                ApplyFilter();

                string currentMonth = DateTime.Now.ToString("MMM yyyy");
                TotalCollected = await repository.GetTotalCollectedAsync(currentMonth).ConfigureAwait(true);
                TotalOutstanding = await repository.GetTotalOutstandingAsync().ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to load fees: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadFeeTypesAsync()
        {
            var selected = FeeTypeFilter;
            var feeTypes = await repository.GetFeeTypesAsync().ConfigureAwait(true);

            FeeTypeOptions.Clear();
            FeeTypeOptions.Add("All Fee Types");
            foreach (var feeType in feeTypes)
            {
                FeeTypeOptions.Add(feeType);
            }

            FeeTypeFilter = FeeTypeOptions.Contains(selected) ? selected : "All Fee Types";
        }

        public void ApplyFilter()
        {
            var search = (SearchText ?? string.Empty).Trim();
            var status = StatusFilter ?? "All";
            var feeType = FeeTypeFilter ?? "All Fee Types";
            var displayMonth = DisplayMonthFilter ?? "All Months";
            var selectedClassId = SelectedClassFilter?.ClassID;
            var selectedSection = GetSelectedSection();

            FilteredFees.Clear();

            var query = AllFees.AsEnumerable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(f =>
                    (f.StudentName != null && f.StudentName.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (f.RegistrationNo != null && f.RegistrationNo.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0)
                );
            }

            if (!string.Equals(status, "All", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(f => string.Equals(f.Status, status, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.Equals(feeType, "All Fee Types", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(f => string.Equals((f.FeeType ?? "").Trim(), feeType.Trim(), StringComparison.OrdinalIgnoreCase));
            }

            if (!string.Equals(displayMonth, "All Months", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(f => string.Equals((f.Month ?? "").Trim(), displayMonth.Trim(), StringComparison.OrdinalIgnoreCase));
            }

            if (selectedClassId.HasValue)
            {
                query = query.Where(f => f.ClassID == selectedClassId.Value);
            }

            if (selectedSection != null)
            {
                query = query.Where(f => string.Equals((f.Section ?? string.Empty).Trim(), selectedSection, StringComparison.OrdinalIgnoreCase));
            }

            foreach (var fee in query)
            {
                FilteredFees.Add(fee);
            }

            TotalRecords = FilteredFees.Count;
        }

        private async Task OnGenerateMonthlyFeesAsync()
        {
            string month = DateTime.Now.ToString("MMM yyyy");
            try
            {
                bool success = await repository.GenerateMonthlyFeesAsync(month, "Monthly Tuition").ConfigureAwait(true);
                if (success)
                {
                    MessageBox.Show($"Monthly fees generated for {month}", "Generate Fees", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show($"Fees already exist for {month}", "Generate Fees", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                await LoadFeesAsync().ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to generate monthly fees: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task OnMarkAsPaidAsync(FeeRecord fee)
        {
            if (fee == null) return;
            if (!fee.HasFeeRecord)
            {
                MessageBox.Show("This fee has not been generated yet.", "Fee Not Generated", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                var payableFees = await GetPayableFeesForStudentAsync(fee).ConfigureAwait(true);
                if (payableFees.Count == 0)
                {
                    MessageBox.Show("There is no outstanding balance for this student.", "No Balance", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var window = new RecordFeePaymentWindow(payableFees)
                {
                    Owner = Application.Current.MainWindow
                };

                if (window.ShowDialog() != true)
                {
                    return;
                }

                var success = await repository.ApplyPaymentAsync(payableFees, window.PaymentAmount, window.PaymentDate).ConfigureAwait(true);
                if (success)
                {
                    await LoadFeesAsync().ConfigureAwait(true);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to mark fee as paid: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task<List<FeeRecord>> GetPayableFeesForStudentAsync(FeeRecord selectedFee)
        {
            var studentFees = await repository.GetAllFeesAsync(studentId: selectedFee.StudentID).ConfigureAwait(true);
            var selectedMonth = TryParseMonth(selectedFee.Month);

            return studentFees
                .Where(f => f.HasFeeRecord && f.Balance > 0)
                .Where(f => ShouldIncludeInPayment(f, selectedFee, selectedMonth))
                .OrderBy(f => TryParseMonth(f.Month) ?? DateTime.MaxValue)
                .ThenBy(f => f.FeeID)
                .ToList();
        }

        private static bool ShouldIncludeInPayment(FeeRecord fee, FeeRecord selectedFee, DateTime? selectedMonth)
        {
            if (fee.FeeID == selectedFee.FeeID)
            {
                return true;
            }

            if (!selectedMonth.HasValue)
            {
                return false;
            }

            var feeMonth = TryParseMonth(fee.Month);
            return feeMonth.HasValue && feeMonth.Value <= selectedMonth.Value;
        }

        private static DateTime? TryParseMonth(string month)
        {
            if (DateTime.TryParseExact(
                (month ?? string.Empty).Trim(),
                "MMM yyyy",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var parsed))
            {
                return parsed;
            }

            return null;
        }

        private void OnAddFee()
        {
            var window = new AddEditFeeWindow(null);
            window.Owner = Application.Current.MainWindow;
            if (window.ShowDialog() == true)
            {
                _ = LoadFeesAsync();
            }
        }

        private void OnEditFee(FeeRecord fee)
        {
            if (fee == null) return;
            if (!fee.HasFeeRecord)
            {
                MessageBox.Show("This fee is not generated yet. Use Mark as Paid or Generate Monthly Fees first.", "Fee Not Generated", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var window = new AddEditFeeWindow(fee);
            window.Owner = Application.Current.MainWindow;
            if (window.ShowDialog() == true)
            {
                _ = LoadFeesAsync();
            }
        }

        private async Task OnDeleteFeeAsync(FeeRecord fee)
        {
            if (fee == null) return;
            if (!fee.HasFeeRecord)
            {
                MessageBox.Show("This fee has no saved record to delete.", "Fee Not Generated", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show($"Are you sure you want to delete the fee record for {fee.StudentName}?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    bool success = await repository.DeleteFeeAsync(fee.FeeID).ConfigureAwait(true);
                    if (success)
                    {
                        await LoadFeesAsync().ConfigureAwait(true);
                    }
                    else
                    {
                        MessageBox.Show("Failed to delete fee record.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Failed to delete fee record: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void OpenFeeDetail(FeeRecord fee)
        {
            if (fee == null) return;
            if (!fee.HasFeeRecord)
            {
                MessageBox.Show("This fee has not been generated yet.", "Fee Not Generated", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var window = new FeeDetailWindow(fee) { Owner = Application.Current.MainWindow };
            window.ShowDialog();
        }

        private void OpenFeeReceipt(FeeRecord fee)
        {
            if (fee == null) return;
            if (!fee.HasFeeRecord || !string.Equals(fee.Status, "Paid", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("Receipt is available after payment is recorded.", "Receipt Not Available", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var window = new FeeReceiptWindow(fee) { Owner = Application.Current.MainWindow };
            window.ShowDialog();
        }

        private string GetSelectedSection()
        {
            var selected = (SectionFilter ?? string.Empty).Trim();
            return string.IsNullOrEmpty(selected) ||
                   string.Equals(selected, "All Sections", StringComparison.OrdinalIgnoreCase)
                ? null
                : selected;
        }
    }

    public class ClassFilterOption
    {
        public ClassFilterOption(int? classId, string className)
        {
            ClassID = classId;
            ClassName = className;
        }

        public int? ClassID { get; }

        public string ClassName { get; }
    }
}
