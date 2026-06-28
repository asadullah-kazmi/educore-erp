using System;
using SchoolERP.Models;

namespace SchoolERP.ViewModels
{
    public class StaffViewModel : ViewModelBase
    {
        public int TeacherID { get; set; }
        public string Name { get; set; }
        public int? Age { get; set; }
        public string Experience { get; set; }
        public DateTime? DOB { get; set; }
        public string ContactNumber { get; set; }
        public DateTime? DateOfJoining { get; set; }
        public string Designation { get; set; }
        public decimal Salary { get; set; }
        public string Address { get; set; }
        public string CnicNumber { get; set; }
        public string CnicFrontImagePath { get; set; }
        public string CnicBackImagePath { get; set; }
        public string EducationalDocumentsPath { get; set; }
        public string CertificatesPath { get; set; }
        public int? FingerprintId { get; set; }

        public string SalaryDisplay => Salary.ToString("N0");
        public string DOBDisplay => DOB?.ToString("dd MMM yyyy") ?? "-";
        public string DateOfJoiningDisplay => DateOfJoining?.ToString("dd MMM yyyy") ?? "-";

        public static StaffViewModel FromModel(Teacher teacher)
        {
            if (teacher == null)
            {
                return null;
            }

            return new StaffViewModel
            {
                TeacherID = teacher.TeacherID,
                Name = teacher.Name,
                Age = teacher.Age,
                Experience = teacher.Experience,
                DOB = teacher.DOB,
                ContactNumber = teacher.ContactNumber,
                DateOfJoining = teacher.DateOfJoining,
                Designation = teacher.Designation,
                Salary = teacher.Salary,
                Address = teacher.Address,
                CnicNumber = teacher.CnicNumber,
                CnicFrontImagePath = teacher.CnicFrontImagePath,
                CnicBackImagePath = teacher.CnicBackImagePath,
                EducationalDocumentsPath = teacher.EducationalDocumentsPath,
                CertificatesPath = teacher.CertificatesPath,
                FingerprintId = teacher.FingerprintID
            };
        }

        public Teacher ToModel()
        {
            return new Teacher
            {
                TeacherID = TeacherID,
                Name = Name,
                Age = Age,
                Experience = Experience,
                DOB = DOB,
                ContactNumber = ContactNumber,
                DateOfJoining = DateOfJoining,
                Designation = Designation,
                Salary = Salary,
                Address = Address,
                CnicNumber = CnicNumber,
                CnicFrontImagePath = CnicFrontImagePath,
                CnicBackImagePath = CnicBackImagePath,
                EducationalDocumentsPath = EducationalDocumentsPath,
                CertificatesPath = CertificatesPath,
                FingerprintID = FingerprintId
            };
        }
    }
}
