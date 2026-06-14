using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using SchoolERP.Controls;
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
            EditStaffCommand = new RelayCommand<StaffViewModel>(OpenEditStaff, _ => CanManageStaff);
            DeleteStaffCommand = new RelayCommand<StaffViewModel>(member => DeleteStaff(member), _ => CanManageStaff);

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

        public ICommand EditStaffCommand { get; }

        public ICommand DeleteStaffCommand { get; }

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
                _ = LoadStaffAsync();
            }
        }

        private void OpenEditStaff(StaffViewModel member)
        {
            if (member == null)
            {
                return;
            }

            var window = new AddEditStaffWindow(member.TeacherID);
            window.Owner = Application.Current.MainWindow;
            if (window.ShowDialog() == true)
            {
                _ = LoadStaffAsync();
            }
        }

        private async void DeleteStaff(StaffViewModel member)
        {
            if (member == null)
            {
                return;
            }

            var confirmed = ConfirmationDialog.Show(
                $"Are you sure you want to delete staff member \"{member.Name}\"?",
                "Confirm Delete");

            if (!confirmed)
            {
                return;
            }

            try
            {
                var deleted = await repository.DeleteStaffAsync(member.TeacherID).ConfigureAwait(true);
                if (deleted)
                {
                    await LoadStaffAsync().ConfigureAwait(true);
                }
                else
                {
                    MessageBox.Show("Unable to delete the selected staff member.", "Staff", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to delete staff: " + ex.Message, "Staff", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
