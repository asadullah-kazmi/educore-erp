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
    public class StudentListViewModel : ViewModelBase
    {
        private readonly StudentRepository repository = new StudentRepository();
        private string searchText = string.Empty;

        public StudentListViewModel()
        {
            Students = new ObservableCollection<StudentViewModel>();
            FilteredStudents = new ObservableCollection<StudentViewModel>();

            AddStudentCommand = new RelayCommand(_ => OpenAddStudent(), _ => CanAddStudent);
            EditStudentCommand = new RelayCommand<StudentViewModel>(OpenEditStudent, _ => CanManageStudents);
            DeleteStudentCommand = new RelayCommand<StudentViewModel>(student => DeleteStudent(student), _ => CanManageStudents);

            _ = LoadStudentsAsync();
        }

        public ObservableCollection<StudentViewModel> Students { get; }

        public ObservableCollection<StudentViewModel> FilteredStudents { get; }

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

        public bool CanAddStudent =>
            string.Equals(AppSession.CurrentRole, "Admin", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(AppSession.CurrentRole, "Staff", StringComparison.OrdinalIgnoreCase);

        public bool CanManageStudents => CanAddStudent;

        public string StatusText => $"Showing {FilteredStudents.Count} of {Students.Count} students";

        public ICommand AddStudentCommand { get; }

        public ICommand EditStudentCommand { get; }

        public ICommand DeleteStudentCommand { get; }

        public async Task LoadStudentsAsync()
        {
            try
            {
                var students = await repository.GetAllStudentsAsync().ConfigureAwait(true);

                Students.Clear();
                foreach (var student in students.Select(StudentViewModel.FromModel))
                {
                    Students.Add(student);
                }

                ApplyFilter();
                OnPropertyChanged(nameof(StatusText));
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to load students: " + ex.Message, "Students", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyFilter()
        {
            var query = (SearchText ?? string.Empty).Trim();

            FilteredStudents.Clear();

            var filtered = string.IsNullOrEmpty(query)
                ? Students
                : Students.Where(s =>
                    (!string.IsNullOrEmpty(s.Name) && s.Name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (!string.IsNullOrEmpty(s.RegistrationNo) && s.RegistrationNo.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0));

            foreach (var student in filtered)
            {
                FilteredStudents.Add(student);
            }

            OnPropertyChanged(nameof(StatusText));
        }

        private void OpenAddStudent()
        {
            var window = new AddEditStudentWindow(null);
            window.Owner = Application.Current.MainWindow;
            if (window.ShowDialog() == true)
            {
                _ = LoadStudentsAsync();
            }
        }

        private void OpenEditStudent(StudentViewModel student)
        {
            if (student == null)
            {
                return;
            }

            var window = new AddEditStudentWindow(student.StudentID);
            window.Owner = Application.Current.MainWindow;
            if (window.ShowDialog() == true)
            {
                _ = LoadStudentsAsync();
            }
        }

        private async void DeleteStudent(StudentViewModel student)
        {
            if (student == null)
            {
                return;
            }

            var confirmed = ConfirmationDialog.Show(
                $"Are you sure you want to delete student \"{student.Name}\"?",
                "Confirm Delete");

            if (!confirmed)
            {
                return;
            }

            try
            {
                var deleted = await repository.DeleteStudentAsync(student.StudentID).ConfigureAwait(true);
                if (deleted)
                {
                    await LoadStudentsAsync().ConfigureAwait(true);
                }
                else
                {
                    MessageBox.Show("Unable to delete the selected student.", "Students", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to delete student: " + ex.Message, "Students", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
