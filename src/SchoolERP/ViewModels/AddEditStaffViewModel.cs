using System;
using System.Threading.Tasks;
using System.Windows.Input;
using SchoolERP.Data;
using SchoolERP.Models;

namespace SchoolERP.ViewModels
{
    public class AddEditStaffViewModel : ViewModelBase
    {
        private readonly StaffRepository repository = new StaffRepository();
        private readonly int? teacherId;
        private string name;
        private string designation;
        private string salaryText;
        private string fingerprintIdText;
        private string nameError;
        private string designationError;
        private string salaryError;
        private string fingerprintIdError;
        private bool isSaving;

        public AddEditStaffViewModel(int? teacherId)
        {
            this.teacherId = teacherId;
            IsEditMode = teacherId.HasValue;
            WindowTitle = IsEditMode ? "Edit Staff" : "Add Staff";

            SaveCommand = new RelayCommand(_ => SaveAsync(), _ => !IsSaving);
            CancelCommand = new RelayCommand(_ => RequestClose?.Invoke(false));

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

        private async Task LoadStaffAsync(int id)
        {
            try
            {
                var teacher = await repository.GetStaffByIdAsync(id).ConfigureAwait(true);
                if (teacher != null)
                {
                    Name = teacher.Name;
                    Designation = teacher.Designation;
                    SalaryText = teacher.Salary.ToString("0.##");
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
            DesignationError = null;
            SalaryError = null;
            FingerprintIdError = null;

            if (string.IsNullOrWhiteSpace(Name))
            {
                NameError = "Full name is required.";
            }

            if (string.IsNullOrWhiteSpace(Designation))
            {
                DesignationError = "Designation is required.";
            }

            if (!decimal.TryParse(SalaryText, out var salary) || salary <= 0)
            {
                SalaryError = "Base salary is required and must be greater than zero.";
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

            if (!string.IsNullOrEmpty(NameError) || !string.IsNullOrEmpty(DesignationError) ||
                !string.IsNullOrEmpty(SalaryError) || !string.IsNullOrEmpty(FingerprintIdError))
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
                    Designation = Designation.Trim(),
                    Salary = salary,
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
    }
}
