using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Win32;
using SchoolERP.Data;
using SchoolERP.Models;

namespace SchoolERP.ViewModels
{
    public class AddEditStaffViewModel : ViewModelBase
    {
        private readonly StaffRepository repository = new StaffRepository();
        private readonly int? teacherId;
        private string name;
        private string ageText;
        private string experience;
        private DateTime? dob;
        private string contactNumber;
        private DateTime? dateOfJoining;
        private string designation;
        private string salaryText;
        private string address;
        private string cnicNumber;
        private string cnicFrontImagePath;
        private string cnicBackImagePath;
        private string educationalDocumentsPath;
        private string certificatesPath;
        private string fingerprintIdText;
        private string nameError;
        private string ageError;
        private string dobError;
        private string contactNumberError;
        private string dateOfJoiningError;
        private string designationError;
        private string salaryError;
        private string cnicNumberError;
        private string fingerprintIdError;
        private bool isSaving;

        public AddEditStaffViewModel(int? teacherId)
        {
            this.teacherId = teacherId;
            IsEditMode = teacherId.HasValue;
            WindowTitle = IsEditMode ? "Edit Staff" : "Add Staff";

            SaveCommand = new RelayCommand(_ => SaveAsync(), _ => !IsSaving);
            CancelCommand = new RelayCommand(_ => RequestClose?.Invoke(false));
            BrowseCnicFrontCommand = new RelayCommand(_ => SetPickedFile(path => CnicFrontImagePath = path, "Image files|*.jpg;*.jpeg;*.png;*.bmp;*.pdf|All files|*.*", false));
            BrowseCnicBackCommand = new RelayCommand(_ => SetPickedFile(path => CnicBackImagePath = path, "Image files|*.jpg;*.jpeg;*.png;*.bmp;*.pdf|All files|*.*", false));
            BrowseEducationalDocumentsCommand = new RelayCommand(_ => SetPickedFile(path => EducationalDocumentsPath = path, "Documents|*.pdf;*.doc;*.docx;*.jpg;*.jpeg;*.png|All files|*.*", true));
            BrowseCertificatesCommand = new RelayCommand(_ => SetPickedFile(path => CertificatesPath = path, "Documents|*.pdf;*.doc;*.docx;*.jpg;*.jpeg;*.png|All files|*.*", true));

            if (IsEditMode && teacherId.HasValue)
            {
                _ = LoadStaffAsync(teacherId.Value);
            }
        }

        public event Action<bool> RequestClose;

        public bool IsEditMode { get; }

        public string WindowTitle { get; }

        public string Name
        {
            get => name;
            set => SetProperty(ref name, value);
        }

        public string AgeText
        {
            get => ageText;
            set => SetProperty(ref ageText, value);
        }

        public string Experience
        {
            get => experience;
            set => SetProperty(ref experience, value);
        }

        public DateTime? DOB
        {
            get => dob;
            set => SetProperty(ref dob, value);
        }

        public string ContactNumber
        {
            get => contactNumber;
            set => SetProperty(ref contactNumber, value);
        }

        public DateTime? DateOfJoining
        {
            get => dateOfJoining;
            set => SetProperty(ref dateOfJoining, value);
        }

        public string Designation
        {
            get => designation;
            set => SetProperty(ref designation, value);
        }

        public string SalaryText
        {
            get => salaryText;
            set => SetProperty(ref salaryText, value);
        }

        public string Address
        {
            get => address;
            set => SetProperty(ref address, value);
        }

        public string CnicNumber
        {
            get => cnicNumber;
            set => SetProperty(ref cnicNumber, value);
        }

        public string CnicFrontImagePath
        {
            get => cnicFrontImagePath;
            set => SetProperty(ref cnicFrontImagePath, value);
        }

        public string CnicBackImagePath
        {
            get => cnicBackImagePath;
            set => SetProperty(ref cnicBackImagePath, value);
        }

        public string EducationalDocumentsPath
        {
            get => educationalDocumentsPath;
            set => SetProperty(ref educationalDocumentsPath, value);
        }

        public string CertificatesPath
        {
            get => certificatesPath;
            set => SetProperty(ref certificatesPath, value);
        }

        public string FingerprintIdText
        {
            get => fingerprintIdText;
            set => SetProperty(ref fingerprintIdText, value);
        }

        public string NameError
        {
            get => nameError;
            set => SetProperty(ref nameError, value);
        }

        public string AgeError
        {
            get => ageError;
            set => SetProperty(ref ageError, value);
        }

        public string DOBError
        {
            get => dobError;
            set => SetProperty(ref dobError, value);
        }

        public string ContactNumberError
        {
            get => contactNumberError;
            set => SetProperty(ref contactNumberError, value);
        }

        public string DateOfJoiningError
        {
            get => dateOfJoiningError;
            set => SetProperty(ref dateOfJoiningError, value);
        }

        public string DesignationError
        {
            get => designationError;
            set => SetProperty(ref designationError, value);
        }

        public string SalaryError
        {
            get => salaryError;
            set => SetProperty(ref salaryError, value);
        }

        public string CnicNumberError
        {
            get => cnicNumberError;
            set => SetProperty(ref cnicNumberError, value);
        }

        public string FingerprintIdError
        {
            get => fingerprintIdError;
            set => SetProperty(ref fingerprintIdError, value);
        }

        public bool IsSaving
        {
            get => isSaving;
            private set => SetProperty(ref isSaving, value);
        }

        public ICommand SaveCommand { get; }

        public ICommand CancelCommand { get; }

        public ICommand BrowseCnicFrontCommand { get; }

        public ICommand BrowseCnicBackCommand { get; }

        public ICommand BrowseEducationalDocumentsCommand { get; }

        public ICommand BrowseCertificatesCommand { get; }

        private async Task LoadStaffAsync(int id)
        {
            try
            {
                var teacher = await repository.GetStaffByIdAsync(id).ConfigureAwait(true);
                if (teacher != null)
                {
                    Name = teacher.Name;
                    AgeText = teacher.Age?.ToString();
                    Experience = teacher.Experience;
                    DOB = teacher.DOB;
                    ContactNumber = teacher.ContactNumber;
                    DateOfJoining = teacher.DateOfJoining;
                    Designation = teacher.Designation;
                    SalaryText = teacher.Salary.ToString("0.##");
                    Address = teacher.Address;
                    CnicNumber = teacher.CnicNumber;
                    CnicFrontImagePath = teacher.CnicFrontImagePath;
                    CnicBackImagePath = teacher.CnicBackImagePath;
                    EducationalDocumentsPath = teacher.EducationalDocumentsPath;
                    CertificatesPath = teacher.CertificatesPath;
                    FingerprintIdText = teacher.FingerprintID?.ToString();
                }
            }
            catch (Exception ex)
            {
                NameError = "Failed to load staff data: " + ex.Message;
            }
        }

        private async void SaveAsync()
        {
            NameError = null;
            AgeError = null;
            DOBError = null;
            ContactNumberError = null;
            DateOfJoiningError = null;
            DesignationError = null;
            SalaryError = null;
            CnicNumberError = null;
            FingerprintIdError = null;

            if (string.IsNullOrWhiteSpace(Name))
            {
                NameError = "Full name is required.";
            }

            int? age = null;
            if (!string.IsNullOrWhiteSpace(AgeText))
            {
                if (!int.TryParse(AgeText.Trim(), out var parsedAge) || parsedAge <= 0)
                {
                    AgeError = "Age must be a valid number greater than zero.";
                }
                else
                {
                    age = parsedAge;
                }
            }

            if (!DOB.HasValue)
            {
                DOBError = "DOB is required.";
            }

            if (string.IsNullOrWhiteSpace(ContactNumber))
            {
                ContactNumberError = "Contact number is required.";
            }

            if (!DateOfJoining.HasValue)
            {
                DateOfJoiningError = "Date of joining is required.";
            }

            if (string.IsNullOrWhiteSpace(Designation))
            {
                DesignationError = "Designation is required.";
            }

            if (!decimal.TryParse(SalaryText, out var salary) || salary <= 0)
            {
                SalaryError = "Base salary is required and must be greater than zero.";
            }

            if (string.IsNullOrWhiteSpace(CnicNumber))
            {
                CnicNumberError = "CNIC number is required.";
            }

            int? fingerprintId = null;
            if (!string.IsNullOrWhiteSpace(FingerprintIdText))
            {
                if (!int.TryParse(FingerprintIdText.Trim(), out var parsedFingerprintId))
                {
                    FingerprintIdError = "Fingerprint ID must be a valid number.";
                }
                else
                {
                    fingerprintId = parsedFingerprintId;
                }
            }

            if (!string.IsNullOrEmpty(NameError) || !string.IsNullOrEmpty(AgeError) ||
                !string.IsNullOrEmpty(DOBError) ||
                !string.IsNullOrEmpty(ContactNumberError) || !string.IsNullOrEmpty(DateOfJoiningError) ||
                !string.IsNullOrEmpty(DesignationError) || !string.IsNullOrEmpty(SalaryError) ||
                !string.IsNullOrEmpty(CnicNumberError) || !string.IsNullOrEmpty(FingerprintIdError))
            {
                return;
            }

            IsSaving = true;

            try
            {
                var teacher = new Teacher
                {
                    TeacherID = teacherId ?? 0,
                    Name = Name.Trim(),
                    Age = age,
                    Experience = Experience?.Trim(),
                    DOB = DOB,
                    ContactNumber = ContactNumber.Trim(),
                    DateOfJoining = DateOfJoining,
                    Designation = Designation.Trim(),
                    Salary = salary,
                    Address = Address?.Trim(),
                    CnicNumber = CnicNumber.Trim(),
                    CnicFrontImagePath = CnicFrontImagePath?.Trim(),
                    CnicBackImagePath = CnicBackImagePath?.Trim(),
                    EducationalDocumentsPath = EducationalDocumentsPath?.Trim(),
                    CertificatesPath = CertificatesPath?.Trim(),
                    FingerprintID = fingerprintId
                };

                bool success;
                if (IsEditMode)
                {
                    success = await repository.UpdateStaffAsync(teacher).ConfigureAwait(true);
                }
                else
                {
                    success = await repository.AddStaffAsync(teacher).ConfigureAwait(true);
                }

                if (success)
                {
                    RequestClose?.Invoke(true);
                }
                else
                {
                    NameError = "Unable to save staff member. Please try again.";
                }
            }
            catch (Exception ex)
            {
                NameError = "Save failed: " + ex.Message;
            }
            finally
            {
                IsSaving = false;
            }
        }

        private static void SetPickedFile(Action<string> applyPath, string filter, bool allowMultiple)
        {
            var path = PickFile(filter, allowMultiple);
            if (!string.IsNullOrWhiteSpace(path))
            {
                applyPath(path);
            }
        }

        private static string PickFile(string filter, bool allowMultiple)
        {
            var dialog = new OpenFileDialog
            {
                Filter = filter,
                Multiselect = allowMultiple
            };

            return dialog.ShowDialog() == true
                ? string.Join(";", dialog.FileNames)
                : null;
        }
    }
}
