using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using SchoolERP.Data;
using SchoolERP.Services;
using SchoolERP.Views;

namespace SchoolERP.ViewModels
{
    public class StaffListViewModel : ViewModelBase
    {
        private readonly StaffRepository repository = new StaffRepository();
        private string searchText = string.Empty;
        private string selectedStaffType = "All Staff Types";

        public StaffListViewModel()
        {
            Staff = new ObservableCollection<StaffViewModel>();
            FilteredStaff = new ObservableCollection<StaffViewModel>();
            StaffTypeOptions = new ObservableCollection<string> { "All Staff Types" };

            AddStaffCommand = new RelayCommand(_ => OpenAddStaff(), _ => CanAddStaff);
            LoadStaffCommand = new RelayCommand(async _ => await LoadStaffAsync());
            EditStaffCommand = new RelayCommand<StaffViewModel>(staff =>
            {
                if (staff == null) return;
                var window = new AddEditStaffWindow(staff.TeacherID)
                {
                    Owner = Application.Current.MainWindow
                };
                if (window.ShowDialog() == true)
                {
                    LoadStaffCommand.Execute(null);
                }
            });
            DeleteStaffCommand = new RelayCommand<StaffViewModel>(async staff =>
            {
                if (staff == null) return;
                var result = MessageBox.Show(
                    $"Are you sure you want to delete {staff.Name}?",
                    "Confirm Delete",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    await repository.DeleteStaffAsync(staff.TeacherID);
                    LoadStaffCommand.Execute(null);
                }
            });
            ViewStaffDetailCommand = new RelayCommand<StaffViewModel>(staff => OpenStaffDetail(staff));

            _ = LoadStaffAsync();
        }

        public ObservableCollection<StaffViewModel> Staff { get; }

        public ObservableCollection<StaffViewModel> FilteredStaff { get; }

        public ObservableCollection<string> StaffTypeOptions { get; }

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

        public string SelectedStaffType
        {
            get => selectedStaffType;
            set
            {
                if (SetProperty(ref selectedStaffType, value))
                {
                    ApplyFilter();
                }
            }
        }

        public bool CanAddStaff =>
            string.Equals(AppSession.CurrentRole, "Admin", StringComparison.OrdinalIgnoreCase);

        public bool CanManageStaff => CanAddStaff;

        public string StatusText => $"Showing {FilteredStaff.Count} of {Staff.Count} staff";

        public ICommand AddStaffCommand { get; }
        
        public ICommand LoadStaffCommand { get; }

        public RelayCommand<StaffViewModel> EditStaffCommand { get; }

        public RelayCommand<StaffViewModel> DeleteStaffCommand { get; }

        public RelayCommand<StaffViewModel> ViewStaffDetailCommand { get; }

        public async Task LoadStaffAsync()
        {
            try
            {
                var staff = await repository.GetAllStaffAsync().ConfigureAwait(true);

                Staff.Clear();
                foreach (var member in staff.Select(StaffViewModel.FromModel))
                {
                    Staff.Add(member);
                }

                RefreshStaffTypeOptions();
                ApplyFilter();
                OnPropertyChanged(nameof(StatusText));
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to load staff: " + ex.Message, "Staff", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyFilter()
        {
            var query = (SearchText ?? string.Empty).Trim();
            var staffType = (SelectedStaffType ?? string.Empty).Trim();

            FilteredStaff.Clear();

            var filtered = Staff.AsEnumerable();

            if (!string.IsNullOrEmpty(staffType) && !string.Equals(staffType, "All Staff Types", StringComparison.OrdinalIgnoreCase))
            {
                filtered = filtered.Where(s => string.Equals(GetStaffTypeValue(s), staffType, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrEmpty(query))
            {
                filtered = filtered.Where(s =>
                    ContainsText(s.Name, query) ||
                    ContainsText(s.StaffType, query) ||
                    ContainsText(s.Designation, query));
            }

            foreach (var member in filtered)
            {
                FilteredStaff.Add(member);
            }

            OnPropertyChanged(nameof(StatusText));
        }

        private void RefreshStaffTypeOptions()
        {
            var selected = SelectedStaffType;
            StaffTypeOptions.Clear();
            StaffTypeOptions.Add("All Staff Types");

            foreach (var staffType in Staff
                .Select(GetStaffTypeValue)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(type => type))
            {
                StaffTypeOptions.Add(staffType);
            }

            SelectedStaffType = StaffTypeOptions.Contains(selected) ? selected : "All Staff Types";
        }

        private void OpenAddStaff()
        {
            var window = new AddEditStaffWindow(null);
            window.Owner = Application.Current.MainWindow;
            if (window.ShowDialog() == true)
            {
                LoadStaffCommand.Execute(null);
            }
        }

        private void OpenStaffDetail(StaffViewModel staff)
        {
            if (staff == null) return;
            var window = new StaffDetailWindow(staff) { Owner = Application.Current.MainWindow };
            window.ShowDialog();
        }

        private static bool ContainsText(string value, string query)
        {
            return !string.IsNullOrWhiteSpace(value) &&
                   value.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string GetStaffTypeValue(StaffViewModel staff)
        {
            return string.IsNullOrWhiteSpace(staff?.StaffType)
                ? "Teacher"
                : staff.StaffType.Trim();
        }
    }
}
