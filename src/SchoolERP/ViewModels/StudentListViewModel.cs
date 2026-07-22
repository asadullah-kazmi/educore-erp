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
        private string statusFilter = "Active";

        public StudentListViewModel()
        {
            Students = new ObservableCollection<StudentViewModel>();
            FilteredStudents = new ObservableCollection<StudentViewModel>();
            ClassFilterOptions = new ObservableCollection<string> { "All" };
            SectionFilterOptions = new ObservableCollection<string> { "All" };
            StatusFilterOptions = new ObservableCollection<string>
            {
                "Active",
                "Inactive",
                "All"
            };

            ClearFiltersCommand = new RelayCommand(_ => ClearFilters());
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
                    try
                    {
                        await repository.DeleteStudentAsync(student.StudentID);
                        LoadStudentsCommand.Execute(null);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Failed to delete student: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            });
            ViewStudentDetailCommand = new RelayCommand<StudentViewModel>(async student => await OpenStudentDetailAsync(student));

            _ = LoadStudentsAsync();
        }

        public ObservableCollection<StudentViewModel> Students { get; }

        public ObservableCollection<StudentViewModel> FilteredStudents { get; }
        
        public ObservableCollection<string> StatusFilterOptions { get; }
        
        public ObservableCollection<string> ClassFilterOptions { get; }
        
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

        private string classFilter = "All";
        public string ClassFilter
        {
            get => classFilter;
            set
            {
                if (SetProperty(ref classFilter, value))
                {
                    ApplyFilter();
                }
            }
        }

        private string sectionFilter = "All";
        public string SectionFilter
        {
            get => sectionFilter;
            set
            {
                if (SetProperty(ref sectionFilter, value))
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

        public ICommand ClearFiltersCommand { get; }

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

                // Update Class Filter Options
                var classes = Students.Where(s => !string.IsNullOrEmpty(s.ClassName)).Select(s => s.ClassName).Distinct().OrderBy(c => c).ToList();
                ClassFilterOptions.Clear();
                ClassFilterOptions.Add("All");
                foreach (var c in classes) ClassFilterOptions.Add(c);

                // Update Section Filter Options
                var sections = Students.Where(s => !string.IsNullOrEmpty(s.Section)).Select(s => s.Section).Distinct().OrderBy(s => s).ToList();
                SectionFilterOptions.Clear();
                SectionFilterOptions.Add("All");
                foreach (var s in sections) SectionFilterOptions.Add(s);

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

            var filtered = Students.Where(s =>
            {
                bool matchesSearch = string.IsNullOrEmpty(query) ||
                    (!string.IsNullOrEmpty(s.Name) && s.Name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (!string.IsNullOrEmpty(s.RegistrationNo) && s.RegistrationNo.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0);

                bool matchesStatus = true;
                if (StatusFilter == "Active") matchesStatus = s.IsActive;
                else if (StatusFilter == "Inactive") matchesStatus = !s.IsActive;

                bool matchesClass = ClassFilter == "All" || string.Equals(s.ClassName, ClassFilter, StringComparison.OrdinalIgnoreCase);
                bool matchesSection = SectionFilter == "All" || string.Equals(s.Section, SectionFilter, StringComparison.OrdinalIgnoreCase);

                return matchesSearch && matchesStatus && matchesClass && matchesSection;
            });

            foreach (var student in filtered)
            {
                FilteredStudents.Add(student);
            }

            OnPropertyChanged(nameof(StatusText));
        }

        private void ClearFilters()
        {
            SearchText = string.Empty;
            StatusFilter = "Active";
            ClassFilter = "All";
            SectionFilter = "All";
            ApplyFilter();
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
