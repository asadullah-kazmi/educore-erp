using System;
using SchoolERP.Data;
using SchoolERP.Models;

namespace SchoolERP.ViewModels
{
    public class TeacherAttendanceRowViewModel : ViewModelBase
    {
        private readonly AttendanceRepository repository;
        private int teacherID;
        private string name;
        private string designation;
        private int? fingerprintID;
        private int? attendanceID;
        private DateTime date;
        private string status;
        private DateTime? inTime;
        private bool isPresent;
        private bool isAbsent;

        public TeacherAttendanceRowViewModel(AttendanceRepository repository, TeacherAttendanceRow model)
        {
            this.repository = repository;
            UpdateFromModel(model);
        }

        public int TeacherID
        {
            get => teacherID;
            set => SetProperty(ref teacherID, value);
        }

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

        public int? FingerprintID
        {
            get => fingerprintID;
            set => SetProperty(ref fingerprintID, value);
        }

        public int? AttendanceID
        {
            get => attendanceID;
            set => SetProperty(ref attendanceID, value);
        }

        public DateTime Date
        {
            get => date;
            set => SetProperty(ref date, value);
        }

        public string Status
        {
            get => status;
            set
            {
                if (SetProperty(ref status, value))
                {
                    OnPropertyChanged(nameof(StatusDisplay));
                    OnPropertyChanged(nameof(StatusColor));
                }
            }
        }

        public DateTime? InTime
        {
            get => inTime;
            set => SetProperty(ref inTime, value);
        }

        public bool IsPresent
        {
            get => isPresent;
            set
            {
                if (SetProperty(ref isPresent, value) && value)
                {
                    IsAbsent = false;
                    Status = "Present";
                    _ = UpsertStatusAsync();
                }
            }
        }

        public bool IsAbsent
        {
            get => isAbsent;
            set
            {
                if (SetProperty(ref isAbsent, value) && value)
                {
                    IsPresent = false;
                    Status = "Absent";
                    _ = UpsertStatusAsync();
                }
            }
        }

        public string StatusDisplay
        {
            get
            {
                if (IsPresent) return "Present";
                if (IsAbsent) return "Absent";
                return "Not Marked";
            }
        }

        public string StatusColor
        {
            get
            {
                if (IsPresent) return "#10B981";
                if (IsAbsent) return "#EF4444";
                return "#94A3B8";
            }
        }

        private void UpdateFromModel(TeacherAttendanceRow model)
        {
            TeacherID = model.TeacherID;
            Name = model.Name;
            Designation = model.Designation;
            FingerprintID = model.FingerprintID;
            AttendanceID = model.AttendanceID;
            Date = model.Date;
            Status = model.Status;
            InTime = model.InTime;

            IsPresent = model.Status == "Present";
            IsAbsent = model.Status == "Absent";
        }

        private async System.Threading.Tasks.Task UpsertStatusAsync()
        {
            try
            {
                await repository.UpsertAttendanceAsync(
                    TeacherID,
                    Date,
                    Status,
                    IsPresent ? (DateTime?)DateTime.Now : null
                ).ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    "Failed to save attendance: " + ex.Message,
                    "Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error
                );
            }
        }
    }
}
