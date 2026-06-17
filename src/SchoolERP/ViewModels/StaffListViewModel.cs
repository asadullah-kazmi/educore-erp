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

        public StaffListViewModel()
        {
            Staff = new ObservableCollection<StaffViewModel>();
            FilteredStaff = new ObservableCollection<StaffViewModel>();

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

            FilteredStaff.Clear();

            var filtered = string.IsNullOrEmpty(query)
                ? Staff
                : Staff.Where(s =>
                    !string.IsNullOrEmpty(s.Name) && s.Name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0);

            foreach (var member in filtered)
            {
                FilteredStaff.Add(member);
            }

            OnPropertyChanged(nameof(StatusText));
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
    }
}
