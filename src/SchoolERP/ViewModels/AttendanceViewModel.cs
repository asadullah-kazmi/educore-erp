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
        private string syncStatusMessage;
        private bool isSyncing;
        private bool isLoading;

        public AttendanceViewModel()
        {
            TeacherRows = new ObservableCollection<TeacherAttendanceRowViewModel>();
            selectedDate = DateTime.Today;

            LoadAttendanceCommand = new RelayCommand(async _ => await LoadAttendanceAsync());
            PreviousDayCommand = new RelayCommand(_ => ChangeDate(-1));
            NextDayCommand = new RelayCommand(_ => ChangeDate(1));
            MarkPresentCommand = new RelayCommand<TeacherAttendanceRowViewModel>(row =>
            {
                if (row == null) return;
                row.IsPresent = true;
            });
            MarkAbsentCommand = new RelayCommand<TeacherAttendanceRowViewModel>(row =>
            {
                if (row == null) return;
                row.IsAbsent = true;
            });
            SyncFingerprintCommand = new RelayCommand(async _ => await SyncFingerprintAsync(), _ => !IsSyncing);

            _ = LoadAttendanceAsync();
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

        public int PresentTodayCount => TeacherRows.Count(r => r.Status == "Present");
        public int AbsentTodayCount => TeacherRows.Count(r => r.Status == "Absent");
        public int NotMarkedCount => TeacherRows.Count(r => r.Status == "Not Marked");
        public string FooterText => $"Showing {TeacherRows.Count} teachers for {SelectedDate:dddd, dd MMM yyyy}";

        public string SyncStatusMessage
        {
            get => syncStatusMessage;
            set => SetProperty(ref syncStatusMessage, value);
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

        public ICommand LoadAttendanceCommand { get; }
        public ICommand PreviousDayCommand { get; }
        public ICommand NextDayCommand { get; }
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
