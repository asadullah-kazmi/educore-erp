using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using SchoolERP.Models;
using SchoolERP.Repositories;
using SchoolERP.Services;
using SchoolERP.Views;

namespace SchoolERP.ViewModels
{
    public class FeesViewModel : ViewModelBase
    {
        private readonly FeeRepository repository = new FeeRepository();
        private string searchText = string.Empty;
        private string statusFilter = "All";
        private string displayMonthFilter = "All Months";
        private decimal totalCollected;
        private decimal totalOutstanding;
        private int totalRecords;

        public FeesViewModel()
        {
            AllFees = new ObservableCollection<FeeRecord>();
            FilteredFees = new ObservableCollection<FeeRecord>();
            MonthOptions = new ObservableCollection<string>();

            // Generate months Jan 2025 through Dec 2026
            MonthOptions.Add("All Months");
            DateTime start = new DateTime(2025, 1, 1);
            for (int i = 0; i < 24; i++)
            {
                MonthOptions.Add(start.AddMonths(i).ToString("MMM yyyy"));
            }

            LoadFeesCommand = new RelayCommand(async _ => await LoadFeesAsync());
            GenerateMonthlyFeesCommand = new RelayCommand(async _ => await OnGenerateMonthlyFeesAsync(), _ => IsAdmin);
            MarkAsPaidCommand = new RelayCommand<FeeRecord>(async fee => await OnMarkAsPaidAsync(fee));
            AddFeeCommand = new RelayCommand(_ => OnAddFee());
            EditFeeCommand = new RelayCommand<FeeRecord>(fee => OnEditFee(fee));
            DeleteFeeCommand = new RelayCommand<FeeRecord>(async fee => await OnDeleteFeeAsync(fee));
            ViewFeeDetailCommand = new RelayCommand<FeeRecord>(fee => OpenFeeDetail(fee));
            PrintReceiptCommand = new RelayCommand<FeeRecord>(fee => OpenFeeReceipt(fee));

            StatusFilter = "All";
            DisplayMonthFilter = DateTime.Now.ToString("MMM yyyy");

            // Execute the load command
            LoadFeesCommand.Execute(null);
        }

        public ObservableCollection<FeeRecord> AllFees { get; }
        public ObservableCollection<FeeRecord> FilteredFees { get; }
        public ObservableCollection<string> MonthOptions { get; }

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

        public string DisplayMonthFilter
        {
            get => displayMonthFilter;
            set
            {
                if (SetProperty(ref displayMonthFilter, value))
                {
                    ApplyFilter();
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

        public void ApplyFilter()
        {
            var search = (SearchText ?? string.Empty).Trim();
            var status = StatusFilter ?? "All";
            var displayMonth = DisplayMonthFilter ?? "All Months";

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

            if (!string.Equals(displayMonth, "All Months", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(f => string.Equals((f.Month ?? "").Trim(), displayMonth.Trim(), StringComparison.OrdinalIgnoreCase));
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
            try
            {
                bool success = await repository.MarkAsPaidAsync(fee.FeeID, DateTime.Today).ConfigureAwait(true);
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
            var window = new FeeDetailWindow(fee) { Owner = Application.Current.MainWindow };
            window.ShowDialog();
        }

        private void OpenFeeReceipt(FeeRecord fee)
        {
            if (fee == null) return;
            var window = new FeeReceiptWindow(fee) { Owner = Application.Current.MainWindow };
            window.ShowDialog();
        }
    }
}
