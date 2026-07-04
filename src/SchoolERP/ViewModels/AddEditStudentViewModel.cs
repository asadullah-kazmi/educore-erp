using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Win32;
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
        private string section;
        private decimal monthlyFee;
        private string address;
        private string phone;
        private string studentFormBOrCnicNumber;
        private string studentFormBOrCnicPicturePath;
        private string studentFormBOrCnicFrontPicturePath;
        private byte[] studentFormBOrCnicFrontPictureData;
        private string studentFormBOrCnicFrontPictureFileName;
        private string studentFormBOrCnicBackPicturePath;
        private byte[] studentFormBOrCnicBackPictureData;
        private string studentFormBOrCnicBackPictureFileName;
        private string guardianCnicNumber;
        private string guardianCnicPicturePath;
        private string guardianCnicFrontPicturePath;
        private byte[] guardianCnicFrontPictureData;
        private string guardianCnicFrontPictureFileName;
        private string guardianCnicBackPicturePath;
        private byte[] guardianCnicBackPictureData;
        private string guardianCnicBackPictureFileName;
        private string guardianPhone;
        private string emergencyContactNumber;
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
            BrowseStudentFormPictureCommand = new RelayCommand(_ => StudentFormBOrCnicFrontPicturePath = BrowsePicture(StudentFormBOrCnicFrontPicturePath));
            BrowseStudentFormFrontPictureCommand = new RelayCommand(_ => StudentFormBOrCnicFrontPicturePath = BrowsePicture(StudentFormBOrCnicFrontPicturePath));
            BrowseStudentFormBackPictureCommand = new RelayCommand(_ => StudentFormBOrCnicBackPicturePath = BrowsePicture(StudentFormBOrCnicBackPicturePath));
            BrowseGuardianCnicPictureCommand = new RelayCommand(_ => GuardianCnicFrontPicturePath = BrowsePicture(GuardianCnicFrontPicturePath));
            BrowseGuardianCnicFrontPictureCommand = new RelayCommand(_ => GuardianCnicFrontPicturePath = BrowsePicture(GuardianCnicFrontPicturePath));
            BrowseGuardianCnicBackPictureCommand = new RelayCommand(_ => GuardianCnicBackPicturePath = BrowsePicture(GuardianCnicBackPicturePath));

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

        public string Section
        {
            get => section;
            set => SetProperty(ref section, value);
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

        public string StudentFormBOrCnicNumber
        {
            get => studentFormBOrCnicNumber;
            set => SetProperty(ref studentFormBOrCnicNumber, value);
        }

        public string StudentFormBOrCnicPicturePath
        {
            get => studentFormBOrCnicPicturePath;
            set => SetProperty(ref studentFormBOrCnicPicturePath, value);
        }

        public string StudentFormBOrCnicFrontPicturePath
        {
            get => studentFormBOrCnicFrontPicturePath;
            set => SetProperty(ref studentFormBOrCnicFrontPicturePath, value);
        }

        public string StudentFormBOrCnicBackPicturePath
        {
            get => studentFormBOrCnicBackPicturePath;
            set => SetProperty(ref studentFormBOrCnicBackPicturePath, value);
        }

        public string GuardianCnicNumber
        {
            get => guardianCnicNumber;
            set => SetProperty(ref guardianCnicNumber, value);
        }

        public string GuardianCnicPicturePath
        {
            get => guardianCnicPicturePath;
            set => SetProperty(ref guardianCnicPicturePath, value);
        }

        public string GuardianCnicFrontPicturePath
        {
            get => guardianCnicFrontPicturePath;
            set => SetProperty(ref guardianCnicFrontPicturePath, value);
        }

        public string GuardianCnicBackPicturePath
        {
            get => guardianCnicBackPicturePath;
            set => SetProperty(ref guardianCnicBackPicturePath, value);
        }

        public string GuardianPhone
        {
            get => guardianPhone;
            set => SetProperty(ref guardianPhone, value);
        }

        public string EmergencyContactNumber
        {
            get => emergencyContactNumber;
            set => SetProperty(ref emergencyContactNumber, value);
        }

        public DateTime? AdmissionDate
        {
            get => admissionDate;
            set => SetProperty(ref admissionDate, value);
        }

        public decimal MonthlyFee
        {
            get => monthlyFee;
            set => SetProperty(ref monthlyFee, value);
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

        public ICommand BrowseStudentFormPictureCommand { get; }

        public ICommand BrowseStudentFormFrontPictureCommand { get; }

        public ICommand BrowseStudentFormBackPictureCommand { get; }

        public ICommand BrowseGuardianCnicPictureCommand { get; }

        public ICommand BrowseGuardianCnicFrontPictureCommand { get; }

        public ICommand BrowseGuardianCnicBackPictureCommand { get; }

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
                        Section = student.Section;
                        MonthlyFee = student.MonthlyFee;
                        Address = student.Address;
                        Phone = student.Phone;
                        StudentFormBOrCnicNumber = student.StudentFormBOrCnicNumber;
                        StudentFormBOrCnicPicturePath = student.StudentFormBOrCnicPicturePath;
                        StudentFormBOrCnicFrontPicturePath = student.StudentFormBOrCnicFrontPicturePath;
                        studentFormBOrCnicFrontPictureData = student.StudentFormBOrCnicFrontPictureData;
                        studentFormBOrCnicFrontPictureFileName = student.StudentFormBOrCnicFrontPictureFileName;
                        StudentFormBOrCnicBackPicturePath = student.StudentFormBOrCnicBackPicturePath;
                        studentFormBOrCnicBackPictureData = student.StudentFormBOrCnicBackPictureData;
                        studentFormBOrCnicBackPictureFileName = student.StudentFormBOrCnicBackPictureFileName;
                        GuardianCnicNumber = student.GuardianCnicNumber;
                        GuardianCnicPicturePath = student.GuardianCnicPicturePath;
                        GuardianCnicFrontPicturePath = student.GuardianCnicFrontPicturePath;
                        guardianCnicFrontPictureData = student.GuardianCnicFrontPictureData;
                        guardianCnicFrontPictureFileName = student.GuardianCnicFrontPictureFileName;
                        GuardianCnicBackPicturePath = student.GuardianCnicBackPicturePath;
                        guardianCnicBackPictureData = student.GuardianCnicBackPictureData;
                        guardianCnicBackPictureFileName = student.GuardianCnicBackPictureFileName;
                        GuardianPhone = student.GuardianPhone;
                        EmergencyContactNumber = student.EmergencyContactNumber;
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
                var studentFrontImage = BuildImageDocument(StudentFormBOrCnicFrontPicturePath, studentFormBOrCnicFrontPictureData, studentFormBOrCnicFrontPictureFileName);
                var studentBackImage = BuildImageDocument(StudentFormBOrCnicBackPicturePath, studentFormBOrCnicBackPictureData, studentFormBOrCnicBackPictureFileName);
                var guardianFrontImage = BuildImageDocument(GuardianCnicFrontPicturePath, guardianCnicFrontPictureData, guardianCnicFrontPictureFileName);
                var guardianBackImage = BuildImageDocument(GuardianCnicBackPicturePath, guardianCnicBackPictureData, guardianCnicBackPictureFileName);

                var student = new Student
                {
                    StudentID = studentId ?? 0,
                    RegistrationNo = regNo,
                    Name = Name.Trim(),
                    FatherName = string.IsNullOrWhiteSpace(FatherName) ? null : FatherName.Trim(),
                    DOB = DOB,
                    ClassID = SelectedClassId,
                    ClassName = selectedClass?.ClassName,
                    Section = string.IsNullOrWhiteSpace(Section) ? null : Section.Trim().ToUpperInvariant(),
                    MonthlyFee = MonthlyFee,
                    Address = string.IsNullOrWhiteSpace(Address) ? null : Address.Trim(),
                    Phone = string.IsNullOrWhiteSpace(Phone) ? null : Phone.Trim(),
                    StudentFormBOrCnicNumber = string.IsNullOrWhiteSpace(StudentFormBOrCnicNumber) ? null : StudentFormBOrCnicNumber.Trim(),
                    StudentFormBOrCnicPicturePath = NormalizePicturePath(StudentFormBOrCnicFrontPicturePath, StudentFormBOrCnicPicturePath),
                    StudentFormBOrCnicFrontPicturePath = NormalizePicturePath(StudentFormBOrCnicFrontPicturePath, StudentFormBOrCnicPicturePath),
                    StudentFormBOrCnicFrontPictureData = studentFrontImage.Data,
                    StudentFormBOrCnicFrontPictureFileName = studentFrontImage.FileName,
                    StudentFormBOrCnicBackPicturePath = NormalizePicturePath(StudentFormBOrCnicBackPicturePath),
                    StudentFormBOrCnicBackPictureData = studentBackImage.Data,
                    StudentFormBOrCnicBackPictureFileName = studentBackImage.FileName,
                    GuardianCnicNumber = string.IsNullOrWhiteSpace(GuardianCnicNumber) ? null : GuardianCnicNumber.Trim(),
                    GuardianCnicPicturePath = NormalizePicturePath(GuardianCnicFrontPicturePath, GuardianCnicPicturePath),
                    GuardianCnicFrontPicturePath = NormalizePicturePath(GuardianCnicFrontPicturePath, GuardianCnicPicturePath),
                    GuardianCnicFrontPictureData = guardianFrontImage.Data,
                    GuardianCnicFrontPictureFileName = guardianFrontImage.FileName,
                    GuardianCnicBackPicturePath = NormalizePicturePath(GuardianCnicBackPicturePath),
                    GuardianCnicBackPictureData = guardianBackImage.Data,
                    GuardianCnicBackPictureFileName = guardianBackImage.FileName,
                    GuardianPhone = string.IsNullOrWhiteSpace(GuardianPhone) ? null : GuardianPhone.Trim(),
                    EmergencyContactNumber = string.IsNullOrWhiteSpace(EmergencyContactNumber) ? null : EmergencyContactNumber.Trim(),
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

        private static string BrowsePicture(string currentPath)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Select picture",
                Filter = "Image files (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp|All files (*.*)|*.*",
                FileName = currentPath
            };

            return dialog.ShowDialog() == true ? dialog.FileName : currentPath;
        }

        private static string NormalizePicturePath(params string[] paths)
        {
            foreach (var path in paths)
            {
                if (!string.IsNullOrWhiteSpace(path))
                {
                    return path.Trim();
                }
            }

            return null;
        }

        private static ImageDocument BuildImageDocument(string path, byte[] existingData, string existingFileName)
        {
            if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
            {
                return new ImageDocument
                {
                    Data = File.ReadAllBytes(path),
                    FileName = Path.GetFileName(path)
                };
            }

            return new ImageDocument
            {
                Data = existingData,
                FileName = existingFileName
            };
        }

        private class ImageDocument
        {
            public byte[] Data { get; set; }

            public string FileName { get; set; }
        }
    }
}
