using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using SchoolERP.Data;
using SchoolERP.Models;
using SchoolERP.Services;

namespace SchoolERP.ViewModels
{
    public class AttendanceViewModel : ViewModelBase
    {
        private readonly AttendanceRepository repository = new AttendanceRepository();
        private readonly IFingerprintDeviceClient deviceClient = FingerprintDeviceFactory.Create();
        private DateTime selectedDate;
        private DateTime monthlySelectedDate;
        private string syncStatusMessage;
        private string monthlyStatusMessage;
        private bool isSyncing;
        private bool isLoading;
        private bool isLoadingMonthlySummary;
        private int monthlyPresentDaysTotal;
        private int monthlyAbsentDaysTotal;
        private int monthlyNotMarkedDaysTotal;

        public AttendanceViewModel()
        {
            TeacherRows = new ObservableCollection<TeacherAttendanceRowViewModel>();
            MonthlySummaryRows = new ObservableCollection<TeacherAttendanceSummary>();
            selectedDate = DateTime.Today;
            monthlySelectedDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

            LoadAttendanceCommand = new RelayCommand(async _ => await LoadAttendanceAsync());
            LoadMonthlySummaryCommand = new RelayCommand(async _ => await LoadMonthlySummaryAsync());
            PreviousDayCommand = new RelayCommand(_ => ChangeDate(-1));
            NextDayCommand = new RelayCommand(_ => ChangeDate(1));
            PreviousMonthCommand = new RelayCommand(_ => ChangeMonth(-1));
            NextMonthCommand = new RelayCommand(_ => ChangeMonth(1));
            MarkPresentCommand = new RelayCommand<TeacherAttendanceRowViewModel>(async row =>
            {
                if (row == null) return;
                await row.MarkPresentAsync().ConfigureAwait(true);
                await LoadAttendanceAsync().ConfigureAwait(true);
                await LoadMonthlySummaryAsync().ConfigureAwait(true);
            });
            MarkAbsentCommand = new RelayCommand<TeacherAttendanceRowViewModel>(async row =>
            {
                if (row == null) return;
                await row.MarkAbsentAsync().ConfigureAwait(true);
                await LoadAttendanceAsync().ConfigureAwait(true);
                await LoadMonthlySummaryAsync().ConfigureAwait(true);
            });
            SyncFingerprintCommand = new RelayCommand(async _ => await SyncFingerprintAsync(), _ => !IsSyncing);

            _ = LoadAttendanceAsync();
            _ = LoadMonthlySummaryAsync();
        }

        public DateTime SelectedDate
        {
            get => selectedDate;
            set
            {
                if (SetProperty(ref selectedDate, value))
                {
                    _ = LoadAttendanceAsync();
                }
            }
        }

        public ObservableCollection<TeacherAttendanceRowViewModel> TeacherRows { get; }
        public ObservableCollection<TeacherAttendanceSummary> MonthlySummaryRows { get; }

        public int PresentTodayCount => TeacherRows.Count(r => r.Status == "Present");
        public int AbsentTodayCount => TeacherRows.Count(r => r.Status == "Absent");
        public int NotMarkedCount => TeacherRows.Count(r => r.Status == "Not Marked");
        public string FooterText => $"Showing {TeacherRows.Count} teachers for {SelectedDate:dddd, dd MMM yyyy}";

        public DateTime MonthlySelectedDate
        {
            get => monthlySelectedDate;
            set
            {
                if (SetProperty(ref monthlySelectedDate, new DateTime(value.Year, value.Month, 1)))
                {
                    _ = LoadMonthlySummaryAsync();
                }
            }
        }

        public string MonthlyMonthLabel => MonthlySelectedDate.ToString("MMMM yyyy");
        public string MonthlyFooterText => $"Showing {MonthlySummaryRows.Count} teachers for {MonthlyMonthLabel}";

        public int MonthlyPresentDaysTotal
        {
            get => monthlyPresentDaysTotal;
            set => SetProperty(ref monthlyPresentDaysTotal, value);
        }

        public int MonthlyAbsentDaysTotal
        {
            get => monthlyAbsentDaysTotal;
            set => SetProperty(ref monthlyAbsentDaysTotal, value);
        }

        public int MonthlyNotMarkedDaysTotal
        {
            get => monthlyNotMarkedDaysTotal;
            set => SetProperty(ref monthlyNotMarkedDaysTotal, value);
        }

        public string SyncStatusMessage
        {
            get => syncStatusMessage;
            set => SetProperty(ref syncStatusMessage, value);
        }

        public string MonthlyStatusMessage
        {
            get => monthlyStatusMessage;
            set => SetProperty(ref monthlyStatusMessage, value);
        }

        public bool IsSyncing
        {
            get => isSyncing;
            set
            {
                if (SetProperty(ref isSyncing, value))
                {
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public bool IsLoading
        {
            get => isLoading;
            set => SetProperty(ref isLoading, value);
        }

        public bool IsLoadingMonthlySummary
        {
            get => isLoadingMonthlySummary;
            set => SetProperty(ref isLoadingMonthlySummary, value);
        }

        public ICommand LoadAttendanceCommand { get; }
        public ICommand LoadMonthlySummaryCommand { get; }
        public ICommand PreviousDayCommand { get; }
        public ICommand NextDayCommand { get; }
        public ICommand PreviousMonthCommand { get; }
        public ICommand NextMonthCommand { get; }
        public ICommand MarkPresentCommand { get; }
        public ICommand MarkAbsentCommand { get; }
        public ICommand SyncFingerprintCommand { get; }

        private async Task LoadAttendanceAsync()
        {
            IsLoading = true;
            SyncStatusMessage = string.Empty;

            try
            {
                var data = await repository.GetTeacherAttendanceByDateAsync(SelectedDate).ConfigureAwait(true);

                TeacherRows.Clear();
                foreach (var row in data.Select(m => new TeacherAttendanceRowViewModel(repository, m)))
                {
                    TeacherRows.Add(row);
                }

                OnPropertyChanged(nameof(PresentTodayCount));
                OnPropertyChanged(nameof(AbsentTodayCount));
                OnPropertyChanged(nameof(NotMarkedCount));
                OnPropertyChanged(nameof(FooterText));
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Failed to load attendance: " + ex.Message,
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ChangeDate(int days)
        {
            SelectedDate = SelectedDate.AddDays(days);
        }

        private void ChangeMonth(int months)
        {
            MonthlySelectedDate = MonthlySelectedDate.AddMonths(months);
        }

        private async Task LoadMonthlySummaryAsync()
        {
            IsLoadingMonthlySummary = true;
            MonthlyStatusMessage = string.Empty;

            try
            {
                var summary = await repository.GetMonthlyAttendanceSummaryAsync(MonthlySelectedDate.Year, MonthlySelectedDate.Month).ConfigureAwait(true);

                MonthlySummaryRows.Clear();
                foreach (var row in summary.Rows.OrderBy(r => r.Name))
                {
                    MonthlySummaryRows.Add(row);
                }

                MonthlyPresentDaysTotal = MonthlySummaryRows.Sum(r => r.PresentDays);
                MonthlyAbsentDaysTotal = MonthlySummaryRows.Sum(r => r.AbsentDays);
                MonthlyNotMarkedDaysTotal = MonthlySummaryRows.Sum(r => r.NotMarkedDays);

                OnPropertyChanged(nameof(MonthlyMonthLabel));
                OnPropertyChanged(nameof(MonthlyFooterText));
                MonthlyStatusMessage = $"Showing monthly sheet for {MonthlyMonthLabel}";
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Failed to load monthly attendance summary: " + ex.Message,
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
            finally
            {
                IsLoadingMonthlySummary = false;
            }
        }

        private async Task SyncFingerprintAsync()
        {
            IsSyncing = true;
            SyncStatusMessage = "Syncing...";

            try
            {
                var syncService = new FingerprintSyncService(deviceClient);
                var result = await Task.Run(() => syncService.SyncTeacherAttendance()).ConfigureAwait(true);

                SyncStatusMessage =
                    $"Sync complete! Received: {result.ReceivedLogs}, Inserted: {result.InsertedAttendanceRows}, Skipped: {result.SkippedLogs}";

                await LoadAttendanceAsync();
                await LoadMonthlySummaryAsync();
            }
            catch (Exception ex)
            {
                SyncStatusMessage = "Sync failed: " + ex.Message;
                MessageBox.Show(
                    "Failed to sync fingerprints: " + ex.Message,
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
            finally
            {
                IsSyncing = false;
            }
        }
    }
}
