using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using SchoolERP.Data;
using SchoolERP.Repositories;
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
            LoadStudentsCommand = new RelayCommand(async _ => await LoadStudentsAsync());
            EditStudentCommand = new RelayCommand<StudentViewModel>(student =>
            {
                if (student == null) return;
                var window = new AddEditStudentWindow(student.StudentID)
                {
                    Owner = Application.Current.MainWindow
                };
                if (window.ShowDialog() == true)
                {
                    LoadStudentsCommand.Execute(null);
                }
            });
            DeleteStudentCommand = new RelayCommand<StudentViewModel>(async student =>
            {
                if (student == null) return;
                var result = MessageBox.Show(
                    $"Are you sure you want to delete {student.Name}?",
                    "Confirm Delete",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    await repository.DeleteStudentAsync(student.StudentID);
                    LoadStudentsCommand.Execute(null);
                }
            });
            ViewStudentDetailCommand = new RelayCommand<StudentViewModel>(async student => await OpenStudentDetailAsync(student));

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
        
        public ICommand LoadStudentsCommand { get; }

        public RelayCommand<StudentViewModel> EditStudentCommand { get; }

        public RelayCommand<StudentViewModel> DeleteStudentCommand { get; }

        public RelayCommand<StudentViewModel> ViewStudentDetailCommand { get; }

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
                LoadStudentsCommand.Execute(null);
            }
        }

        private async Task OpenStudentDetailAsync(StudentViewModel student)
        {
            if (student == null) return;
            var feeRepo = new FeeRepository();
            var fees = await feeRepo.GetFeesByStudentAsync(student.StudentID).ConfigureAwait(true);
            var window = new StudentDetailWindow(student, fees)
            {
                Owner = Application.Current.MainWindow
            };
            window.ShowDialog();
        }
    }
}
