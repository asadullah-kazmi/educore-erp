using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using SchoolERP.Data;
using SchoolERP.Models;

namespace SchoolERP.ViewModels
{
    public class AddEditStudentViewModel : ViewModelBase
    {
        private readonly StudentRepository repository = new StudentRepository();
        private readonly int? studentId;
        private string registrationNo;
        private string name;
        private string fatherName;
        private DateTime? dob;
        private int? selectedClassId;
        private string address;
        private string phone;
        private DateTime? admissionDate = DateTime.Today;
        private string registrationNoError;
        private string nameError;
        private bool isSaving;

        public AddEditStudentViewModel(int? studentId)
        {
            this.studentId = studentId;
            IsEditMode = studentId.HasValue;
            WindowTitle = IsEditMode ? "Edit Student" : "Add Student";

            Classes = new ObservableCollection<Class>();
            SaveCommand = new RelayCommand(_ => SaveAsync(), _ => !IsSaving);
            CancelCommand = new RelayCommand(_ => RequestClose?.Invoke(false));

            _ = InitializeAsync();
        }

        public event Action<bool> RequestClose;

        public bool IsEditMode { get; }

        public string WindowTitle { get; }

        public ObservableCollection<Class> Classes { get; }

        public string RegistrationNo
        {
            get => registrationNo;
            set => SetProperty(ref registrationNo, value);
        }

        public string Name
        {
            get => name;
            set => SetProperty(ref name, value);
        }

        public string FatherName
        {
            get => fatherName;
            set => SetProperty(ref fatherName, value);
        }

        public DateTime? DOB
        {
            get => dob;
            set => SetProperty(ref dob, value);
        }

        public int? SelectedClassId
        {
            get => selectedClassId;
            set => SetProperty(ref selectedClassId, value);
        }

        public string Address
        {
            get => address;
            set => SetProperty(ref address, value);
        }

        public string Phone
        {
            get => phone;
            set => SetProperty(ref phone, value);
        }

        public DateTime? AdmissionDate
        {
            get => admissionDate;
            set => SetProperty(ref admissionDate, value);
        }

        public string RegistrationNoError
        {
            get => registrationNoError;
            set => SetProperty(ref registrationNoError, value);
        }

        public string NameError
        {
            get => nameError;
            set => SetProperty(ref nameError, value);
        }

        public bool IsSaving
        {
            get => isSaving;
            private set => SetProperty(ref isSaving, value);
        }

        public ICommand SaveCommand { get; }

        public ICommand CancelCommand { get; }

        private async Task InitializeAsync()
        {
            try
            {
                var classes = await repository.GetAllClassesAsync().ConfigureAwait(true);
                Classes.Clear();
                foreach (var item in classes)
                {
                    Classes.Add(item);
                }

                if (IsEditMode && studentId.HasValue)
                {
                    var student = await repository.GetStudentByIdAsync(studentId.Value).ConfigureAwait(true);
                    if (student != null)
                    {
                        RegistrationNo = student.RegistrationNo;
                        Name = student.Name;
                        FatherName = student.FatherName;
                        DOB = student.DOB;
                        SelectedClassId = student.ClassID;
                        Address = student.Address;
                        Phone = student.Phone;
                        AdmissionDate = student.AdmissionDate ?? DateTime.Today;
                    }
                }
            }
            catch (Exception ex)
            {
                RegistrationNoError = "Failed to load student data: " + ex.Message;
            }
        }

        private async void SaveAsync()
        {
            RegistrationNoError = null;
            NameError = null;

            if (string.IsNullOrWhiteSpace(RegistrationNo))
            {
                RegistrationNoError = "Registration number is required.";
            }

            if (string.IsNullOrWhiteSpace(Name))
            {
                NameError = "Full name is required.";
            }

            if (!string.IsNullOrEmpty(RegistrationNoError) || !string.IsNullOrEmpty(NameError))
            {
                return;
            }

            IsSaving = true;

            try
            {
                var regNo = RegistrationNo.Trim();
                var exists = await repository.RegistrationNoExistsAsync(regNo, studentId).ConfigureAwait(true);
                if (exists)
                {
                    RegistrationNoError = "Registration number already exists.";
                    return;
                }

                var selectedClass = Classes.FirstOrDefault(c => c.ClassID == SelectedClassId);
                var student = new Student
                {
                    StudentID = studentId ?? 0,
                    RegistrationNo = regNo,
                    Name = Name.Trim(),
                    FatherName = string.IsNullOrWhiteSpace(FatherName) ? null : FatherName.Trim(),
                    DOB = DOB,
                    ClassID = SelectedClassId,
                    ClassName = selectedClass?.ClassName,
                    Address = string.IsNullOrWhiteSpace(Address) ? null : Address.Trim(),
                    Phone = string.IsNullOrWhiteSpace(Phone) ? null : Phone.Trim(),
                    AdmissionDate = AdmissionDate ?? DateTime.Today
                };

                bool success;
                if (IsEditMode)
                {
                    success = await repository.UpdateStudentAsync(student).ConfigureAwait(true);
                }
                else
                {
                    success = await repository.AddStudentAsync(student).ConfigureAwait(true);
                }

                if (success)
                {
                    RequestClose?.Invoke(true);
                }
                else
                {
                    RegistrationNoError = "Unable to save student. Please try again.";
                }
            }
            catch (Exception ex)
            {
                RegistrationNoError = "Save failed: " + ex.Message;
            }
            finally
            {
                IsSaving = false;
            }
        }
    }
}
