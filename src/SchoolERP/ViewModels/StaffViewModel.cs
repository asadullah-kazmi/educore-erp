using System;
using SchoolERP.Models;

namespace SchoolERP.ViewModels
{
    public class StaffViewModel : ViewModelBase
    {
        public int TeacherID { get; set; }
        public string Name { get; set; }
        public string Designation { get; set; }
        public decimal Salary { get; set; }
        public int? FingerprintId { get; set; }

        public string SalaryDisplay => Salary.ToString("N0");

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
                Designation = teacher.Designation,
                Salary = teacher.Salary,
                FingerprintId = teacher.FingerprintID
            };
        }

        public Teacher ToModel()
        {
            return new Teacher
            {
                TeacherID = TeacherID,
                Name = Name,
                Designation = Designation,
                Salary = Salary,
                FingerprintID = FingerprintId
            };
        }
    }
}
